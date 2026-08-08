using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.ClockworkCult.Components;

/// <summary>
/// Integration Cog: вставляется в APC, даёт cult cogs и пассивный power.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkIntegrationCogComponent : Component
{
    [DataField]
    public TimeSpan InstallTime = TimeSpan.FromSeconds(4);

    [DataField]
    public int PowerBonusPerSecond = 5;
}

/// <summary>
/// Маркер APC с установленным cog.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkCoggedApcComponent : Component
{
    [DataField]
    public EntityUid? Installer;
}
