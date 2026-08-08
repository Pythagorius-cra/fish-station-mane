using Content.Server._Fish.ClockworkCult.GameRule;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared._Fish.ClockworkCult;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.ClockworkCult.Systems;

/// <summary>
/// Установка Integration Cog в APC.
/// </summary>
public sealed class ClockworkIntegrationCogSystem : EntitySystem
{
    [Dependency] private readonly ClockworkCultRuleSystem _cult = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClockworkIntegrationCogComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ClockworkIntegrationCogComponent, ClockworkInstallCogDoAfterEvent>(OnInstallFinished);
    }

    private void OnAfterInteract(EntityUid uid, ClockworkIntegrationCogComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!HasComp<ClockworkServantComponent>(args.User))
            return;

        if (!HasComp<ApcComponent>(args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-cog-not-apc"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (HasComp<ClockworkCoggedApcComponent>(args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-cog-already"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (!_cult.TryGetRule(out _, out _))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, component.InstallTime,
            new ClockworkInstallCogDoAfterEvent(), uid, args.Target, uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            args.Handled = true;
    }

    private void OnInstallFinished(EntityUid uid, ClockworkIntegrationCogComponent component, ClockworkInstallCogDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        args.Handled = true;

        if (!HasComp<ApcComponent>(args.Target.Value) || HasComp<ClockworkCoggedApcComponent>(args.Target.Value))
            return;

        if (!_cult.TryGetRule(out _, out var rule))
            return;

        EnsureComp<ClockworkCoggedApcComponent>(args.Target.Value);
        _cult.AddCog(rule, args.Target.Value);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-cog-installed"), args.Target.Value, args.User);
        QueueDel(uid);
    }
}
