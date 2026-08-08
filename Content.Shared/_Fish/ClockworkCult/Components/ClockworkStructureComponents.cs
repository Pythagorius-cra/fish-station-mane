using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.ClockworkCult.Components;

/// <summary>
/// Sigil of Submission — конвертация после do-after.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkSigilSubmissionComponent : Component
{
    [DataField]
    public TimeSpan ConvertTime = TimeSpan.FromSeconds(8);

    [DataField]
    public EntProtoId EffectPrototype = "ClockworkConvertEffect";
}

/// <summary>
/// Vitality Matrix — drain/heal.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkVitalityMatrixComponent : Component
{
    [DataField]
    public float DrainInterval = 2f;

    [DataField]
    public int DrainAmount = 5;

    [DataField]
    public int HealVitalityCost = 5;

    [DataField]
    public int ReviveBaseCost = 20;

    [DataField]
    public float Accumulator;
}

/// <summary>
/// Transmission sigil — питание структур.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkTransmissionSigilComponent : Component
{
    [DataField]
    public float Range = 8f;
}

/// <summary>
/// Tinkerer's Cache — Abscond target / craft anchor.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkCacheComponent : Component
{
}

/// <summary>
/// Ark of the Clockwork Justiciar на станции.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClockworkArkComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Activated;

    [DataField]
    public int MaxIntegrityHint = 1000;
}

/// <summary>
/// Powered clockwork structure base marker.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkStructureComponent : Component
{
    [DataField]
    public int PowerDrawPerSecond = 2;

    [DataField]
    public bool RequiresTransmission;
}

/// <summary>
/// Ocular Warden turret.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkOcularWardenComponent : Component
{
    [DataField]
    public float Range = 6f;

    [DataField]
    public float FireInterval = 1.5f;

    [DataField]
    public float Accumulator;

    [DataField]
    public EntProtoId ProjectilePrototype = "BulletLaser";
}

/// <summary>
/// Prosperity Prism — toxin/stamina cleanse for servants.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkProsperityPrismComponent : Component
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public float Interval = 2f;

    [DataField]
    public float Accumulator;
}

/// <summary>
/// Interdiction Lens — slow non-servants.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkInterdictionLensComponent : Component
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public float Interval = 1f;

    [DataField]
    public float Accumulator;
}

/// <summary>
/// Replica Fabricator tool.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkFabricatorComponent : Component
{
    [DataField]
    public EntProtoId BrassPrototype = "SheetBrass1";

    [DataField]
    public int PowerPerSheet = 25;

    [DataField]
    public TimeSpan ConvertTime = TimeSpan.FromSeconds(2);
}

/// <summary>
/// Abscond anchorage — target for station Abscond.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkAbscondTargetComponent : Component
{
}
