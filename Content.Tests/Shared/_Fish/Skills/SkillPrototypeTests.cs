using System.Collections.Generic;
using Content.Shared._Fish.Skills;
using NUnit.Framework;

namespace Content.Tests.Shared._Fish.Skills;

[TestFixture]
[TestOf(typeof(SkillPrototype))]
public sealed class SkillPrototypeTests
{
    // RA0039: не создаём SkillPrototype напрямую — проверяем формулы через статический хелпер.

    [Test]
    public void GetCostMatchesBayFormula()
    {
        Assert.That(SkillMath.GetCostForLevel(SkillLevels.DifficultyEasy, SkillLevels.Basic), Is.EqualTo(1));
        Assert.That(SkillMath.GetCostForLevel(SkillLevels.DifficultyEasy, SkillLevels.Trained), Is.EqualTo(1));
        Assert.That(SkillMath.GetCostForLevel(SkillLevels.DifficultyEasy, SkillLevels.Experienced), Is.EqualTo(2));
        Assert.That(SkillMath.GetCostForLevel(SkillLevels.DifficultyEasy, SkillLevels.Master), Is.EqualTo(2));

        Assert.That(SkillMath.GetLevelCost(SkillLevels.DifficultyHard, SkillLevels.Unskilled, SkillLevels.Trained), Is.EqualTo(8));
        Assert.That(SkillMath.GetLevelCost(SkillLevels.DifficultyHard, SkillLevels.Unskilled, SkillLevels.Master), Is.EqualTo(24));
    }

    [Test]
    public void LevelCostIsZeroWhenNotIncreasing()
    {
        Assert.That(SkillMath.GetLevelCost(SkillLevels.DifficultyAverage, 3, 3), Is.EqualTo(0));
        Assert.That(SkillMath.GetLevelCost(SkillLevels.DifficultyAverage, 4, 2), Is.EqualTo(0));
    }

    [Test]
    public void DurationMultipliersMatchSs1984StyleTables()
    {
        var table = new Dictionary<int, float>
        {
            [SkillLevels.Unskilled] = 1.5f,
            [SkillLevels.Basic] = 1.25f,
            [SkillLevels.Trained] = 1.0f,
            [SkillLevels.Experienced] = 0.8f,
            [SkillLevels.Master] = 0.65f,
        };

        Assert.That(SkillMath.GetDurationMultiplier(table, 0.3f, SkillLevels.Unskilled), Is.EqualTo(1.5f));
        Assert.That(SkillMath.GetDurationMultiplier(table, 0.3f, SkillLevels.Trained), Is.EqualTo(1.0f));
        Assert.That(SkillMath.GetDurationMultiplier(table, 0.3f, SkillLevels.Master), Is.EqualTo(0.65f));
    }

    [Test]
    public void QualityMultipliersMatchSs1984HealAmountStyle()
    {
        var table = new Dictionary<int, float>
        {
            [SkillLevels.Unskilled] = 0.75f,
            [SkillLevels.Basic] = 0.9f,
            [SkillLevels.Trained] = 1.0f,
            [SkillLevels.Experienced] = 1.2f,
            [SkillLevels.Master] = 1.35f,
        };

        Assert.That(SkillMath.GetQualityMultiplier(table, 0.15f, SkillLevels.Unskilled), Is.EqualTo(0.75f));
        Assert.That(SkillMath.GetQualityMultiplier(table, 0.15f, SkillLevels.Master), Is.EqualTo(1.35f));
    }

    [Test]
    public void BayDelayFormulaUsedWhenNoDurationTable()
    {
        var empty = new Dictionary<int, float>();
        Assert.That(SkillMath.GetDurationMultiplier(empty, 0.3f, SkillLevels.Unskilled), Is.EqualTo(1.6f).Within(0.0001f));
        Assert.That(SkillMath.GetDurationMultiplier(empty, 0.3f, SkillLevels.Trained), Is.EqualTo(1.0f).Within(0.0001f));
        Assert.That(SkillMath.GetDurationMultiplier(empty, 0.3f, SkillLevels.Master), Is.EqualTo(0.4f).Within(0.0001f));
    }
}

