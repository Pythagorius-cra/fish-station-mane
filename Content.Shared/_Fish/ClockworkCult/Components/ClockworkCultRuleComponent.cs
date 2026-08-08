using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._Fish.ClockworkCult.Components;

/// <summary>
/// Состояние GameRule культа Ратвара: экономика, прогресс, Ark.
/// Живёт на сущности правила; клиент видит копию через slab BUI.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClockworkCultRuleComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Power = 2500;

    [DataField]
    public int MaxPower = 100000;

    [DataField, AutoNetworkedField]
    public int Vitality;

    [DataField]
    public int MaxVitality = 10000;

    /// <summary>
    /// Очки разблокировки scripture (из Integration Cog в APC).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Cogs;

    [DataField, AutoNetworkedField]
    public List<string> UnlockedScriptures = new();

    [DataField]
    public List<string> StartingScriptures = new()
    {
        "ClockworkScriptureIntegrationCog",
        "ClockworkScriptureReplicant",
        "ClockworkScriptureReplicaFabricator",
    };

    [DataField, AutoNetworkedField]
    public ClockworkArkPhase ArkPhase = ClockworkArkPhase.Inactive;

    [DataField, AutoNetworkedField]
    public EntityUid? ArkEntity;

    [DataField, AutoNetworkedField]
    public TimeSpan? ArkPhaseEndsAt;

    [DataField]
    public int ConvertThreshold = 5;

    [DataField]
    public int ConvertCap = 12;

    [DataField, AutoNetworkedField]
    public int ServantCount;

    [DataField]
    public TimeSpan GraceDuration = TimeSpan.FromMinutes(3);

    [DataField]
    public TimeSpan SiegeDuration = TimeSpan.FromMinutes(4);

    [DataField]
    public TimeSpan AssaultDuration = TimeSpan.FromMinutes(4);

    [DataField]
    public TimeSpan CleanupDuration = TimeSpan.FromMinutes(2);

    [DataField]
    public int PowerPerCoggedApcPerSecond = 5;

    [DataField]
    public EntProtoId SlabPrototype = "ClockworkSlab";

    [DataField]
    public EntProtoId RatvarSpawnPrototype = "MobRatvarSpawn";

    [ViewVariables]
    public ClockworkCultWinCondition WinCondition = ClockworkCultWinCondition.Ongoing;

    [ViewVariables]
    public HashSet<EntityUid> Servants = new();

    [ViewVariables]
    public HashSet<EntityUid> CoggedApcs = new();
}

public enum ClockworkArkPhase : byte
{
    Inactive = 0,
    Announced,
    Grace,
    Siege,
    Assault,
    Cleanup,
    RatvarRisen,
    Destroyed,
}

public enum ClockworkCultWinCondition : byte
{
    Ongoing = 0,
    CultWin,
    CultFailure,
}
