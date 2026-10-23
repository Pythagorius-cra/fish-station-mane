using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Fish.Skills;

[Serializable, NetSerializable]
public enum SkillUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SkillBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly int RemainingPoints;
    public readonly int TotalPoints;
    public readonly bool Locked;
    public readonly string? JobName;
    public readonly string? StatusMessage;
    public readonly List<SkillUiEntry> Skills;

    public SkillBoundUserInterfaceState(
        int remainingPoints,
        int totalPoints,
        bool locked,
        string? jobName,
        string? statusMessage,
        List<SkillUiEntry> skills)
    {
        RemainingPoints = remainingPoints;
        TotalPoints = totalPoints;
        Locked = locked;
        JobName = jobName;
        StatusMessage = statusMessage;
        Skills = skills;
    }
}

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

[Serializable, NetSerializable]
public sealed class SkillSetLevelMessage : BoundUserInterfaceMessage
{
    public readonly string SkillId;
    public readonly int Level;

    public SkillSetLevelMessage(string skillId, int level)
    {
        SkillId = skillId;
        Level = level;
    }
}

[Serializable, NetSerializable]
public sealed class SkillConfirmMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SkillResetMessage : BoundUserInterfaceMessage;
