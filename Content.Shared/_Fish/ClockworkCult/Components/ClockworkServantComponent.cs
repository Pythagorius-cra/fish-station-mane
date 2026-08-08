using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.ClockworkCult.Components;

/// <summary>
/// Маркер слуги Ратвара на теле.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClockworkServantComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "ClockworkCultFaction";

    /// <summary>
    /// Ссылка на активный slab, если выдан.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Slab;
}
