using Content.Shared._Fish.ClockworkCult;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Fish.ClockworkCult.Systems;

/// <summary>
/// РўСЂРёРіРіРµСЂС‹ СЃРёРіРёР»РѕРІ: Transgression, Submission, Vitality, Transmission.
/// </summary>
public sealed partial class FishClockworkSigilSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private FishClockworkPowerSystem _power = default!;
    [Dependency] private FishClockworkServantSystem _servant = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<DamageTypePrototype> HeatDamage = "Heat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishClockworkSigilComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<FishClockworkSigilComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<FishCityOfCogsRiftComponent, StartCollideEvent>(OnRiftCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FishClockworkSigilComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sigil, out _))
        {
            switch (sigil.SigilType)
            {
                case FishClockworkSigilType.Submission:
                    UpdateSubmission((uid, sigil));
                    break;
                case FishClockworkSigilType.Vitality:
                    UpdateVitality((uid, sigil), frameTime);
                    break;
                case FishClockworkSigilType.Transmission:
                    UpdateTransmission((uid, sigil), frameTime);
                    break;
            }
        }

        var riftQuery = EntityQueryEnumerator<FishCityOfCogsRiftComponent>();
        while (riftQuery.MoveNext(out var riftUid, out var rift))
        {
            rift.RemainingLifetime -= frameTime;
            if (rift.RemainingLifetime <= 0f)
                QueueDel(riftUid);
        }
    }

    private void OnStartCollide(Entity<FishClockworkSigilComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!HasComp<MobStateComponent>(other))
            return;

        ent.Comp.OccupantEnterTimes[other] = _timing.CurTime;

        switch (ent.Comp.SigilType)
        {
            case FishClockworkSigilType.Transgression:
                TriggerTransgression(ent, other);
                break;
        }
    }

    private void OnEndCollide(Entity<FishClockworkSigilComponent> ent, ref EndCollideEvent args)
    {
        ent.Comp.OccupantEnterTimes.Remove(args.OtherEntity);
    }

    private void TriggerTransgression(Entity<FishClockworkSigilComponent> ent, EntityUid target)
    {
        if (_servant.IsServant(target))
            return;

        _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(3));
        _popup.PopupEntity(Loc.GetString("fish-clockwork-sigil-transgression"), target, PopupType.MediumCaution);

        if (TryComp<FishClockworkTrapComponent>(ent.Owner, out var trap) && trap.ConsumedOnTrigger)
            QueueDel(ent.Owner);
        else
            QueueDel(ent.Owner);
    }

    private void UpdateSubmission(Entity<FishClockworkSigilComponent> ent)
    {
        foreach (var (occupant, entered) in ent.Comp.OccupantEnterTimes)
        {
            if (!Exists(occupant) || _servant.IsServant(occupant))
                continue;

            if (_timing.CurTime - entered < TimeSpan.FromSeconds(FishClockworkCultConstants.SubmissionConvertSeconds))
                continue;

            if (_servant.TryAddServant(occupant))
            {
                _popup.PopupEntity(Loc.GetString("fish-clockwork-sigil-submission"), occupant, PopupType.Large);
                QueueDel(ent.Owner);
                return;
            }
        }
    }

    private void UpdateTransmission(Entity<FishClockworkSigilComponent> ent, float frameTime)
    {
        // Слуга на сигиле — +10 W/s в глобальный пул (упрощённый Transmission).
        foreach (var occupant in ent.Comp.OccupantEnterTimes.Keys)
        {
            if (!Exists(occupant) || !_servant.IsServant(occupant))
                continue;

            _power.TryAdjustPower(FishClockworkCultConstants.TransmissionSigilPowerPerSecond * frameTime, quiet: true);
            ent.Comp.StoredPower += FishClockworkCultConstants.TransmissionSigilPowerPerSecond * frameTime;
            Dirty(ent);
            return; // один слуга достаточно
        }
    }

    private void UpdateVitality(Entity<FishClockworkSigilComponent> ent, float frameTime)
    {
        if (!TryComp<FishVitalityMatrixComponent>(ent.Owner, out var matrix))
            return;

        foreach (var occupant in ent.Comp.OccupantEnterTimes.Keys)
        {
            if (!Exists(occupant) || !TryComp<MobStateComponent>(occupant, out var mob))
                continue;

            if (_servant.IsServant(occupant))
            {
                if (_mobState.IsDead(occupant, mob))
                {
                    if (_power.TrySpendVitality(matrix.ReviveVitalityCost))
                    {
                        _mobState.ChangeMobState(occupant, MobState.Critical, mob);
                        _popup.PopupEntity(Loc.GetString("fish-clockwork-vitality-revive"), occupant, PopupType.Large);
                        QueueDel(ent.Owner);
                        return;
                    }
                }
                else
                {
                    var heal = new DamageSpecifier();
                    heal.DamageDict["Blunt"] = -matrix.HealPerSecond * frameTime;
                    heal.DamageDict["Slash"] = -matrix.HealPerSecond * frameTime;
                    heal.DamageDict["Piercing"] = -matrix.HealPerSecond * frameTime;
                    heal.DamageDict["Heat"] = -matrix.HealPerSecond * frameTime;
                    _damageable.TryChangeDamage(occupant, heal);
                    _power.AdjustVitality(-0.5f * frameTime);
                }
            }
            else if (_mobState.IsAlive(occupant, mob))
            {
                var dmg = new DamageSpecifier();
                dmg.DamageDict[HeatDamage] = matrix.DrainPerSecond * frameTime;
                if (_damageable.TryChangeDamage(occupant, dmg))
                    _power.AdjustVitality(matrix.DrainPerSecond * frameTime);
            }
        }
    }

    private void OnRiftCollide(Entity<FishCityOfCogsRiftComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!HasComp<MobStateComponent>(other))
            return;

        EntityCoordinates? destCoords = ent.Comp.Destination;

        // Stationв†’Reebe rifts Р±РµР· СЏРІРЅРѕРіРѕ Destination вЂ” РёС‰РµРј marker/Ark.
        if (destCoords == null && !ent.Comp.IsSpatialGateway)
        {
            var markerQuery = EntityQueryEnumerator<FishClockworkReebeMarkerComponent, TransformComponent>();
            while (markerQuery.MoveNext(out var markerUid, out var marker, out _))
            {
                if (marker.CrewInvasion || marker.AbscondTarget)
                {
                    destCoords = Transform(markerUid).Coordinates;
                    break;
                }
            }

            if (destCoords == null)
            {
                var arkQuery = EntityQueryEnumerator<FishClockworkArkComponent, TransformComponent>();
                while (arkQuery.MoveNext(out var arkUid, out _, out _))
                {
                    destCoords = Transform(arkUid).Coordinates;
                    break;
                }
            }
        }

        if (destCoords == null)
            return;

        EntityManager.System<SharedTransformSystem>().SetCoordinates(other, destCoords.Value);
        _popup.PopupEntity(
            Loc.GetString(ent.Comp.IsSpatialGateway
                ? "fish-clockwork-gateway-success"
                : "fish-clockwork-rift-teleport"),
            other,
            PopupType.Medium);
    }
}


