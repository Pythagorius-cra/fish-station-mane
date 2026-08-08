using Content.Server._Fish.ClockworkCult.GameRule;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._Fish.ClockworkCult;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._Fish.ClockworkCult.Systems;

/// <summary>
/// Sigil of Submission conversion.
/// </summary>
public sealed class ClockworkConversionSystem : EntitySystem
{
    [Dependency] private readonly ClockworkCultRuleSystem _cult = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClockworkSigilSubmissionComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ClockworkSigilSubmissionComponent, ClockworkConvertDoAfterEvent>(OnConvertFinished);
    }

    private void OnInteractHand(EntityUid uid, ClockworkSigilSubmissionComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<ClockworkServantComponent>(args.User))
            return;

        if (!_cult.TryGetRule(out _, out var rule))
            return;

        EntityUid? victim = null;
        foreach (var ent in _lookup.GetEntitiesInRange(uid, 0.6f))
        {
            if (ent == uid || ent == args.User)
                continue;
            if (!HasComp<ActorComponent>(ent))
                continue;
            if (!_cult.CanConvert(ent, rule))
                continue;

            victim = ent;
            break;
        }

        if (victim == null)
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-convert-no-target"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, component.ConvertTime,
            new ClockworkConvertDoAfterEvent(), uid, victim)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("clockwork-cult-convert-start"), victim.Value, args.User);
        }
    }

    private void OnConvertFinished(EntityUid uid, ClockworkSigilSubmissionComponent component, ClockworkConvertDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        args.Handled = true;

        if (!_cult.TryGetRule(out _, out var rule))
            return;

        if (!_cult.CanConvert(args.Target.Value, rule))
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-convert-failed"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        if (!_cult.MakeServant(args.Target.Value, rule))
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-convert-failed"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        Spawn(component.EffectPrototype, Transform(args.Target.Value).Coordinates);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-convert-success"), args.Target.Value, args.User);

        if (rule.ServantCount >= rule.ConvertThreshold && rule.ArkEntity != null)
            _cult.TryActivateArk(rule);
    }
}
