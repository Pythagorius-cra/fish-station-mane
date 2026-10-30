using Robust.Shared.GameStates;

namespace Content.Shared._Fish.Xenobiology.Extracts;

/// <summary>
/// Ядро слайма: контейнер на 100 единиц, ограниченное число использований и цвет реакции.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FishSlimeExtractComponent : Component
{
    public const int DefaultMaxUses = 5;

    /// <summary>
    /// Цвет как в оригинале: grey, dark blue, light pink.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string Color = string.Empty;

    [DataField, AutoNetworkedField]
    public int Uses = 1;

    [DataField]
    public int MaxUses = DefaultMaxUses;

    /// <summary>
    /// Оружие ядра из оригинала. Реакции экстракта его не спавнят.
    /// </summary>
    [DataField]
    public string? AssociatedGun;

    [DataField]
    public string SolutionId = "solution";

    /// <summary>
    /// Кто последним ткнул ядро шприцом. Нужен для лога взрыва oil.
    /// </summary>
    public EntityUid? LastInjector;
}
