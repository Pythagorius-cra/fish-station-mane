using Content.Shared._Fish.Reactions;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Robust.Shared.Timing;
using MobStateEnum = Content.Shared.Mobs.MobState;

namespace Content.Shared._Fish.Crit;

/// <summary>
/// Shared API и soft-crit переопределения для SS1984 Crit.
/// </summary>
public abstract partial class SharedFishSs1984CritSystem : EntitySystem
{
    [Dependency] protected DamageableSystem Damageable = default!;
    [Dependency] protected MobStateSystem MobStateSys = default!;
    [Dependency] protected MobThresholdSystem MobThreshold = default!;
    [Dependency] protected StandingStateSystem Standing = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishSs1984CritComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FishSs1984CritComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FishSs1984CritComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(Entity<FishSs1984CritComponent> ent, ref MapInitEvent args)
    {
        // Dead-порог НЕ поднимаем: GibBehavior срабатывает при Blunt 400,
        // а Paradise complex-crit глубже -100 маппится на Critical 100–200 + documented deviation.
        if (TryComp<MobThresholdsComponent>(ent, out var thresholds) &&
            MobThreshold.TryGetThresholdForState(ent, MobStateEnum.Dead, out var dead, thresholds) &&
            dead != null)
        {
            ent.Comp.OriginalDeadThreshold = dead.Value.Float();
            ent.Comp.DeadDamageThreshold = dead.Value.Float();
        }

        ent.Comp.NextTick = Timing.CurTime + ent.Comp.TickInterval;
        Dirty(ent);
    }

    private void OnShutdown(Entity<FishSs1984CritComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<FishCritShockComponent>(ent);
        RemCompDeferred<FishHeartFailureComponent>(ent);
        RemCompDeferred<FishCardiacArrestComponent>(ent);
        RemCompDeferred<FishLoseBreathComponent>(ent);
        RemCompDeferred<FishCritUnconsciousComponent>(ent);
    }

    private void OnMobStateChanged(Entity<FishSs1984CritComponent> ent, ref MobStateChangedEvent args)
    {
        // Soft-crit: после входа в Critical сразу поднимаем, если нет полной бессознательности / огня.
        if (args.NewMobState != MobStateEnum.Critical)
            return;

        if (SharedFishCharacterReactionsSystem.ShouldSuppressSoftCritStand(EntityManager, ent.Owner))
            return;

        Standing.Stand(ent.Owner);
        _blocker.UpdateCanMove(ent.Owner);
    }

    /// <summary>
    /// Paradise health = CritThreshold - TotalDamage (при CritThreshold=100).
    /// </summary>
    public float GetParadiseHealth(EntityUid uid, FishSs1984CritComponent? crit = null, DamageableComponent? damageable = null)
    {
        if (!Resolve(uid, ref crit, false) || !Resolve(uid, ref damageable, false))
            return 100f;

#pragma warning disable CS0618 // GetTotalDamage нужен для маппинга шкалы Paradise
        var total = Damageable.GetTotalDamage((uid, damageable));
#pragma warning restore CS0618
        return crit.CritDamageThreshold - (float)total;
    }

    public bool IsInCritZone(EntityUid uid, FishSs1984CritComponent? crit = null, DamageableComponent? damageable = null)
    {
        if (!Resolve(uid, ref crit, false) || !Resolve(uid, ref damageable, false))
            return false;

        if (MobStateSys.IsDead(uid))
            return false;

#pragma warning disable CS0618
        return (float)Damageable.GetTotalDamage((uid, damageable)) >= crit.CritDamageThreshold;
#pragma warning restore CS0618
    }

    /// <summary>
    /// Soft-crit: в Critical без FishCritUnconscious действия не блокируются полностью.
    /// Вызывается из Fish-хука в MobStateSystem.
    /// </summary>
    public static bool AllowsSoftCritActions(IEntityManager entMan, EntityUid uid, MobStateEnum state)
    {
        if (state != MobStateEnum.Critical)
            return false;

        if (!entMan.HasComponent<FishSs1984CritComponent>(uid))
            return false;

        if (SharedFishCharacterReactionsSystem.ShouldSuppressSoftCritStand(entMan, uid))
            return false;

        return true;
    }

    public void AdjustLoseBreath(EntityUid uid, TimeSpan amount, TimeSpan? max = null)
    {
        var lose = EnsureComp<FishLoseBreathComponent>(uid);
        var cap = max ?? lose.MaxRemaining;
        var next = lose.Remaining + amount;
        if (next < TimeSpan.Zero)
            next = TimeSpan.Zero;
        if (next > cap)
            next = cap;
        lose.Remaining = next;
        lose.MaxRemaining = cap;
        Dirty(uid, lose);

        if (lose.Remaining <= TimeSpan.Zero)
            RemCompDeferred<FishLoseBreathComponent>(uid);
    }

    public void SetLoseBreath(EntityUid uid, TimeSpan amount)
    {
        if (amount <= TimeSpan.Zero)
        {
            RemCompDeferred<FishLoseBreathComponent>(uid);
            return;
        }

        var lose = EnsureComp<FishLoseBreathComponent>(uid);
        lose.Remaining = amount > lose.MaxRemaining ? lose.MaxRemaining : amount;
        Dirty(uid, lose);
    }

    public bool TrySetHeartAttack(EntityUid uid, bool arrest)
    {
        if (arrest)
        {
            var comp = EnsureComp<FishCardiacArrestComponent>(uid);
            comp.Active = true;
            Dirty(uid, comp);
            return true;
        }

        if (!TryComp<FishCardiacArrestComponent>(uid, out _))
            return false;

        RemCompDeferred<FishCardiacArrestComponent>(uid);
        return true;
    }

    public bool IsUndergoingCardiacArrest(EntityUid uid)
    {
        return TryComp<FishCardiacArrestComponent>(uid, out var arrest) && arrest.Active;
    }
}
