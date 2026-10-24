using Content.Shared._Fish.Crit;
using Content.Shared.Atmos.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;
using MobStateEnum = Content.Shared.Mobs.MobState;

namespace Content.Shared._Fish.Reactions;

/// <summary>
/// Shared-координатор Character Reactions: Crit / огонь / очистка состояний.
/// </summary>
public abstract partial class SharedFishCharacterReactionsSystem : EntitySystem
{
    [Dependency] protected MobStateSystem MobStateSys = default!;
    [Dependency] protected StandingStateSystem Standing = default!;
    [Dependency] protected IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FishCharacterReactionsComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<FishCharacterReactionsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMobStateChanged(Entity<FishCharacterReactionsComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobStateEnum.Dead)
        {
            ClearTransientReactionState(ent.Owner);
            return;
        }

        // После revive из Dead — убрать зависшие осложнения Crit/реакций, если урон уже ниже крита.
        if (args.OldMobState == MobStateEnum.Dead && args.NewMobState != MobStateEnum.Dead)
        {
            // LoseBreath / unconscious сбрасываем; шок/HF оставляем — в оригинале HF сам не проходит.
            RemCompDeferred<FishCritUnconsciousComponent>(ent);
            RemCompDeferred<FishLoseBreathComponent>(ent);
        }
    }

    private void OnShutdown(Entity<FishCharacterReactionsComponent> ent, ref ComponentShutdown args)
    {
        ClearTransientReactionState(ent.Owner);
    }

    /// <summary>
    /// Временные FX, которые не должны переживать смерть / удаление сущности.
    /// </summary>
    public void ClearTransientReactionState(EntityUid uid)
    {
        RemCompDeferred<FishCritUnconsciousComponent>(uid);
        RemCompDeferred<FishLoseBreathComponent>(uid);
        // Шок снимается при смерти (как disease на трупе в Paradise stage_act early-out).
        RemCompDeferred<FishCritShockComponent>(uid);
        // HF и cardiac arrest не чистим здесь при простом Clear — только при смерти через OnMobStateChanged.
        if (MobStateSys.IsDead(uid))
        {
            RemCompDeferred<FishHeartFailureComponent>(uid);
            RemCompDeferred<FishCardiacArrestComponent>(uid);
        }
    }

    /// <summary>
    /// Soft-crit не должен поднимать горящего / сбитого / бессознательного персонажа
    /// (stop-drop-roll / Resist и Knockdown из Crit требуют оставаться вниз).
    /// </summary>
    public static bool ShouldSuppressSoftCritStand(IEntityManager entMan, EntityUid uid)
    {
        if (entMan.TryGetComponent<FlammableComponent>(uid, out var flam) && flam.OnFire)
            return true;

        if (entMan.HasComponent<FishCritUnconsciousComponent>(uid))
            return true;

        // Не отменять активный knockdown/crawl soft-crit'ом при входе в Critical.
        if (entMan.HasComponent<KnockedDownComponent>(uid))
            return true;

        return false;
    }
}
