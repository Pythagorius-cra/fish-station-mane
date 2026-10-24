using Content.Server.DoAfter;
using Content.Shared._Fish.ClockworkCult;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared._Fish.ClockworkCult.Events;
using Content.Shared._Fish.ClockworkCult.Prototypes;
using Content.Shared._Fish.ClockworkCult.UI;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.ClockworkCult.Systems;

/// <summary>
/// BUI слэба и чтение писаний через DoAfter.
/// </summary>
public sealed partial class FishClockworkSlabSystem : EntitySystem
{
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private FishClockworkPowerSystem _power = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private FishClockworkScriptureSystem _scripture = default!;
    [Dependency] private FishClockworkServantSystem _servant = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<FishClockworkSlabComponent>(FishClockworkSlabUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<FishClockworkSlabReciteMessage>(OnReciteMessage);
        });

        SubscribeLocalEvent<FishClockworkSlabComponent, FishClockworkScriptureReciteDoAfterEvent>(OnReciteDoAfter);
    }

    private void OnUiOpened(Entity<FishClockworkSlabComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent.Owner, args.Actor);
    }

    private void OnReciteMessage(Entity<FishClockworkSlabComponent> ent, ref FishClockworkSlabReciteMessage args)
    {
        var user = args.Actor;
        TryRecite(ent.Owner, user, args.ScripturePrototypeId);
    }

    public bool TryRecite(EntityUid slab, EntityUid user, string scriptureId)
    {
        if (!CanRecite(slab, user, scriptureId, out var proto))
            return false;

        var ev = new FishClockworkScriptureReciteDoAfterEvent { ScripturePrototypeId = scriptureId };
        var args = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(proto.ChannelTime), ev, slab)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        return _doAfter.TryStartDoAfter(args);
    }

    public bool CanRecite(EntityUid slab, EntityUid user, string scriptureId, out FishClockworkScripturePrototype proto, bool quiet = false)
    {
        proto = default!;

        if (!_servant.IsServant(user))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("fish-clockwork-not-servant"), user, user);
            return false;
        }

        if (!_prototypes.TryIndex(scriptureId, out proto!))
            return false;

        if (!_power.TryGetPower(out var power))
            return false;

        if (proto.Tier == FishClockworkScriptureTier.Script && !power.Comp.ScriptUnlocked)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("fish-clockwork-tier-locked-script"), user, user);
            return false;
        }

        if (proto.Tier == FishClockworkScriptureTier.Application && !power.Comp.ApplicationUnlocked)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("fish-clockwork-tier-locked-application"), user, user);
            return false;
        }

        if (!_power.HasPower(proto.PowerCost))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("fish-clockwork-not-enough-power"), user, user);
            return false;
        }

        if (proto.InvokersRequired > 1)
        {
            var nearby = 0;
            var query = EntityQueryEnumerator<FishClockworkServantComponent, TransformComponent>();
            var origin = Transform(user).Coordinates;
            while (query.MoveNext(out var servant, out _, out var xform))
            {
                if (xform.Coordinates.TryDistance(EntityManager, origin, out var dist) && dist <= 1.5f)
                    nearby++;
            }

            if (nearby < proto.InvokersRequired)
            {
                if (!quiet)
                    _popup.PopupEntity(Loc.GetString("fish-clockwork-need-invokers", ("count", proto.InvokersRequired)), user, user);
                return false;
            }
        }

        return _scripture.CanInvoke(user, proto, quiet);
    }

    private void OnReciteDoAfter(Entity<FishClockworkSlabComponent> ent, ref FishClockworkScriptureReciteDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;
        if (!CanRecite(ent.Owner, user, args.ScripturePrototypeId, out var proto))
            return;

        if (!_power.TrySpendPower(proto.PowerCost))
            return;

        if (_scripture.TryInvoke(ent.Owner, user, proto))
        {
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("fish-clockwork-scripture-success", ("name", Loc.GetString(proto.Name))), user, user);
            UpdateUi(ent.Owner, user);
        }
    }

    public void UpdateUi(EntityUid slab, EntityUid user)
    {
        if (!_ui.HasUi(slab, FishClockworkSlabUiKey.Key))
            return;

        _power.TryGetPower(out var powerEnt);
        var power = powerEnt.Comp;
        var arkPhase = FishClockworkArkPhase.Prep;
        TimeSpan? arkRemaining = null;

        var arkQuery = EntityQueryEnumerator<FishClockworkArkComponent>();
        while (arkQuery.MoveNext(out _, out var ark))
        {
            arkPhase = ark.Phase;
            arkRemaining = ark.Phase switch
            {
                FishClockworkArkPhase.Prep => TimeSpan.FromSeconds(Math.Max(0, ark.SecondsUntilActivation)),
                FishClockworkArkPhase.Grace => TimeSpan.FromSeconds(Math.Max(0, ark.GracePeriodRemaining)),
                FishClockworkArkPhase.Defense => TimeSpan.FromSeconds(Math.Max(0, FishClockworkCultConstants.GatewayRatvarArrival - ark.ProgressSeconds)),
                _ => null,
            };
            break;
        }

        var scriptures = new List<FishClockworkScriptureEntry>();
        foreach (var proto in _prototypes.EnumeratePrototypes<FishClockworkScripturePrototype>())
        {
            var available = true;
            string? reason = null;

            if (proto.Tier == FishClockworkScriptureTier.Script && (power == null || !power.ScriptUnlocked))
            {
                available = false;
                reason = Loc.GetString("fish-clockwork-tier-locked-script");
            }
            else if (proto.Tier == FishClockworkScriptureTier.Application && (power == null || !power.ApplicationUnlocked))
            {
                available = false;
                reason = Loc.GetString("fish-clockwork-tier-locked-application");
            }
            else if (power != null && !_power.HasPower(proto.PowerCost))
            {
                available = false;
                reason = Loc.GetString("fish-clockwork-not-enough-power");
            }

            scriptures.Add(new FishClockworkScriptureEntry(
                proto.ID,
                Loc.GetString(proto.Name),
                Loc.GetString(proto.Description),
                proto.Tier,
                proto.PowerCost,
                proto.ChannelTime,
                proto.InvokersRequired,
                available,
                reason));
        }

        scriptures.Sort((a, b) =>
        {
            var tier = a.Tier.CompareTo(b.Tier);
            return tier != 0 ? tier : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });

        var state = new FishClockworkSlabBoundUserInterfaceState(
            power?.Power ?? 0f,
            power?.Vitality ?? 0f,
            _servant.CountServants(),
            power?.ScriptUnlocked ?? false,
            power?.ApplicationUnlocked ?? false,
            power?.Heralded ?? false,
            arkRemaining,
            arkPhase,
            scriptures);

        _ui.SetUiState(slab, FishClockworkSlabUiKey.Key, state);
    }
}

