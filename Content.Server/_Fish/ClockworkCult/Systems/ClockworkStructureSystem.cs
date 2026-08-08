using Content.Server._Fish.ClockworkCult.GameRule;
using Content.Server.Popups;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server._Fish.ClockworkCult.Systems;

/// <summary>
/// Структуры, traps, vitality matrix, constructs.
/// </summary>
public sealed class ClockworkStructureSystem : EntitySystem
{
    [Dependency] private readonly ClockworkCultRuleSystem _cult = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClockworkTrapComponent, StartCollideEvent>(OnTrapCollide);
        SubscribeLocalEvent<ClockworkMarauderComponent, BeforeDamageChangedEvent>(OnMarauderDamage);
        SubscribeLocalEvent<ClockworkObservationConsoleComponent, ActivateInWorldEvent>(OnObservationActivate);
        SubscribeLocalEvent<ClockworkStargazerComponent, InteractUsingEvent>(OnStargazer);
        SubscribeLocalEvent<ClockworkFabricatorComponent, AfterInteractEvent>(OnFabricatorInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateVitalityMatrices(frameTime);
        UpdateWardens(frameTime);
        UpdatePrisms(frameTime);
        UpdateLenses(frameTime);
        UpdateRifts(frameTime);
        UpdateStructuresPower(frameTime);
    }

