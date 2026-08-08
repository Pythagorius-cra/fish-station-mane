using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared.EntityEffects;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.ClockworkCult;

/// <summary>
/// Святая вода снимает статус слуги Ратвара.
/// </summary>
public sealed partial class DeconvertClockworkServantEntityEffectSystem
    : EntityEffectSystem<ClockworkServantComponent, DeconvertClockworkServant>
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> ServantTag = "ClockworkServant";
    private static readonly ProtoId<TagPrototype> DeconvertedTag = "DeconvertedClockwork";

    protected override void Effect(Entity<ClockworkServantComponent> entity, ref EntityEffectEvent<DeconvertClockworkServant> args)
    {
        var uid = entity.Owner;
        _stun.TryAddParalyzeDuration(uid, TimeSpan.FromSeconds(3f));
        var target = Identity.Name(uid, EntityManager);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-holy-water-deconvert", ("target", target)), uid);

        RemCompDeferred<ClockworkServantComponent>(uid);
        RemCompDeferred<CultMemberComponent>(uid);
        _tag.RemoveTag(uid, ServantTag);
        _tag.AddTag(uid, DeconvertedTag);
    }
}

public sealed partial class DeconvertClockworkServant : EntityEffectBase<DeconvertClockworkServant>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("clockwork-cult-reagent-deconvert");
}
