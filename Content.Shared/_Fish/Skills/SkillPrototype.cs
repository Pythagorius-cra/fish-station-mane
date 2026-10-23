using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.Skills;

/// <summary>
/// Прототип навыка (Bay /singleton/hierarchy/skill + таблицы модификаторов в духе SS1984).
/// </summary>
[Prototype]
public sealed partial class SkillPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = string.Empty;

    [DataField]
    public LocId Description = string.Empty;

    /// <summary>
    /// Категория для группировки в UI (engineering, medical, ...).
    /// </summary>
    [DataField]
    public string Category = "general";

    /// <summary>
    /// Сложность навыка: влияет на стоимость уровней (Bay get_cost).
    /// </summary>
    [DataField]
    public int Difficulty = SkillLevels.DifficultyAverage;

    /// <summary>
    /// Максимум по умолчанию, если у роли нет своего max.
    /// </summary>
    [DataField]
    public int DefaultMax = SkillLevels.Trained;

    /// <summary>
    /// Предварительные требования: skillId → минимальный уровень.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SkillPrototype>, int> Prerequisites = new();

    /// <summary>
    /// Коэффициент Bay skill_delay_mult, если DurationMultipliers пусты.
    /// </summary>
    [DataField]
    public float DelayFactor = 0.3f;

    /// <summary>
    /// Коэффициент прироста качества (HEAL_AMOUNT / SURGERY_SUCCESS), если QualityMultipliers пусты.
    /// </summary>
    [DataField]
    public float QualityFactor = 0.15f;

    /// <summary>
    /// Явные множители длительности по уровню (SS1984 speed_modifiers / HEAL_DURATION_MOD).
    /// Ключ — уровень навыка Fish (1..5). Пустой словарь → формула Bay.
    /// </summary>
    [DataField]
    public Dictionary<int, float> DurationMultipliers = new();

    /// <summary>
    /// Явные множители качества/эффективности по уровню (SS1984 quality / HEAL_AMOUNT_MOD).
    /// </summary>
    [DataField]
    public Dictionary<int, float> QualityMultipliers = new();

    /// <summary>
    /// Стоимость повышения ДО указанного уровня (Bay: get_cost(level)).
    /// </summary>
    public int GetCostForLevel(int level) => SkillMath.GetCostForLevel(Difficulty, level);

    /// <summary>
    /// Суммарная стоимость перехода с fromLevel до toLevel (включительно шаги вверх).
    /// </summary>
    public int GetLevelCost(int fromLevel, int toLevel) => SkillMath.GetLevelCost(Difficulty, fromLevel, toLevel);

    /// <summary>
    /// Множитель длительности действия для уровня (меньше = быстрее).
    /// </summary>
    public float GetDurationMultiplierForLevel(int level) =>
        SkillMath.GetDurationMultiplier(DurationMultipliers, DelayFactor, level);

    /// <summary>
    /// Множитель качества/эффективности для уровня (больше = сильнее эффект).
    /// </summary>
    public float GetQualityMultiplierForLevel(int level) =>
        SkillMath.GetQualityMultiplier(QualityMultipliers, QualityFactor, level);
}

