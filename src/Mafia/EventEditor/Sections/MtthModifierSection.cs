using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// MTTH modifier list: factor + condition per entry.
/// </summary>
public partial class MtthModifierSection : CollapsibleSection
{
    private VBoxContainer _itemsContainer = null!;
    private readonly List<MtthModifierRow> _rows = [];
    private List<MtthModifierDto> _dtos = [];

    public MtthModifierSection() { Title = "MTTH Modifiers"; _expanded = false; }

    public override void _Ready()
    {
        base._Ready();

        var addBtn = new Button { Text = "+ Add MTTH Modifier" };
        addBtn.Pressed += AddNew;
        Body.AddChild(addBtn);

        _itemsContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        Body.AddChild(_itemsContainer);
    }

    public void LoadFromDtos(List<MtthModifierDto>? dtos)
    {
        _dtos = dtos != null ? new List<MtthModifierDto>(dtos) : [];
        Rebuild();
    }

    public List<MtthModifierDto>? WriteToDtos()
    {
        var result = new List<MtthModifierDto>();
        foreach (var row in _rows)
            result.Add(row.WriteToDto());
        return result.Count == 0 ? null : result;
    }

    private void AddNew()
    {
        _dtos.Add(new MtthModifierDto { Factor = 0.5 });
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

    private void SaveState()
    {
        var saved = new List<MtthModifierDto>();
        foreach (var row in _rows)
            saved.Add(row.WriteToDto());
        for (int i = 0; i < Math.Min(saved.Count, _dtos.Count); i++)
            _dtos[i] = saved[i];
    }

    private void Rebuild()
    {
        foreach (var child in _itemsContainer.GetChildren())
            child.QueueFree();
        _rows.Clear();

        for (var i = 0; i < _dtos.Count; i++)
        {
            var idx = i;
            var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            var toolbar = new HBoxContainer();
            toolbar.AddChild(new Label
            {
                Text = $"MTTH Modifier #{i + 1}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            var remove = new Button { Text = "✕", CustomMinimumSize = new Vector2(30, 0) };
            remove.Pressed += () => RemoveAt(idx);
            toolbar.AddChild(remove);
            container.AddChild(toolbar);

            var row = new MtthModifierRow();
            row.Initialize(_dtos[i]);
            _rows.Add(row);
            container.AddChild(row);

            container.AddChild(new HSeparator());
            _itemsContainer.AddChild(container);
        }
    }
}

public partial class MtthModifierRow : VBoxContainer
{
    private SpinBox _factor = null!;
    private ConditionFormSection _conditionForm = null!;
    private MtthModifierDto _dto = new();

    public void Initialize(MtthModifierDto dto)
    {
        _dto = dto;
    }

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _factor = FormField.DoubleInput(0.01, 100, 0.1);
        _factor.Value = _dto.Factor;
        AddChild(FormField.LabeledRow("Factor", _factor));

        AddChild(new Label { Text = "Condition:" });
        _conditionForm = new ConditionFormSection();
        _conditionForm.Initialize(_dto.Condition ?? new ConditionDto { Type = "stat_threshold" });
        AddChild(_conditionForm);
    }

    public MtthModifierDto WriteToDto()
    {
        return new MtthModifierDto
        {
            Factor = _factor.Value,
            Condition = _conditionForm.WriteToDto(),
        };
    }
}
