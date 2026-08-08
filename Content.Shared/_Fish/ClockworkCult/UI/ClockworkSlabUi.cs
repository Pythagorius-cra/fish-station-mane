using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared._Fish.ClockworkCult.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Fish.ClockworkCult.UI;

[Serializable, NetSerializable]
public enum ClockworkSlabUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ClockworkSlabBoundUserInterfaceState : BoundUserInterfaceState
{
    public int Power;
    public int Vitality;
    public int Cogs;
    public int ServantCount;
    public ClockworkArkPhase ArkPhase;
    public TimeSpan? ArkPhaseEndsAt;
    public List<ClockworkScriptureUiEntry> Scriptures = new();
    public List<string> Quickbinds = new();
    public ClockworkSlabEmpowerment Empowerment;
}

[Serializable, NetSerializable]
public sealed class ClockworkScriptureUiEntry
{
    public string Id = string.Empty;
    public string Name = string.Empty;
    public string Description = string.Empty;
    public ClockworkScriptureCategory Category;
    public int PowerCost;
    public int VitalityCost;
    public int CogUnlockCost;
    public bool Unlocked;
    public bool CanAfford;
    public bool CanUnlock;
    public TimeSpan InvokeTime;
}

[Serializable, NetSerializable]
public sealed class ClockworkSlabReciteMessage : BoundUserInterfaceMessage
{
    public string ScriptureId;

    public ClockworkSlabReciteMessage(string scriptureId)
    {
        ScriptureId = scriptureId;
    }
}

[Serializable, NetSerializable]
public sealed class ClockworkSlabUnlockMessage : BoundUserInterfaceMessage
{
    public string ScriptureId;

    public ClockworkSlabUnlockMessage(string scriptureId)
    {
        ScriptureId = scriptureId;
    }
}

[Serializable, NetSerializable]
public sealed class ClockworkSlabQuickbindMessage : BoundUserInterfaceMessage
{
    public string ScriptureId;
    public int Slot;

    public ClockworkSlabQuickbindMessage(string scriptureId, int slot)
    {
        ScriptureId = scriptureId;
        Slot = slot;
    }
}
