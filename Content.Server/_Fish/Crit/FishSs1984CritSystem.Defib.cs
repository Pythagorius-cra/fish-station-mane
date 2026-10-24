using Content.Shared._Fish.Crit;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Popups;

namespace Content.Server._Fish.Crit;

public sealed partial class FishSs1984CritSystem
{
    private void InitializeDefib()
    {
        SubscribeLocalEvent<FishSs1984CritComponent, TargetDefibrillatedEvent>(OnDefibrillated);
    }

    /// <summary>
    /// Дефибриллятор снимает остановку сердца (Paradise set_heartattack(FALSE)).
    /// </summary>
    private void OnDefibrillated(Entity<FishSs1984CritComponent> ent, ref TargetDefibrillatedEvent args)
    {
        if (IsUndergoingCardiacArrest(ent))
        {
            TrySetHeartAttack(ent, false);
            _popup.PopupEntity(Loc.GetString("fish-crit-defib-arrhythmia-cleared"), ent, args.User, PopupType.Medium);
        }

        // После успешного подъёма из Dead в Critical — soft-crit встаёт сам через OnMobStateChanged.
        RemCompDeferred<FishCritUnconsciousComponent>(ent);
        SetLoseBreath(ent, TimeSpan.Zero);
    }
}
