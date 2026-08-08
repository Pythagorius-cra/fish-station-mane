using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Fish.ClockworkCult;

/// <summary>
/// HUD-иконка слуг Ратвара для других слуг.
/// </summary>
public sealed class ShowClockworkCultHudSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClockworkServantComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, ClockworkServantComponent component, ref GetStatusIconsEvent args)
    {
        var ent = _player.LocalSession?.AttachedEntity;
        if (!HasComp<ClockworkServantComponent>(ent))
            return;

        if (_prototype.TryIndex(component.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
