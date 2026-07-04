namespace Content.Server._Sunrise.Doors.Components;

/// <summary>
///     Маркер для блокеров мультитайловых шлюзов.
///     Игнорируется при расчёте перепада давления у пожарных шлюзов.
/// </summary>
[RegisterComponent]
public sealed partial class SunriseMultiTileAirtightBlockerComponent : Component;
