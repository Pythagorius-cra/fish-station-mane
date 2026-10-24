using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Fish.Reactions;

/// <summary>
/// Включает набор физических реакций персонажа (боль, кровь, огонь, взрывы в руках)
/// и их связку с SS1984 Crit.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedFishCharacterReactionsSystem))]
public sealed partial class FishCharacterReactionsComponent : Component
{
    /// <summary>TG: 15% шанс детонации гранаты в руке от пули.</summary>
    [DataField, AutoNetworkedField]
    public float HeldExplosiveDetonateChance = 0.15f;

    /// <summary>Минимальный прирост урона для реакции на выстрел по гранате.</summary>
    [DataField, AutoNetworkedField]
    public float HeldExplosiveMinDamage = 1f;

    /// <summary>Кулдаун кровоточащего следа (сек).</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BleedTrailCooldown = TimeSpan.FromSeconds(0.75);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBleedTrail;

    /// <summary>Сколько крови переносить в раствор ног за тик следа.</summary>
    [DataField, AutoNetworkedField]
    public float BleedTrailTransfer = 1.5f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPainEmote;

    [DataField, AutoNetworkedField]
    public TimeSpan PainEmoteCooldown = TimeSpan.FromSeconds(2.5);
}
