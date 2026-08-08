using Content.Shared._Fish.ClockworkCult.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.ClockworkCult.Prototypes;

/// <summary>
/// Data-driven scripture запись для slab UI и invoke pipeline.
/// </summary>
[Prototype]
public sealed partial class ClockworkScripturePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = string.Empty;

    [DataField]
    public LocId Description = string.Empty;

    [DataField(required: true)]
    public ClockworkScriptureCategory Category;

    [DataField]
    public int PowerCost;

    [DataField]
    public int VitalityCost;

    /// <summary>
    /// Стоимость разблокировки в cogs. 0 = стартовая / всегда доступна после старта.
    /// </summary>
    [DataField]
    public int CogUnlockCost;

    [DataField]
    public TimeSpan InvokeTime = TimeSpan.FromSeconds(5);

    [DataField]
    public int InvokerCount = 1;

    [DataField]
    public bool StartsUnlocked;

    /// <summary>
    /// Прототип сущности-эффекта / структуры, которую создаёт scripture (если применимо).
    /// </summary>
    [DataField]
    public EntProtoId? SpawnPrototype;

    /// <summary>
    /// Ключ эффекта для handler registry.
    /// </summary>
    [DataField(required: true)]
    public string Effect = string.Empty;

    [DataField]
    public ClockworkSlabEmpowerment? SetsEmpowerment;
}

public enum ClockworkScriptureCategory : byte
{
    Servitude = 0,
    Preservation = 1,
    Structures = 2,
}
