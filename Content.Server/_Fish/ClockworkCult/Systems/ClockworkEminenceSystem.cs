using Content.Server._Fish.ClockworkCult.GameRule;
using Content.Server.Popups;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._Fish.ClockworkCult.Systems;

/// <summary>
/// Eminence abilities: mass recall once per round.
/// </summary>
public sealed class ClockworkEminenceSystem : EntitySystem
{
    [Dependency] private readonly ClockworkCultRuleSystem _cult = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClockworkEminenceComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, ClockworkEminenceComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<ActorComponent>(args.User) && args.User != uid)
            return;

        // Сам Eminence активирует recall на себе (use verb / activate).
        if (component.MassRecallUsed)
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-eminence-recall-used"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (!_cult.TryGetRule(out _, out var rule))
            return;

        component.MassRecallUsed = true;
        Dirty(uid, component);
        _cult.MassRecall(rule);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-eminence-recall"), uid, uid);
        args.Handled = true;
    }
}
