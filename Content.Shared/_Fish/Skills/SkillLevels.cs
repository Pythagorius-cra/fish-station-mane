namespace Content.Shared._Fish.Skills;

/// <summary>
/// Уровни навыков по модели Baystation12 (SKILL_UNSKILLED..SKILL_MASTER).
/// </summary>
public static class SkillLevels
{
    public const int Unskilled = 1;
    public const int Basic = 2;
    public const int Trained = 3;
    public const int Experienced = 4;
    public const int Master = 5;

    public const int Min = Unskilled;
    public const int Max = Master;

    /// <summary>Базовый уровень для расчёта множителя времени (SKILL_BASELINE).</summary>
    public const int Baseline = Trained;

    /// <summary>Сложность навыка: SKILL_EASY.</summary>
    public const int DifficultyEasy = Unskilled;

    /// <summary>Сложность навыка: SKILL_AVERAGE.</summary>
    public const int DifficultyAverage = Basic;

    /// <summary>Сложность навыка: SKILL_HARD.</summary>
    public const int DifficultyHard = Experienced;

    public static string GetLevelNameKey(int level) => level switch
    {
        Unskilled => "skills-level-unskilled",
        Basic => "skills-level-basic",
        Trained => "skills-level-trained",
        Experienced => "skills-level-experienced",
        Master => "skills-level-master",
        _ => "skills-level-unknown",
    };
}
