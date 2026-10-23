using Content.Server._Fish.ClockworkCult.GameRule;
using Content.Server.Chat.Managers;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.CollectiveMind;
using Content.Shared.Bible.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.ClockworkCult.Systems;

/// <summary>
/// Р”РѕР±Р°РІР»РµРЅРёРµ/СѓРґР°Р»РµРЅРёРµ СЃР»СѓРі Р Р°С‚РІР°СЂР° Рё РїСЂРѕРІРµСЂРєРё eligibility.
/// </summary>
public sealed partial class FishClockworkServantSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private FishClockworkPowerSystem _power = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedRoleSystem _roles = default!;

    private static readonly ProtoId<NpcFactionPrototype> ClockworkFaction = "FishClockworkCult";
    private static readonly ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";
    private static readonly EntProtoId MindRoleId = "FishMindRoleClockworkServant";

    public bool CanBecomeServant(EntityUid uid, bool forConversion = true, bool quiet = false)
    {
        if (HasComp<FishClockworkServantComponent>(uid))
            return false;

        if (HasComp<BloodCultistComponent>(uid))
            return false;

        if (forConversion && HasComp<MindShieldComponent>(uid))
            return false;

        // РЎС‚Р°СЂС‚РѕРІС‹Рµ СЃР»СѓРіРё РЅРµ РјРѕРіСѓС‚ Р±С‹С‚СЊ chaplain (РєР°Рє Captain/Chaplain restricted РЅР° tg).
        if (!forConversion && HasComp<BibleUserComponent>(uid))
            return false;

        if (!_mind.TryGetMind(uid, out _, out _))
            return false;

        return true;
    }

    public bool TryAddServant(EntityUid uid, bool silent = false, bool giveSlab = true)
    {
        if (!CanBecomeServant(uid, forConversion: true, quiet: silent))
            return false;

        DoAddServant(uid, silent, giveSlab, isConversion: true);
        return true;
    }

    public bool TryAddStartingServant(EntityUid uid)
    {
        if (!CanBecomeServant(uid, forConversion: false, quiet: true))
            return false;

        DoAddServant(uid, silent: false, giveSlab: true, isConversion: false);
        return true;
    }

    private void DoAddServant(EntityUid uid, bool silent, bool giveSlab, bool isConversion)
    {
        EnsureComp<FishClockworkServantComponent>(uid);

        var collective = EnsureComp<CollectiveMindComponent>(uid);
        if (!collective.Minds.Contains("FishClockworkCult"))
            collective.Minds.Add("FishClockworkCult");

        _faction.RemoveFaction(uid, NanoTrasenFaction, false);
        _faction.AddFaction(uid, ClockworkFaction);

        if (_mind.TryGetMind(uid, out var mindId, out _))
            _roles.MindAddRole(mindId, MindRoleId);

        var servantCount = 0;
        if (TryGetRule(out var rule))
        {
            rule.Comp.Servants.Add(uid);
            servantCount = rule.Comp.Servants.Count;
        }

        // Script unlock: 50kW РР›Р РєРѕРЅРІРµСЂСЃРёСЏ СЃР»СѓРіРё (РЅР° tg вЂ” convert at least one).
        if (isConversion)
            _power.UnlockScript();

        // Application unlock: 100kW РР›Р convert a servant.
        if (isConversion)
            _power.UnlockApplication();

        if (giveSlab)
            Spawn("FishClockworkSlab", Transform(uid).Coordinates);

        if (!silent && _mind.TryGetMind(uid, out _, out var mind) &&
            mind.UserId != null &&
            _player.TryGetSessionById(mind.UserId.Value, out var session))
        {
            _audio.PlayGlobal(
                new SoundPathSpecifier("/Audio/_Fish/ClockworkCult/ambience/antag/clockcultalr.ogg"),
                Filter.Empty().AddPlayer(session),
                false);

            _chatManager.DispatchServerMessage(session, Loc.GetString("fish-clockwork-role-greeting"));
        }

        _power.HierophantAnnounce(Loc.GetString("fish-clockwork-servant-joined", ("name", Name(uid))));
        _ = servantCount;
    }

    public bool TryRemoveServant(EntityUid uid, bool silent = false)
    {
        if (!HasComp<FishClockworkServantComponent>(uid))
            return false;

        RemComp<FishClockworkServantComponent>(uid);
        RemCompDeferred<FishClockworkArmamentsComponent>(uid);
        RemCompDeferred<FishEminenceComponent>(uid);

        if (TryComp<CollectiveMindComponent>(uid, out var collective))
        {
            collective.Minds.Remove("FishClockworkCult");
            if (collective.Minds.Count == 0)
                RemCompDeferred<CollectiveMindComponent>(uid);
        }

        _faction.RemoveFaction(uid, ClockworkFaction, false);
        _faction.AddFaction(uid, NanoTrasenFaction);

        if (TryGetRule(out var rule))
            rule.Comp.Servants.Remove(uid);

        if (!silent)
            _power.HierophantAnnounce(Loc.GetString("fish-clockwork-servant-left", ("name", Name(uid))));

        return true;
    }

    public bool IsServant(EntityUid uid)
    {
        return HasComp<FishClockworkServantComponent>(uid);
    }

    public int CountServants()
    {
        return TryGetRule(out var rule) ? rule.Comp.Servants.Count : 0;
    }

    private bool TryGetRule(out Entity<FishClockworkCultRuleComponent> rule)
    {
        rule = default;
        var query = EntityQueryEnumerator<FishClockworkCultRuleComponent, ActiveGameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            rule = (uid, comp);
            return true;
        }

        return false;
    }
}

