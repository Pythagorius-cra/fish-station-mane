using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Fish.Xenobiology.Slimes;

/// <summary>
/// Тело слайма SS220: голод, рост, ядра, дисциплина. Логика на сервере.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class FishSlimeComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ColorId = "grey";

    [DataField, AutoNetworkedField]
    public bool IsAdult;

    [DataField, AutoNetworkedField]
    public float Nutrition = 700f;

    [DataField, AutoNetworkedField]
    public int AmountGrown;

    [DataField, AutoNetworkedField]
    public int MutationChance = FishSlimeMutation.DefaultMutationChance;

    [DataField, AutoNetworkedField]
    public int Cores = 1;

    [DataField, AutoNetworkedField]
    public int PowerLevel;

    [DataField, AutoNetworkedField]
    public bool Rabid;

    [DataField, AutoNetworkedField]
    public bool Docile;

    [DataField, AutoNetworkedField]
    public bool Trained;

    [DataField, AutoNetworkedField]
    public bool MutatorUsed;

    [DataField, AutoNetworkedField]
    public int Discipline;

    [DataField, AutoNetworkedField]
    public int Attacked;

    /// <summary>
    /// Жертва, к которой слайм присосался.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? FeedTarget;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextLife;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextAi;

    /// <summary>
    /// Последняя температура окружения с <see cref="Content.Shared.Temperature.TemperatureChangedEvent"/>.
    /// </summary>
    [AutoNetworkedField]
    public float AmbientTemperature = 293.15f;

    /// <summary>
    /// Множитель задержки: морфин ×2, frostoil ×5.
    /// </summary>
    [AutoNetworkedField]
    public float SlowMultiplier = 1f;

    public bool DeathHandled;

    public float MaxNutrition => IsAdult ? 1200f : 1000f;
    public float GrowNutrition => IsAdult ? 1000f : 800f;
    public float HungerNutrition => IsAdult ? 600f : 500f;
    public float StarveNutrition => IsAdult ? 300f : 200f;
}
