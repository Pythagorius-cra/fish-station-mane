using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Fish.Skills;

/// <summary>
/// Учебник: изучение повышает навык на один уровень (до MaxTrainLevel).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSkillSystem))]
public sealed partial class SkillTrainingManualComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<SkillPrototype> Skill;

    /// <summary>
    /// Уровень, до которого обучает эта книга (игрок должен быть на target-1).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TargetLevel = SkillLevels.Basic;

    /// <summary>
    /// Минимальный текущий уровень навыка, чтобы понять материал (обычно TargetLevel - 1).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinSkillToStudy = SkillLevels.Unskilled;

    [DataField, AutoNetworkedField]
    public TimeSpan StudyTime = TimeSpan.FromSeconds(45);

    /// <summary>
    /// Можно ли уничтожить книгу после успешного изучения.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ConsumeOnStudy;
}

