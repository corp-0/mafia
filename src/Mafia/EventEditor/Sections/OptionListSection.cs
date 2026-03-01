using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// List of options with add/remove/reorder. Each option expands into an OptionFormSection.
/// </summary>
public partial class OptionListSection : CollapsibleSection
{
    private VBoxContainer _itemsContainer = null!;
    private readonly List<OptionFormSection> _forms = [];
    private List<OptionDto> _dtos = [];

    public OptionListSection() { Title = "Options"; }

    public override void _Ready()
    {
        base._Ready();

        var addBtn = new Button { Text = "+ Add Option" };
        addBtn.Pressed += AddNew;
        Body.AddChild(addBtn);

        _itemsContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        Body.AddChild(_itemsContainer);
    }

    public void LoadFromDtos(List<OptionDto>? dtos)
    {
        _dtos = dtos != null ? new List<OptionDto>(dtos) : [];
        Rebuild();
    }

    public List<OptionDto>? WriteToDtos()
    {
        var result = new List<OptionDto>();
        foreach (var form in _forms)
            result.Add(form.WriteToDto());
        return result.Count == 0 ? null : result;
    }

    private void AddNew()
    {
        _dtos.Add(new OptionDto
        {
            Type = "standard",
            Id = $"option_{_dtos.Count + 1}",
            DisplayTextKey = "",
        });
        Rebuild();
    }

    private void RemoveAt(int index)
    {
        SaveState();
        if (index >= 0 && index < _dtos.Count)
        {
            _dtos.RemoveAt(index);
            Rebuild();
        }
    }

    private void MoveUp(int index)
    {
        SaveState();
        if (index <= 0) return;
        (_dtos[index - 1], _dtos[index]) = (_dtos[index], _dtos[index - 1]);
        Rebuild();
    }

    private void MoveDown(int index)
    {
        SaveState();
        if (index >= _dtos.Count - 1) return;
        (_dtos[index], _dtos[index + 1]) = (_dtos[index + 1], _dtos[index]);
        Rebuild();
    }

    private void SaveState()
    {
        var saved = new List<OptionDto>();
        foreach (var form in _forms)
            saved.Add(form.WriteToDto());
        for (int i = 0; i < Math.Min(saved.Count, _dtos.Count); i++)
            _dtos[i] = saved[i];
    }

    private void Rebuild()
    {
        foreach (var child in _itemsContainer.GetChildren())
            child.QueueFree();
        _forms.Clear();

        for (var i = 0; i < _dtos.Count; i++)
        {
            var idx = i;
            var dto = _dtos[i];
            var section = new CollapsibleSection { Title = $"Option: {dto.Id}", Expanded = false };

            // Body is null until _Ready runs, so wire up content in the callback
            section.Ready += () =>
            {
                var body = section.Body;

                var toolbar = new HBoxContainer();
                if (idx > 0)
                {
                    var up = new Button { Text = "↑", CustomMinimumSize = new Vector2(30, 0) };
                    up.Pressed += () => MoveUp(idx);
                    toolbar.AddChild(up);
                }
                if (idx < _dtos.Count - 1)
                {
                    var down = new Button { Text = "↓", CustomMinimumSize = new Vector2(30, 0) };
                    down.Pressed += () => MoveDown(idx);
                    toolbar.AddChild(down);
                }
                var remove = new Button { Text = "✕ Remove", CustomMinimumSize = new Vector2(80, 0) };
                remove.Pressed += () => RemoveAt(idx);
                toolbar.AddChild(remove);
                body.AddChild(toolbar);

                var form = new OptionFormSection();
                form.Initialize(dto);
                _forms.Add(form);
                body.AddChild(form);
            };

            _itemsContainer.AddChild(section);
        }
    }
}
