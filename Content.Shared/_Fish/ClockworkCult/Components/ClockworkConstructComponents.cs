using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.ClockworkCult.Components;

/// <summary>
/// Cogscarab ghost-role construction drone.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkCogscarabComponent : Component
{
}

/// <summary>
/// Clockwork Marauder combat construct.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClockworkMarauderComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ShieldCharges = 3;

    [DataField]
    public int MaxShieldCharges = 3;

    [DataField]
    public TimeSpan ShieldRegenTime = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Eminence — ghost commander (station-adapted).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClockworkEminenceComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool MassRecallUsed;
}

/// <summary>
/// Trap trigger / skewer.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkTrapComponent : Component
{
    [DataField]
    public float StunSeconds = 3f;

    [DataField]
    public int PierceDamage = 20;

    [DataField]
    public bool Armed = true;
}

/// <summary>
/// Stargazer weapon enchant forge.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkStargazerComponent : Component
{
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan? NextReady;
}

/// <summary>
/// Observation console — warp to cache.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkObservationConsoleComponent : Component
{
}

/// <summary>
/// Clockwork rift during siege (station pressure event).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkRiftComponent : Component
{
    [DataField]
    public float Lifetime = 120f;

    [DataField]
    public float Accumulator;
}

/// <summary>
/// Armaments gear marker.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkArmamentComponent : Component
{
}
