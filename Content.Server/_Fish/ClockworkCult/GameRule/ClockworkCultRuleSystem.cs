using System.Linq;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.CollectiveMind;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Fish.ClockworkCult.GameRule;

/// <summary>
/// Roundstart Clockwork Cult на основной станции (без Reebe).
/// </summary>
public sealed class ClockworkCultRuleSystem : GameRuleSystem<ClockworkCultRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly ISharedPlayerManager _players = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId MindRoleId = "MindRoleClockworkServant";
    private static readonly EntProtoId ProtectArkObjective = "ClockworkProtectArkObjective";
    private static readonly ProtoId<TagPrototype> DeconvertedTag = "DeconvertedClockwork";
    private static readonly ProtoId<TagPrototype> ServantTag = "ClockworkServant";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterSelected);
        SubscribeLocalEvent<ClockworkCultRuleComponent, AntagSelectionCompleteEvent>(OnSelectionComplete);
        SubscribeLocalEvent<ClockworkServantComponent, MobStateChangedEvent>(OnServantMobState);
        SubscribeLocalEvent<ClockworkServantComponent, ComponentRemove>(OnServantRemoved);
        SubscribeLocalEvent<ClockworkArkComponent, ComponentShutdown>(OnArkDestroyed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ClockworkCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            TickEconomy(rule, frameTime);
            TickArkPhases(uid, rule);
        }
    }

    private void OnAfterSelected(Entity<ClockworkCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeServant(args.EntityUid, ent.Comp);
    }

    private void OnSelectionComplete(Entity<ClockworkCultRuleComponent> ent, ref AntagSelectionCompleteEvent args)
    {
        foreach (var scripture in ent.Comp.StartingScriptures)
        {
            if (!ent.Comp.UnlockedScriptures.Contains(scripture))
                ent.Comp.UnlockedScriptures.Add(scripture);
        }

        Dirty(ent);
    }

    private void OnServantMobState(EntityUid uid, ClockworkServantComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            CheckRoundShouldEnd();
    }

    private void OnServantRemoved(EntityUid uid, ClockworkServantComponent component, ComponentRemove args)
    {
        var rule = GetRule();
        if (rule == null)
            return;

        rule.Servants.Remove(uid);
        rule.ServantCount = rule.Servants.Count;
        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);

        CheckRoundShouldEnd();
    }

    private void OnArkDestroyed(EntityUid uid, ClockworkArkComponent component, ComponentShutdown args)
    {
        var rule = GetRule();
        if (rule == null || rule.WinCondition != ClockworkCultWinCondition.Ongoing)
            return;

        if (rule.ArkEntity != uid)
            return;

        if (rule.ArkPhase is ClockworkArkPhase.RatvarRisen)
            return;

        rule.ArkPhase = ClockworkArkPhase.Destroyed;
        rule.WinCondition = ClockworkCultWinCondition.CultFailure;
        rule.ArkEntity = null;
        Announce(Loc.GetString("clockwork-cult-ark-destroyed"));
        _roundEnd.EndRound();
    }

    public ClockworkCultRuleComponent? GetRule()
    {
        return EntityQuery<ClockworkCultRuleComponent>().FirstOrDefault();
    }

    public EntityUid? GetRuleEntity()
    {
        var query = EntityQueryEnumerator<ClockworkCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var gameRule))
        {
            if (GameTicker.IsGameRuleActive(uid, gameRule))
                return uid;
        }

        return null;
    }

    public bool TryGetRule(out EntityUid ruleUid, out ClockworkCultRuleComponent rule)
    {
        ruleUid = default;
        rule = null!;

        var query = EntityQueryEnumerator<ClockworkCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var component, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            ruleUid = uid;
            rule = component;
            return true;
        }

        return false;
    }

    public bool MakeServant(EntityUid servant, ClockworkCultRuleComponent rule)
    {
        if (!_mind.TryGetMind(servant, out var mindId, out var mind))
            return false;

        if (_tag.HasTag(servant, DeconvertedTag))
            return false;

        if (HasComp<ClockworkServantComponent>(servant))
            return true;

        if (rule.ServantCount >= rule.ConvertCap && rule.Servants.Count > 0)
            return false;

        _roles.MindAddRole(mindId, MindRoleId);

        var servantComp = EnsureComp<ClockworkServantComponent>(servant);
        EnsureComp<CultMemberComponent>(servant);

        var collectiveMind = EnsureComp<CollectiveMindComponent>(servant);
        if (!collectiveMind.Minds.Contains("ClockworkCult"))
            collectiveMind.Minds.Add("ClockworkCult");

        _tag.AddTag(servant, ServantTag);

        _faction.RemoveFaction(servant, "NanoTrasen", false);
        _faction.AddFaction(servant, "ClockworkCult");

        var isHumanoid = HasComp<HumanoidAppearanceComponent>(servant);
        if (!isHumanoid && !HasComp<StatusIconComponent>(servant))
            EnsureComp<StatusIconComponent>(servant);

        if (isHumanoid)
        {
            GiveSlab(servant, servantComp, rule);

            if (_players.TryGetSessionById(mind.UserId, out var session))
            {
                _audio.PlayGlobal(
                    new SoundPathSpecifier("/Audio/Misc/ratvar_reveal.ogg"),
                    Filter.Empty().AddPlayer(session),
                    false,
                    AudioParams.Default.WithVolume(-5f));

                _chat.DispatchServerMessage(session, Loc.GetString("clockwork-cult-role-greeting"));
            }

            _mind.TryAddObjective(mindId, mind, ProtectArkObjective);
        }

        rule.Servants.Add(servant);
        rule.ServantCount = rule.Servants.Count;
        Dirty(servant, servantComp);

        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);

        return true;
    }

    public void RemoveServant(EntityUid servant, ClockworkCultRuleComponent rule, bool deconverted = true)
    {
        RemCompDeferred<ClockworkServantComponent>(servant);
        RemCompDeferred<CultMemberComponent>(servant);

        if (TryComp<CollectiveMindComponent>(servant, out var mind))
            mind.Minds.Remove("ClockworkCult");

        _tag.RemoveTag(servant, ServantTag);
        if (deconverted)
            _tag.AddTag(servant, DeconvertedTag);

        _faction.RemoveFaction(servant, "ClockworkCult", false);
        _faction.AddFaction(servant, "NanoTrasen");

        rule.Servants.Remove(servant);
        rule.ServantCount = rule.Servants.Count;

        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);
    }

    private void GiveSlab(EntityUid servant, ClockworkServantComponent servantComp, ClockworkCultRuleComponent rule)
    {
        if (servantComp.Slab != null && Exists(servantComp.Slab.Value))
            return;

        var slab = Spawn(rule.SlabPrototype, Transform(servant).Coordinates);
        servantComp.Slab = slab;

        if (_inventory.TryGetSlotEntity(servant, "back", out var backPack) && backPack != null)
            _storage.Insert(backPack.Value, slab, out _);
    }

    public bool TrySpendPower(ClockworkCultRuleComponent rule, int amount)
    {
        if (amount <= 0)
            return true;

        if (rule.Power < amount)
            return false;

        rule.Power -= amount;
        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);
        return true;
    }

    public bool TrySpendVitality(ClockworkCultRuleComponent rule, int amount)
    {
        if (amount <= 0)
            return true;

        if (rule.Vitality < amount)
            return false;

        rule.Vitality -= amount;
        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);
        return true;
    }

    public void AddPower(ClockworkCultRuleComponent rule, int amount)
    {
        if (amount <= 0)
            return;

        rule.Power = Math.Min(rule.MaxPower, rule.Power + amount);
        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);
    }

    public void AddVitality(ClockworkCultRuleComponent rule, int amount)
    {
        if (amount <= 0)
            return;

        rule.Vitality = Math.Min(rule.MaxVitality, rule.Vitality + amount);
        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);
    }

    public void AddCog(ClockworkCultRuleComponent rule, EntityUid apc)
    {
        if (!rule.CoggedApcs.Add(apc))
            return;

        rule.Cogs += 1;
        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);
    }

    public bool TryUnlockScripture(ClockworkCultRuleComponent rule, string scriptureId, int cost)
    {
        if (rule.UnlockedScriptures.Contains(scriptureId))
            return true;

        if (rule.Cogs < cost)
            return false;

        rule.Cogs -= cost;
        rule.UnlockedScriptures.Add(scriptureId);
        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);
        return true;
    }

    public bool IsUnlocked(ClockworkCultRuleComponent rule, string scriptureId)
    {
        return rule.UnlockedScriptures.Contains(scriptureId);
    }

    public bool CanConvert(EntityUid target, ClockworkCultRuleComponent rule)
    {
        if (!HasComp<MindContainerComponent>(target))
            return false;

        if (!_mobState.IsAlive(target))
            return false;

        if (HasComp<ClockworkServantComponent>(target))
            return false;

        if (HasComp<MindShieldComponent>(target))
            return false;

        if (_tag.HasTag(target, DeconvertedTag))
            return false;

        if (rule.ServantCount >= rule.ConvertCap)
            return false;

        return true;
    }

    public void RegisterArk(EntityUid ark, ClockworkCultRuleComponent rule)
    {
        rule.ArkEntity = ark;
        EnsureComp<ClockworkAbscondTargetComponent>(ark);
        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);
    }

    public bool TryActivateArk(ClockworkCultRuleComponent rule, bool force = false)
    {
        if (rule.ArkPhase != ClockworkArkPhase.Inactive)
            return false;

        if (rule.ArkEntity == null || !Exists(rule.ArkEntity.Value))
            return false;

        if (!force && rule.ServantCount < rule.ConvertThreshold)
            return false;

        BeginArkSequence(rule);
        return true;
    }

    private void BeginArkSequence(ClockworkCultRuleComponent rule)
    {
        rule.ArkPhase = ClockworkArkPhase.Announced;
        rule.ArkPhaseEndsAt = _timing.CurTime + TimeSpan.FromSeconds(30);
        Announce(Loc.GetString("clockwork-cult-ark-announced"));
        MassRecall(rule);

        if (TryComp(rule.ArkEntity, out ClockworkArkComponent? ark))
        {
            ark.Activated = true;
            Dirty(rule.ArkEntity.Value, ark);
        }

        if (TryGetRule(out var ruleUid, out _))
            Dirty(ruleUid, rule);
    }

    private void TickEconomy(ClockworkCultRuleComponent rule, float frameTime)
    {
        if (rule.CoggedApcs.Count == 0)
            return;

        // Начисляем power от cogged APC раз в секунду пачкой через аккумулятор в Update вызывающей стороны:
        // здесь простое пропорциональное начисление.
        var gain = (int)(rule.PowerPerCoggedApcPerSecond * rule.CoggedApcs.Count * frameTime);
        if (gain > 0)
            AddPower(rule, gain);
    }

    private void TickArkPhases(EntityUid ruleUid, ClockworkCultRuleComponent rule)
    {
        if (rule.ArkPhaseEndsAt == null)
            return;

        if (_timing.CurTime < rule.ArkPhaseEndsAt.Value)
            return;

        switch (rule.ArkPhase)
        {
            case ClockworkArkPhase.Announced:
                rule.ArkPhase = ClockworkArkPhase.Grace;
                rule.ArkPhaseEndsAt = _timing.CurTime + rule.GraceDuration;
                Announce(Loc.GetString("clockwork-cult-ark-grace"));
                break;
            case ClockworkArkPhase.Grace:
                rule.ArkPhase = ClockworkArkPhase.Siege;
                rule.ArkPhaseEndsAt = _timing.CurTime + rule.SiegeDuration;
                Announce(Loc.GetString("clockwork-cult-ark-siege"));
                SpawnRifts(8);
                break;
            case ClockworkArkPhase.Siege:
                rule.ArkPhase = ClockworkArkPhase.Assault;
                rule.ArkPhaseEndsAt = _timing.CurTime + rule.AssaultDuration;
                Announce(Loc.GetString("clockwork-cult-ark-assault"));
                SpawnRifts(12);
                break;
            case ClockworkArkPhase.Assault:
                rule.ArkPhase = ClockworkArkPhase.Cleanup;
                rule.ArkPhaseEndsAt = _timing.CurTime + rule.CleanupDuration;
                Announce(Loc.GetString("clockwork-cult-ark-cleanup"));
                break;
            case ClockworkArkPhase.Cleanup:
                RiseRatvar(rule);
                break;
        }

        Dirty(ruleUid, rule);
    }

    private void RiseRatvar(ClockworkCultRuleComponent rule)
    {
        if (rule.WinCondition != ClockworkCultWinCondition.Ongoing)
            return;

        rule.ArkPhase = ClockworkArkPhase.RatvarRisen;
        rule.ArkPhaseEndsAt = null;
        rule.WinCondition = ClockworkCultWinCondition.CultWin;

        var spawnCoords = rule.ArkEntity != null && Exists(rule.ArkEntity.Value)
            ? Transform(rule.ArkEntity.Value).Coordinates
            : default;

        if (spawnCoords != default)
            Spawn(rule.RatvarSpawnPrototype, spawnCoords);

        Announce(Loc.GetString("clockwork-cult-ratvar-risen"));
        _roundEnd.EndRound();
    }

    private void SpawnRifts(int count)
    {
        // Station-local pressure: спавним rifty рядом со слугами / Ark, без другой карты.
        var spawned = 0;
        if (TryGetRule(out _, out var rule) && rule.ArkEntity != null && Exists(rule.ArkEntity.Value))
        {
            var coords = Transform(rule.ArkEntity.Value).Coordinates;
            for (var i = 0; i < count && spawned < count; i++)
            {
                Spawn("ClockworkRift", coords);
                spawned++;
            }
        }
    }

    public void MassRecall(ClockworkCultRuleComponent rule)
    {
        EntityUid? target = null;
        if (rule.ArkEntity != null && Exists(rule.ArkEntity.Value))
            target = rule.ArkEntity;

        if (target == null)
        {
            var cacheQuery = EntityQueryEnumerator<ClockworkCacheComponent>();
            while (cacheQuery.MoveNext(out var cacheUid, out _))
            {
                target = cacheUid;
                break;
            }
        }

        if (target == null)
            return;

        var dest = Transform(target.Value).Coordinates;
        foreach (var servant in rule.Servants.ToList())
        {
            if (!Exists(servant) || !_mobState.IsAlive(servant))
                continue;

            _transform.SetCoordinates(servant, dest);
        }
    }

    private void CheckRoundShouldEnd()
    {
        var rule = GetRule();
        if (rule == null || rule.WinCondition != ClockworkCultWinCondition.Ongoing)
            return;

        // После открытия Ark поражение только через уничтожение Ark.
        if (rule.ArkPhase is not ClockworkArkPhase.Inactive and not ClockworkArkPhase.Destroyed)
            return;

        var alive = rule.Servants.Count(s => Exists(s) && _mobState.IsAlive(s));
        if (alive > 0)
            return;

        rule.WinCondition = ClockworkCultWinCondition.CultFailure;
        Announce(Loc.GetString("clockwork-cult-all-servants-dead"));
        _roundEnd.EndRound();
    }

    private void Announce(string message)
    {
        _chat.DispatchServerAnnouncement(message);
    }

    protected override void AppendRoundEndText(
        EntityUid uid,
        ClockworkCultRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        var winText = Loc.GetString($"clockwork-cult-cond-{component.WinCondition.ToString().ToLower()}");
        args.AddLine(winText);
        args.AddLine(Loc.GetString("clockwork-cult-list-start"));

        foreach (var (_, sessionData, name) in _antag.GetAntagIdentifiers(uid))
        {
            args.AddLine(Loc.GetString("clockwork-cult-list-name", ("name", name), ("user", sessionData.UserName)));
        }
    }
}
