using Robust.Shared.Serialization;

namespace Content.Shared._Fish.Skills;

/// <summary>
/// Запись навыка для EUI состояния. BUI-контракт навыков удалён — интерфейс идёт через EUI.
/// </summary>
[Serializable, NetSerializable]
public sealed class SkillUiEntry
{
    public readonly string SkillId;
    public readonly string Name;
    public readonly string Description;
    public readonly string Category;
    public readonly int CurrentLevel;
    public readonly int MinLevel;
    public readonly int MaxLevel;
    public readonly int NextLevelCost;
    public readonly bool CanIncrease;
    public readonly bool CanDecrease;
    public readonly string? BlockReason;
    public readonly string? EffectHint;

    public SkillUiEntry(
        string skillId,
        string name,
        string description,
        string category,
        int currentLevel,
        int minLevel,
        int maxLevel,
        int nextLevelCost,
        bool canIncrease,
        bool canDecrease,
        string? blockReason,
        string? effectHint = null)
    {
        SkillId = skillId;
        Name = name;
        Description = description;
        Category = category;
        CurrentLevel = currentLevel;
        MinLevel = minLevel;
        MaxLevel = maxLevel;
        NextLevelCost = nextLevelCost;
        CanIncrease = canIncrease;
        CanDecrease = canDecrease;
        BlockReason = blockReason;
        EffectHint = effectHint;
    }
}
