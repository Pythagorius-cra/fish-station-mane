using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Fish.Crit;

/// <summary>
/// Включает поведение критического состояния по правилам SS1984 (Paradise complex crit).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedFishSs1984CritSystem), typeof(Content.Shared._Fish.Reactions.SharedFishCharacterReactionsSystem))]
public sealed partial class FishSs1984CritComponent : Component
{
    /// <summary>
    /// Порог урона, соответствующий Paradise HEALTH_THRESHOLD_CRIT (health &lt;= 0).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CritDamageThreshold = 100f;

    /// <summary>
    /// Порог урона, соответствующий Paradise HEALTH_THRESHOLD_DEAD (health &lt;= -100).
    /// Здесь начинается вероятностная смерть, а не мгновенная.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DeadDamageThreshold = 200f;

    /// <summary>
    /// Интервал Life()-тика Paradise (~2 секунды).
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TickInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTick;

    /// <summary>
    /// Исходный порог Death (кэш с MobThresholds; Dead не поднимаем из‑за Gib@400).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? OriginalDeadThreshold;
}

/// <summary>
/// Маркер полной бессознательности от crit-осложнений (Paralyse), блокирует soft-crit действия.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FishCritUnconsciousComponent : Component;

/// <summary>
/// Шок — осложнение SS1984 critical/shock (3 стадии).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedFishSs1984CritSystem), typeof(Content.Shared._Fish.Reactions.SharedFishCharacterReactionsSystem))]
public sealed partial class FishCritShockComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Stage = 1;

    [DataField, AutoNetworkedField]
    public int MaxStages = 3;

    /// <summary>Paradise stage_prob = 6</summary>
    [DataField, AutoNetworkedField]
    public float StageProb = 0.06f;

    /// <summary>Paradise cure_prob = 10</summary>
    [DataField, AutoNetworkedField]
    public float CureProb = 0.10f;
}

/// <summary>
/// Сердечная недостаточность — SS1984 critical/heart_failure (3 стадии).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedFishSs1984CritSystem), typeof(Content.Shared._Fish.Reactions.SharedFishCharacterReactionsSystem))]
public sealed partial class FishHeartFailureComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Stage = 1;

    [DataField, AutoNetworkedField]
    public int MaxStages = 3;

    /// <summary>Paradise stage_prob = 5</summary>
    [DataField, AutoNetworkedField]
    public float StageProb = 0.05f;

    /// <summary>Paradise cure_prob = 10</summary>
    [DataField, AutoNetworkedField]
    public float CureProb = 0.10f;
}

/// <summary>
/// Остановка сердца — SS1984 heart.beating = FALSE.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedFishSs1984CritSystem), typeof(Content.Shared._Fish.Reactions.SharedFishCharacterReactionsSystem))]
public sealed partial class FishCardiacArrestComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = true;
}

/// <summary>
/// LoseBreath — SS1984 AmountLoseBreath / AdjustLoseBreath (в секундах).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedFishSs1984CritSystem), typeof(Content.Shared._Fish.Reactions.SharedFishCharacterReactionsSystem))]
public sealed partial class FishLoseBreathComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Remaining;

    [DataField, AutoNetworkedField]
    public TimeSpan MaxRemaining = TimeSpan.FromSeconds(50);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextOxyTick;
}
