using Content.Shared._Fish.ClockworkCult.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._Fish.ClockworkCult.UI;

[UsedImplicitly]
public sealed class ClockworkSlabBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ClockworkSlabWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ClockworkSlabWindow>();
        _window.OnRecite += id => SendMessage(new ClockworkSlabReciteMessage(id));
        _window.OnUnlock += id => SendMessage(new ClockworkSlabUnlockMessage(id));
        _window.OnQuickbind += (id, slot) => SendMessage(new ClockworkSlabQuickbindMessage(id, slot));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is ClockworkSlabBoundUserInterfaceState ui && _window != null)
            _window.UpdateState(ui);
    }
}

public sealed class ClockworkSlabWindow : DefaultWindow
{
    public event Action<string>? OnRecite;
    public event Action<string>? OnUnlock;
    public event Action<string, int>? OnQuickbind;

    private readonly Label _resources;
    private readonly Label _ark;
    private readonly BoxContainer _list;
    private string _filter = "All";

    public ClockworkSlabWindow()
    {
        Title = Loc.GetString("clockwork-slab-ui-title");
        MinSize = new Vector2(520, 480);
        SetSize = new Vector2(560, 520);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _resources = new Label();
        _ark = new Label();
        root.AddChild(_resources);
        root.AddChild(_ark);

        var filters = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
        foreach (var cat in new[] { "All", "Servitude", "Preservation", "Structures" })
        {
            var button = new Button { Text = Loc.GetString($"clockwork-slab-ui-cat-{cat.ToLower()}") };
            var captured = cat;
            button.OnPressed += _ =>
            {
                _filter = captured;
            };
            filters.AddChild(button);
        }
        root.AddChild(filters);

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        _list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        scroll.AddChild(_list);
        root.AddChild(scroll);

        Contents.AddChild(root);
    }

    public void UpdateState(ClockworkSlabBoundUserInterfaceState state)
    {
        _resources.Text = Loc.GetString("clockwork-slab-ui-resources",
            ("power", state.Power),
            ("vitality", state.Vitality),
            ("cogs", state.Cogs),
            ("servants", state.ServantCount));

        _ark.Text = Loc.GetString("clockwork-slab-ui-ark", ("phase", state.ArkPhase.ToString()));

        _list.RemoveAllChildren();
        foreach (var entry in state.Scriptures)
        {
            if (_filter != "All" && entry.Category.ToString() != _filter)
                continue;

            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                Margin = new Thickness(0, 0, 0, 6),
            };

            row.AddChild(new Label
            {
                Text = $"{entry.Name} [{entry.Category}] P:{entry.PowerCost} V:{entry.VitalityCost} C:{entry.CogUnlockCost}",
            });
            var desc = new RichTextLabel();
            desc.SetMessage(FormattedMessage.FromMarkupOrThrow(entry.Description));
            row.AddChild(desc);

            var buttons = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
            if (!entry.Unlocked)
            {
                var unlock = new Button
                {
                    Text = Loc.GetString("clockwork-slab-ui-unlock"),
                    Disabled = !entry.CanUnlock,
                };
                var id = entry.Id;
                unlock.OnPressed += _ => OnUnlock?.Invoke(id);
                buttons.AddChild(unlock);
            }
            else
            {
                var recite = new Button
                {
                    Text = Loc.GetString("clockwork-slab-ui-recite"),
                    Disabled = !entry.CanAfford,
                };
                var id = entry.Id;
                recite.OnPressed += _ => OnRecite?.Invoke(id);
                buttons.AddChild(recite);

                for (var i = 0; i < 5; i++)
                {
                    var slot = i;
                    var qb = new Button { Text = $"{slot + 1}", MinWidth = 28 };
                    qb.OnPressed += _ => OnQuickbind?.Invoke(id, slot);
                    buttons.AddChild(qb);
                }
            }

            row.AddChild(buttons);
            _list.AddChild(row);
        }
    }
}
