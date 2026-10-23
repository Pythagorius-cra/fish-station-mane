using Content.Server._Fish.ClockworkCult.GameRule;
using Content.Server.Chat.Systems;
using Content.Shared._Fish.ClockworkCult;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Server.Spawners.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Fish.ClockworkCult.Systems;

/// <summary>
/// Р¤Р°Р·С‹ Ark: prep в†’ activation в†’ grace в†’ defense в†’ Ratvar.
/// </summary>
public sealed partial class FishClockworkArkSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private FishClockworkCultRuleSystem _cultRule = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private FishClockworkPowerSystem _power = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishClockworkArkComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<FishClockworkArkComponent, DestructionEventArgs>(OnDestroyed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FishClockworkArkComponent>();
        while (query.MoveNext(out var uid, out var ark))
        {
            UpdateArk((uid, ark), frameTime);
        }
    }

    private void UpdateArk(Entity<FishClockworkArkComponent> ent, float frameTime)
    {
        var ark = ent.Comp;

        // РџР°СЃСЃРёРІРЅР°СЏ РіРµРЅРµСЂР°С†РёСЏ РјРѕС‰РЅРѕСЃС‚Рё РљРѕРІС‡РµРіРѕРј РІРѕ РІСЃРµС… Р°РєС‚РёРІРЅС‹С… С„Р°Р·Р°С….
        if (ark.Phase is FishClockworkArkPhase.Prep
            or FishClockworkArkPhase.Activating
            or FishClockworkArkPhase.Grace
            or FishClockworkArkPhase.Defense)
        {
            _power.TryAdjustPower(2.5f * frameTime, quiet: true);
        }

        switch (ark.Phase)
        {
            case FishClockworkArkPhase.Prep:
                if (ark.SecondsUntilActivation < 0)
                    return;

                var prevSeconds = ark.SecondsUntilActivation;
                ark.SecondsUntilActivation -= frameTime;

                // РќР° РїРѕР»РѕРІРёРЅРµ prep вЂ” unlock Script (РєР°Рє Ark halfway РЅР° tg).
                if (ark.InitialActivationDelay > 0f)
                {
                    var halfway = ark.InitialActivationDelay * 0.5f;
                    if (prevSeconds > halfway && ark.SecondsUntilActivation <= halfway)
                        _power.UnlockScript();
                }

                if (ark.SecondsUntilActivation <= 30f && prevSeconds > 30f)
                {
                    _power.HierophantAnnounce(Loc.GetString("fish-clockwork-ark-activating-soon"));
                    BeginActivation(ent);
                }
                else if (ark.SecondsUntilActivation <= 0f)
                {
                    BeginActivation(ent);
                }

                Dirty(ent);
                break;

            case FishClockworkArkPhase.Activating:
                // РљРѕСЂРѕС‚РєР°СЏ С„Р°Р·Р° Р°РЅРёРјР°С†РёРё вЂ” СЃСЂР°Р·Сѓ РІ grace.
                ActivateArk(ent);
                break;

            case FishClockworkArkPhase.Grace:
                ark.GracePeriodRemaining -= frameTime;
                if (ark.GracePeriodRemaining <= 0f)
                {
                    ark.Phase = FishClockworkArkPhase.Defense;
                    ark.ProgressSeconds = 0f;
                    _power.HierophantAnnounce(Loc.GetString("fish-clockwork-ark-defense-begin"));
                    SpawnStationRifts();
                }

                Dirty(ent);
                break;

            case FishClockworkArkPhase.Defense:
                ark.ProgressSeconds += frameTime;
                AnnounceDefenseThresholds(ent);

                if (ark.ProgressSeconds >= FishClockworkCultConstants.GatewayRatvarArrival)
                {
                    CompleteRatvar(ent);
                }

                Dirty(ent);
                break;
        }
    }

    private void BeginActivation(Entity<FishClockworkArkComponent> ent)
    {
        ent.Comp.Phase = FishClockworkArkPhase.Activating;
        ent.Comp.SecondsUntilActivation = 0f;
        Dirty(ent);
        _power.HierophantAnnounce(Loc.GetString("fish-clockwork-ark-activating"));
        _audio.PlayPvs(ent.Comp.ActivationSound, ent.Owner);
    }

    private void ActivateArk(Entity<FishClockworkArkComponent> ent)
    {
        ent.Comp.Phase = FishClockworkArkPhase.Grace;
        ent.Comp.Active = true;
        ent.Comp.GracePeriodRemaining = FishClockworkCultConstants.ArkGracePeriodSeconds;
        Dirty(ent);

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("fish-clockwork-ark-crew-announce", ("seconds", (int)ent.Comp.GracePeriodRemaining)),
            Loc.GetString("fish-clockwork-announcer"),
            playDefault: true,
            announcementSound: ent.Comp.ActivationSound,
            colorOverride: Color.FromHex("#BE8700"));

        MassRecall(ent);
        _power.HierophantAnnounce(Loc.GetString("fish-clockwork-ark-activated", ("minutes", (int)(ent.Comp.GracePeriodRemaining / 60f))));
    }

    private void AnnounceDefenseThresholds(Entity<FishClockworkArkComponent> ent)
    {
        var p = ent.Comp.ProgressSeconds;
        // РћРґРЅРѕСЂР°Р·РѕРІС‹Рµ РїРѕСЂРѕРіРё С‡РµСЂРµР· РЅРµР±РѕР»СЊС€РёРµ РѕРєРЅР°.
        if (p >= FishClockworkCultConstants.GatewayReebeFound && p - 1f < FishClockworkCultConstants.GatewayReebeFound)
            _power.HierophantAnnounce(Loc.GetString("fish-clockwork-ark-reebe-found"));
        if (p >= FishClockworkCultConstants.GatewayRatvarComing && p - 1f < FishClockworkCultConstants.GatewayRatvarComing)
            _power.HierophantAnnounce(Loc.GetString("fish-clockwork-ark-ratvar-coming"));
    }

    private void CompleteRatvar(Entity<FishClockworkArkComponent> ent)
    {
        if (ent.Comp.PurposeFulfilled)
            return;

        ent.Comp.PurposeFulfilled = true;
        ent.Comp.Phase = FishClockworkArkPhase.RatvarArrival;
        Dirty(ent);

        Spawn(ent.Comp.RatvarSpawnPrototype, Transform(ent).Coordinates);
        _power.HierophantAnnounce(Loc.GetString("fish-clockwork-ark-ratvar-arrived"));
        _cultRule.NotifyRatvarArrived();
    }

    /// <summary>
    /// Mass recall к первому найденному Ark (для Eminence action).
    /// </summary>
    public bool TryMassRecall()
    {
        var query = EntityQueryEnumerator<FishClockworkArkComponent>();
        while (query.MoveNext(out var uid, out var ark))
        {
            if (ark.Recalling || ark.RecallsRemaining <= 0)
                continue;

            MassRecall((uid, ark));
            return true;
        }

        return false;
    }

    public void MassRecall(Entity<FishClockworkArkComponent> ent)
    {
        if (ent.Comp.Recalling || ent.Comp.RecallsRemaining <= 0)
            return;

        ent.Comp.Recalling = true;
        _audio.PlayGlobal(ent.Comp.RecallSound, Filter.Broadcast(), true);
        _power.HierophantAnnounce(Loc.GetString("fish-clockwork-ark-mass-recall"));

        var dest = Transform(ent).Coordinates;
        var query = EntityQueryEnumerator<FishClockworkServantComponent, MobStateComponent>();
        while (query.MoveNext(out var servant, out _, out var mob))
        {
            if (!_mobState.IsAlive(servant, mob))
                continue;
            _transform.SetCoordinates(servant, dest);
        }

        ent.Comp.RecallsRemaining--;
        ent.Comp.Recalling = false;
        Dirty(ent);
    }

    private void SpawnStationRifts()
    {
        // РЎРїР°РІРЅРёРј РЅРµСЃРєРѕР»СЊРєРѕ СЂРёС„С‚РѕРІ РЅР° СЃС‚Р°РЅС†РёРё (РіСЂРёРґС‹ Р±РµР· Ark).
        var arkGrids = new HashSet<EntityUid>();
        var arkQuery = EntityQueryEnumerator<FishClockworkArkComponent, TransformComponent>();
        while (arkQuery.MoveNext(out _, out _, out var axform))
        {
            if (axform.GridUid != null)
                arkGrids.Add(axform.GridUid.Value);
        }

        var candidates = new List<EntityCoordinates>();
        var spawnQuery = EntityQueryEnumerator<TransformComponent>();
        // Р‘РµСЂС‘Рј СЃР»СѓС‡Р°Р№РЅС‹Рµ РєРѕРѕСЂРґРёРЅР°С‚С‹ СЃ station grids С‡РµСЂРµР· Marker / РїСЂРѕСЃС‚Рѕ interior tiles СЃР»РѕР¶РЅРѕ вЂ”
        // РёСЃРїРѕР»СЊР·СѓРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ SpawnPoint-С‹ СЃС‚Р°РЅС†РёРё РєР°Рє СЏРєРѕСЂСЏ.
        var markerQuery = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (markerQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == null || arkGrids.Contains(xform.GridUid.Value))
                continue;
            candidates.Add(xform.Coordinates);
        }

        if (candidates.Count == 0)
            return;

        EntityCoordinates? reebeDest = null;
        var reebeQuery = EntityQueryEnumerator<FishClockworkReebeMarkerComponent, TransformComponent>();
        while (reebeQuery.MoveNext(out var markerUid, out var marker, out _))
        {
            if (marker.CrewInvasion || marker.AbscondTarget)
            {
                reebeDest = Transform(markerUid).Coordinates;
                break;
            }
        }

        if (reebeDest == null)
        {
            var arkEntQuery = EntityQueryEnumerator<FishClockworkArkComponent, TransformComponent>();
            while (arkEntQuery.MoveNext(out var arkUid, out _, out _))
            {
                reebeDest = Transform(arkUid).Coordinates;
                break;
            }
        }

        var count = Math.Clamp(candidates.Count / 10, 3, 8);
        for (var i = 0; i < count; i++)
        {
            var coords = _random.Pick(candidates);
            var rift = Spawn("FishCityOfCogsRift", coords);
            if (reebeDest != null && TryComp(rift, out FishCityOfCogsRiftComponent? riftComp))
            {
                riftComp.Destination = reebeDest;
                Dirty(rift, riftComp);
            }
        }
    }

    private void OnDamaged(Entity<FishClockworkArkComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() <= 0)
            return;

        _audio.PlayPvs(ent.Comp.DamageSound, ent.Owner);

        if (ent.Comp.LastScreamTime != null &&
            _timing.CurTime < ent.Comp.LastScreamTime.Value + TimeSpan.FromSeconds(FishClockworkCultConstants.ArkScreamCooldownSeconds))
            return;

        ent.Comp.LastScreamTime = _timing.CurTime;
        _audio.PlayGlobal(ent.Comp.ScreamSound, Filter.Broadcast(), true);
        _power.HierophantAnnounce(Loc.GetString("fish-clockwork-ark-taking-damage"));
        Dirty(ent);
    }

    private void OnDestroyed(Entity<FishClockworkArkComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.PurposeFulfilled)
            return;

        ent.Comp.Phase = FishClockworkArkPhase.Destroyed;
        Dirty(ent);
        _power.HierophantAnnounce(Loc.GetString("fish-clockwork-ark-fallen"));
        _cultRule.NotifyArkDestroyed();
    }
}