    private void UpdateVitalityMatrices(float frameTime)
    {
        if (!_cult.TryGetRule(out _, out var rule))
            return;

        var query = EntityQueryEnumerator<ClockworkVitalityMatrixComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var matrix, out _))
        {
            matrix.Accumulator += frameTime;
            if (matrix.Accumulator < matrix.DrainInterval)
                continue;

            matrix.Accumulator = 0f;

            foreach (var ent in _lookup.GetEntitiesInRange(uid, 1.2f))
            {
                if (HasComp<ClockworkServantComponent>(ent))
                {
                    // Heal servants cheaply via vitality spend handled on demand elsewhere.
                    continue;
                }

                if (!_mobState.IsAlive(ent))
                    continue;

                _cult.AddVitality(rule, matrix.DrainAmount);
                var drain = new DamageSpecifier { DamageDict = { ["Slash"] = FixedPoint2.New(matrix.DrainAmount) } };
                _damageable.TryChangeDamage(ent, drain, origin: uid);
            }
        }
    }

    private void UpdateWardens(float frameTime)
    {
        var query = EntityQueryEnumerator<ClockworkOcularWardenComponent, ClockworkStructureComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var warden, out var structure, out _))
        {
            if (!_cult.TryGetRule(out _, out var rule))
                continue;

            if (rule.Power < structure.PowerDrawPerSecond)
                continue;

            warden.Accumulator += frameTime;
            if (warden.Accumulator < warden.FireInterval)
                continue;

            warden.Accumulator = 0f;

            foreach (var ent in _lookup.GetEntitiesInRange(uid, warden.Range))
            {
                if (HasComp<ClockworkServantComponent>(ent))
                    continue;
                if (!_mobState.IsAlive(ent))
                    continue;

                _cult.TrySpendPower(rule, structure.PowerDrawPerSecond);
                Spawn(warden.ProjectilePrototype, _transform.GetMapCoordinates(uid));
                break;
            }
        }
    }

    private void UpdatePrisms(float frameTime)
    {
        var query = EntityQueryEnumerator<ClockworkProsperityPrismComponent, ClockworkStructureComponent>();
        while (query.MoveNext(out var uid, out var prism, out var structure))
        {
            if (!_cult.TryGetRule(out _, out var rule) || rule.Power < structure.PowerDrawPerSecond)
                continue;

            prism.Accumulator += frameTime;
            if (prism.Accumulator < prism.Interval)
                continue;
            prism.Accumulator = 0f;
            _cult.TrySpendPower(rule, structure.PowerDrawPerSecond);

            foreach (var ent in _lookup.GetEntitiesInRange(uid, prism.Range))
            {
                if (!HasComp<ClockworkServantComponent>(ent))
                    continue;
                _stun.TryUpdateParalyzeDuration(ent, TimeSpan.Zero);
            }
        }
    }

    private void UpdateLenses(float frameTime)
    {
        var query = EntityQueryEnumerator<ClockworkInterdictionLensComponent, ClockworkStructureComponent>();
        while (query.MoveNext(out var uid, out var lens, out var structure))
        {
            if (!_cult.TryGetRule(out _, out var rule) || rule.Power < structure.PowerDrawPerSecond)
                continue;

            lens.Accumulator += frameTime;
            if (lens.Accumulator < lens.Interval)
                continue;
            lens.Accumulator = 0f;
            _cult.TrySpendPower(rule, structure.PowerDrawPerSecond);

            foreach (var ent in _lookup.GetEntitiesInRange(uid, lens.Range))
            {
                if (HasComp<ClockworkServantComponent>(ent))
                    continue;
                if (!_mobState.IsAlive(ent))
                    continue;
                _stun.TryUpdateStunDuration(ent, TimeSpan.FromSeconds(0.8f));
            }
        }
    }

    private void UpdateRifts(float frameTime)
    {
        var query = EntityQueryEnumerator<ClockworkRiftComponent>();
        while (query.MoveNext(out var uid, out var rift))
        {
            rift.Accumulator += frameTime;
            if (rift.Accumulator >= rift.Lifetime)
                QueueDel(uid);
        }
    }

    private void UpdateStructuresPower(float frameTime)
    {
        // Placeholder tick for transmission-gated structures — power already spent in specialized updaters.
    }

    private void OnTrapCollide(EntityUid uid, ClockworkTrapComponent component, ref StartCollideEvent args)
    {
        if (!component.Armed)
            return;

        var other = args.OtherEntity;
        if (HasComp<ClockworkServantComponent>(other))
            return;

        if (!_mobState.IsAlive(other))
            return;

        component.Armed = false;
        _stun.TryUpdateParalyzeDuration(other, TimeSpan.FromSeconds(component.StunSeconds));
        var dmg = new DamageSpecifier { DamageDict = { ["Piercing"] = FixedPoint2.New(component.PierceDamage) } };
        _damageable.TryChangeDamage(other, dmg, origin: uid);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-trap-triggered"), other);
    }

    private void OnMarauderDamage(EntityUid uid, ClockworkMarauderComponent component, ref BeforeDamageChangedEvent args)
    {
        if (component.ShieldCharges <= 0)
            return;

        // Блокируем входящий урон щитом.
        component.ShieldCharges--;
        Dirty(uid, component);
        args.Cancelled = true;
    }

    private void OnObservationActivate(EntityUid uid, ClockworkObservationConsoleComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !HasComp<ClockworkServantComponent>(args.User))
            return;

        EntityUid? target = null;
        var query = EntityQueryEnumerator<ClockworkAbscondTargetComponent>();
        while (query.MoveNext(out var cache, out _))
        {
            target = cache;
            break;
        }

        if (target == null)
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-abscond-no-target"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        _transform.SetCoordinates(args.User, Transform(target.Value).Coordinates);
        args.Handled = true;
    }

    private void OnStargazer(EntityUid uid, ClockworkStargazerComponent component, InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<ClockworkServantComponent>(args.User))
            return;

        if (component.NextReady != null && _timing.CurTime < component.NextReady.Value)
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-stargazer-cooldown"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        component.NextReady = _timing.CurTime + component.Cooldown;
        EnsureComp<ClockworkArmamentComponent>(args.Used);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-stargazer-enchant"), args.User, args.User);
        args.Handled = true;
    }

    private void OnFabricatorInteract(EntityUid uid, ClockworkFabricatorComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!HasComp<ClockworkServantComponent>(args.User))
            return;

        if (!_cult.TryGetRule(out _, out var rule))
            return;

        // Упрощённо: клик по тайлу/стене тратит mats-эквивалент и даёт brass sheet + power.
        if (!_cult.TrySpendPower(rule, 10))
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-scripture-no-resources"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        Spawn(component.BrassPrototype, _transform.GetMapCoordinates(args.User));
        _cult.AddPower(rule, component.PowerPerSheet);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-fabricator-brass"), args.User, args.User);
        args.Handled = true;
    }
}
