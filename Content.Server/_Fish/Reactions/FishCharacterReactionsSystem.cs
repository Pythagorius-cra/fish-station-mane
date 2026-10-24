using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared._Fish.Crit;
using Content.Shared._Fish.Reactions;
using Content.Shared._Sunrise.Footprints;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Fish.Reactions;

/// <summary>
/// Серверные реакции: боль/эмоут, кровь, детонация гранат в руках, связка с Crit/огнём.
/// Источники: tgstation grenade hit_reaction (15%), существующие Flammable/Footprint/EmoteOnDamage.
/// </summary>
public sealed partial class FishCharacterReactionsSystem : SharedFishCharacterReactionsSystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedFishSs1984CritSystem _crit = default!;

    private static readonly ProtoId<EmotePrototype> ScreamEmote = "Scream";
    private static readonly ProtoId<EmotePrototype> GaspEmote = "Gasp";
    private static readonly ProtoId<EmotePrototype> WhineEmote = "Whine";
    private static readonly ProtoId<DamageTypePrototype> HeatDamage = "Heat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishCharacterReactionsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FishCharacterReactionsComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<FishCharacterReactionsComponent, IgnitedEvent>(OnIgnited);
        SubscribeLocalEvent<FishCharacterReactionsComponent, ExtinguishedEvent>(OnExtinguished);
        SubscribeLocalEvent<FishCharacterReactionsComponent, MoveEvent>(OnMove);
    }

    private void OnMapInit(Entity<FishCharacterReactionsComponent> ent, ref MapInitEvent args)
    {
        // EmoteOnDamage живёт только на сервере — добавляем здесь, не из YAML.
        var emote = EnsureComp<EmoteOnDamageComponent>(ent);
        emote.WithChat = true;
        emote.EmoteChance = 0.35f;
        emote.EmoteCooldown = TimeSpan.FromSeconds(2.5);
        emote.Emotes =
        [
            ScreamEmote,
            WhineEmote,
            GaspEmote,
        ];
    }

    private void OnDamageChanged(Entity<FishCharacterReactionsComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (MobStateSys.IsDead(ent))
            return;

        // Полная бессознательность Crit — без болевых эмоутов.
        if (HasComp<FishCritUnconsciousComponent>(ent))
            return;

        TryDetonateHeldExplosive(ent, args);
        TryPainEmote(ent, args);
    }

    private void OnIgnited(Entity<FishCharacterReactionsComponent> ent, ref IgnitedEvent args)
    {
        if (MobStateSys.IsDead(ent) || HasComp<FishCritUnconsciousComponent>(ent))
            return;

        // Крик при возгорании (антиспам через NextPainEmote).
        if (Timing.CurTime < ent.Comp.NextPainEmote)
            return;

        _chat.TryEmoteWithChat(ent, ScreamEmote, ChatTransmitRange.Normal, forceEmote: true);
        ent.Comp.NextPainEmote = Timing.CurTime + ent.Comp.PainEmoteCooldown;
        Dirty(ent);

        // В Crit на огне — не даём soft-crit «встать поверх» stop-drop-roll.
        if (_crit.IsInCritZone(ent) && TryComp<FlammableComponent>(ent, out var flam) && flam.OnFire)
            EnsureComp<FishCritUnconsciousComponent>(ent);
    }

    private void OnExtinguished(Entity<FishCharacterReactionsComponent> ent, ref ExtinguishedEvent args)
    {
        // После тушения снимаем только «огненный» unconscious, если нет паралича Crit.
        // Фактический паралич чистит Crit-система.
    }

    private void OnMove(Entity<FishCharacterReactionsComponent> ent, ref MoveEvent args)
    {
        // OnlyRotation — поворот на месте; следы крови только при реальном смещении.
        if (args.OnlyRotation || args.ParentChanged)
            return;

        TryBleedTrail(ent);
    }

    /// <summary>
    /// TG: /obj/item/grenade/hit_reaction — PROJECTILE_ATTACK, non-stamina, prob(15) → detonate.
    /// </summary>
    private void TryDetonateHeldExplosive(Entity<FishCharacterReactionsComponent> ent, DamageChangedEvent args)
    {
        // Только урон от снаряда (Origin с ProjectileComponent) / существенный прирост.
        if (args.Origin is not { } origin || !HasComp<ProjectileComponent>(origin))
            return;

#pragma warning disable CS0618
        var totalDelta = args.DamageDelta?.GetTotal() ?? FixedPoint2.Zero;
#pragma warning restore CS0618
        if (totalDelta.Float() < ent.Comp.HeldExplosiveMinDamage)
            return;

        // Игнорируем чистый stamina-like: у нас нет STAMINA damage type как в TG; Heat-only от лазеров ок.
        if (!_random.Prob(ent.Comp.HeldExplosiveDetonateChance))
            return;

        foreach (var held in _hands.EnumerateHeld(ent.Owner))
        {
            // Только предметы с ExplodeOnTrigger + Explosive (как гранаты TG).
            if (!HasComp<ExplodeOnTriggerComponent>(held) || !HasComp<ExplosiveComponent>(held))
                continue;

            _popup.PopupEntity(
                Loc.GetString("fish-reactions-grenade-shot", ("item", held)),
                ent,
                PopupType.LargeCaution);

            // Ключ timer — стандарт для TimerGrenadeBase; null триггерит все эффекты.
            _trigger.Trigger(held, ent.Owner, key: "timer");
            // Если не взорвалось (dud) — всё равно удаляем как в TG.
            if (!Deleted(held) && !EntityManager.IsQueuedForDeletion(held))
                QueueDel(held);

            return; // одна граната за попадание
        }
    }

    private void TryPainEmote(Entity<FishCharacterReactionsComponent> ent, DamageChangedEvent args)
    {
        if (Timing.CurTime < ent.Comp.NextPainEmote)
            return;

        if (args.DamageDelta == null)
            return;

        // Порог: заметный урон
#pragma warning disable CS0618
        if (args.DamageDelta.GetTotal() < FixedPoint2.New(5))
            return;
#pragma warning restore CS0618

        if (!_random.Prob(0.35f))
            return;

        var heat = args.DamageDelta.DamageDict.TryGetValue(HeatDamage, out var h) && h > 0;
        var inCrit = _crit.IsInCritZone(ent);
        ProtoId<EmotePrototype> emote = heat ? ScreamEmote : inCrit ? GaspEmote : WhineEmote;

        _chat.TryEmoteWithChat(ent, emote, ChatTransmitRange.Normal, forceEmote: true);
        ent.Comp.NextPainEmote = Timing.CurTime + ent.Comp.PainEmoteCooldown;
        Dirty(ent);
    }

    /// <summary>
    /// Кровоточащий след: при BleedAmount > 0 капаем кровь в FootprintEmitter foots solution.
    /// </summary>
    private void TryBleedTrail(Entity<FishCharacterReactionsComponent> ent)
    {
        if (Timing.CurTime < ent.Comp.NextBleedTrail)
            return;

        if (!TryComp<BloodstreamComponent>(ent, out var blood) || blood.BleedAmount <= 0)
            return;

        if (!TryComp<FootprintEmitterComponent>(ent, out var emitter))
            return;

        if (!_solutions.TryGetSolution(ent.Owner, emitter.FootsSolutionName, out var solEnt, out var solution))
            return;

        // Перенос небольшого объёма крови — FootprintSystem сам рисует следы при движении.
        var reagent = blood.BloodReferenceSolution.Contents.Count > 0
            ? blood.BloodReferenceSolution.Contents[0].Reagent
            : new ReagentId("Blood", null);

        var amount = FixedPoint2.New(ent.Comp.BleedTrailTransfer * Math.Clamp(blood.BleedAmount / 5f, 0.25f, 2f));
        solution.AddReagent(reagent, amount);
        _solutions.UpdateChemicals(solEnt.Value);

        ent.Comp.NextBleedTrail = Timing.CurTime + ent.Comp.BleedTrailCooldown;
        Dirty(ent);
    }
}
