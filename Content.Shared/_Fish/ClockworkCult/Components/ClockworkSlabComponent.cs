using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.ClockworkCult.Components;

/// <summary>
/// Clockwork Slab — основной интерфейс scripture и quickbind.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClockworkSlabComponent : Component
{
    /// <summary>
    /// До 5 быстрых привязок scripture id.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> Quickbinds = new();

    [DataField]
    public int MaxQuickbinds = 5;

    /// <summary>
    /// Текущий режим empowerment для Kindle / Manacles / Compromise.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ClockworkSlabEmpowerment Empowerment = ClockworkSlabEmpowerment.None;

    /// <summary>
    /// Scripture, ожидающий завершения do-after invoke.
    /// </summary>
    [DataField]
    public string? PendingScripture;
}

public enum ClockworkSlabEmpowerment : byte
{
    None = 0,
    Kindle,
    Manacles,
    Compromise,
}
