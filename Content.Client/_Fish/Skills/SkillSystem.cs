using Content.Shared._Fish.Skills;
using Robust.Client.Player;

namespace Content.Client._Fish.Skills;

public sealed class SkillSystem : SharedSkillSystem
{
    [Dependency] private readonly IPlayerManager _players = default!;

    public void RequestOpenSkillsUi()
    {
        if (_players.LocalEntity == null)
            return;

        RaiseNetworkEvent(new RequestOpenSkillsEuiEvent());
    }
}
