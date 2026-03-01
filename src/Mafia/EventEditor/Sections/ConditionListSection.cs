using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// List of condition entries with add/remove. Wraps DtoListControl for ConditionDto.
/// </summary>
public partial class ConditionListSection : CollapsibleSection
{
    private int _depth;
    private VBoxContainer _itemsContainer = null!;
    private readonly List<ConditionFormSection> _forms = [];
    private List<ConditionDto> _dtos = [];

    public ConditionListSection() { Title = "Conditions"; }

    public void Initialize(int depth)
    {
        _depth = depth;
    }

    public override void _Ready()
    {
        base._Ready();

        var addBtn = new Button { Text = "+ Add Condition" };
        addBtn.Pressed += AddNew;
        Body.AddChild(addBtn);

        _itemsContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        Body.AddChild(_itemsContainer);
    }

    public void LoadFromDtos(List<ConditionDto>? dtos)
    {
        _dtos = dtos != null ? new List<ConditionDto>(dtos) : [];
        Rebuild();
    }

    public List<ConditionDto>? WriteToDtos()
    {
        var result = new List<ConditionDto>();
        foreach (var form in _forms)
            result.Add(form.WriteToDto());
        return result.Count == 0 ? null : result;
    }

    private void AddNew()
    {
        SaveFormState();
        _dtos.Add(new ConditionDto { Type = "stat_threshold" });
        Rebuild();
    }

    private void RemoveAt(int index)
    {
        SaveFormState();
        if (index < 0 || index >= _dtos.Count) return;
        _dtos.RemoveAt(index);
        Rebuild();
    }

    private void MoveUp(int index)
    {
        SaveFormState();
        if (index <= 0) return;
        (_dtos[index - 1], _dtos[index]) = (_dtos[index], _dtos[index - 1]);
        Rebuild();
    }

    private void MoveDown(int index)
    {
        SaveFormState();
        if (index >= _dtos.Count - 1) return;
        (_dtos[index], _dtos[index + 1]) = (_dtos[index + 1], _dtos[index]);
        Rebuild();
    }

    private void SaveFormState()
    {
        for (int i = 0; i < Math.Min(_forms.Count, _dtos.Count); i++)
            _dtos[i] = _forms[i].WriteToDto();
    }

    private void Rebuild()
    {
        foreach (var child in _itemsContainer.GetChildren())
            child.QueueFree();
        _forms.Clear();

        for (var i = 0; i < _dtos.Count; i++)
        {
            var idx = i;
            var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            var toolbar = new HBoxContainer();
            toolbar.AddChild(new Label
            {
                Text = $"Condition #{i + 1}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            });

            if (i > 0)
            {
                var up = new Button { Text = "↑", CustomMinimumSize = new Vector2(30, 0) };
                up.Pressed += () => MoveUp(idx);
                toolbar.AddChild(up);
            }
            if (i < _dtos.Count - 1)
            {
                var down = new Button { Text = "↓", CustomMinimumSize = new Vector2(30, 0) };
                down.Pressed += () => MoveDown(idx);
                toolbar.AddChild(down);
            }
            var remove = new Button { Text = "✕", CustomMinimumSize = new Vector2(30, 0) };
            remove.Pressed += () => RemoveAt(idx);
            toolbar.AddChild(remove);

            container.AddChild(toolbar);

            var form = new ConditionFormSection();
            form.Initialize(_dtos[i], _depth);
            _forms.Add(form);
            container.AddChild(form);

            container.AddChild(new HSeparator());
            _itemsContainer.AddChild(container);
        }
    }
}
