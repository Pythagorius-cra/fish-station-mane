namespace Content.Shared._Fish.Skills;

/// <summary>
/// Формулы стоимости и модификаторов навыков (Bay + таблицы SS1984).
/// Вынесено из прототипа, чтобы unit-тесты не создавали IPrototype напрямую (RA0039).
/// </summary>
public static class SkillMath
{
    public static int GetCostForLevel(int difficulty, int level)
    {
        return level switch
        {
            SkillLevels.Basic or SkillLevels.Trained => difficulty,
            SkillLevels.Experienced or SkillLevels.Master => 2 * difficulty,
            _ => 0,
        };
    }

    public static int GetLevelCost(int difficulty, int fromLevel, int toLevel)
    {
        if (toLevel <= fromLevel)
            return 0;

        var sum = 0;
        for (var i = fromLevel + 1; i <= toLevel; i++)
            sum += GetCostForLevel(difficulty, i);

        return sum;
    }

    public static float GetDurationMultiplier(IReadOnlyDictionary<int, float> table, float delayFactor, int level)
    {
        level = Math.Clamp(level, SkillLevels.Min, SkillLevels.Max);

        if (table.TryGetValue(level, out var explicitMult))
            return Math.Max(0f, explicitMult);

        return Math.Max(0f, 1f + (SkillLevels.Baseline - level) * delayFactor);
    }

    public static float GetQualityMultiplier(IReadOnlyDictionary<int, float> table, float qualityFactor, int level)
    {
        level = Math.Clamp(level, SkillLevels.Min, SkillLevels.Max);

        if (table.TryGetValue(level, out var explicitMult))
            return Math.Max(0.001f, explicitMult);

        return Math.Max(0.05f, 1f + (level - SkillLevels.Baseline) * qualityFactor);
    }
}
