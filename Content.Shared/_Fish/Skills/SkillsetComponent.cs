using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.Skills;

/// <summary>
/// Набор навыков сущности на текущий раунд (Bay: /datum/skillset).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSkillSystem))]
public sealed partial class SkillsetComponent : Component
{
    /// <summary>
    /// Текущие уровни навыков.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<SkillPrototype>, int> Skills = new();

    /// <summary>
    /// Очки, ещё доступные для распределения.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RemainingPoints;

    /// <summary>
    /// Стартовый бюджет (для UI и аудита).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TotalPoints;

    /// <summary>
    /// Роль, для которой выдан бюджет.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>? JobId;

    /// <summary>
    /// Бюджет уже выдан (защита от повторной выдачи).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BudgetGranted;

    /// <summary>
    /// Распределение подтверждено / закрыто для правок.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked;

    /// <summary>
    /// Черновые уровни в UI до подтверждения (уровни выше минимума).
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SkillPrototype>, int> PendingAllocation = new();
}
