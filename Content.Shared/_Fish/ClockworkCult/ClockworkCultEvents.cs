using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Fish.ClockworkCult;

[Serializable, NetSerializable]
public sealed partial class ClockworkScriptureInvokeDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ClockworkConvertDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ClockworkInstallCogDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ClockworkFabricateDoAfterEvent : SimpleDoAfterEvent;
