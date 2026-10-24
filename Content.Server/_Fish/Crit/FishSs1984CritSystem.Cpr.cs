using Content.Server.DoAfter;
using Content.Shared._Fish.Crit;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Crit;

public sealed partial class FishSs1984CritSystem
{
    [Dependency] private DoAfterSystem _doAfter = default!;

    private void InitializeCpr()
    {
        SubscribeLocalEvent<FishSs1984CritComponent, InteractHandEvent>(OnCritInteractHand);
        SubscribeLocalEvent<FishSs1984CritComponent, FishCritCprDoAfterEvent>(OnCprDoAfter);
    }

    /// <summary>
    /// Порт /mob/living/carbon/human/proc/do_cpr — СЛР на цели в зоне крита.
    /// </summary>
    private void OnCritInteractHand(Entity<FishSs1984CritComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || args.User == ent.Owner)
            return;

        // CPR только на цели, не от самой цели при self-interact.
        if (!IsInCritZone(ent.Owner, ent.Comp))
            return;

        if (MobStateSys.IsDead(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("fish-crit-cpr-target-dead"), ent.Owner, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (HasComp<FishCritCprActiveComponent>(ent))
        {
            _popup.PopupEntity(Loc.GetString("fish-crit-cpr-already"), ent.Owner, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        args.Handled = true;
        EnsureComp<FishCritCprActiveComponent>(ent);

        _popup.PopupEntity(
            Loc.GetString("fish-crit-cpr-start", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(ent, EntityManager))),
            ent,
            PopupType.MediumCaution);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(4), new FishCritCprDoAfterEvent(), ent.Owner, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    private void OnCprDoAfter(Entity<FishSs1984CritComponent> ent, ref FishCritCprDoAfterEvent args)
    {
        RemCompDeferred<FishCritCprActiveComponent>(ent);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (!IsInCritZone(ent.Owner, ent.Comp) || MobStateSys.IsDead(ent.Owner))
            return;

        // heal_damage_type(15, OXY)
        var heal = new DamageSpecifier(_proto.Index(Asphyxiation), FixedPoint2.New(-15));
        Damageable.TryChangeDamage(ent.Owner, heal, true, interruptsDoAfters: false);

        SetLoseBreath(ent.Owner, TimeSpan.Zero);

        // AdjustParalysis(-2 SECONDS) — в SS14 уменьшаем/снимаем паралич.
        RemCompDeferred<FishCritUnconsciousComponent>(ent);
        _stun.TryUpdateParalyzeDuration(ent.Owner, TimeSpan.Zero);

        _popup.PopupEntity(Loc.GetString("fish-crit-cpr-success"), ent, args.User, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("fish-crit-cpr-air"), ent, ent, PopupType.Small);
    }
}
