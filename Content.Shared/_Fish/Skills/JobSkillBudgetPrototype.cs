using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.Skills;

/// <summary>
/// Бюджет очков и min/max навыков для роли (Bay: job.skill_points / min_skill / max_skill).
/// </summary>
[Prototype]
public sealed partial class JobSkillBudgetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Id профессии. Если не задан — совпадает с ID прототипа.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// Свободные очки сверх минимумов (Bay skill_points, default 22).
    /// </summary>
    [DataField]
    public int SkillPoints = 22;

    /// <summary>
    /// Минимальные уровни навыков (бесплатно).
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SkillPrototype>, int> MinSkills = new();

    /// <summary>
    /// Максимальные уровни навыков для роли.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SkillPrototype>, int> MaxSkills = new();

    public ProtoId<JobPrototype> GetJobId() => Job ?? ID;
}
