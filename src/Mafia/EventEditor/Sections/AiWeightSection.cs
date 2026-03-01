using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// AI weight: base weight + list of conditional modifiers.
/// </summary>
public partial class AiWeightSection : CollapsibleSection
{
    private SpinBox _baseWeight = null!;
    private VBoxContainer _modifiersContainer = null!;
    private readonly List<AiWeightModifierRow> _modifierRows = [];
    private List<AiWeightModifierDto> _modifiers = [];

    public AiWeightSection() { Title = "AI Weight"; }

    public override void _Ready()
    {
        base._Ready();

        _baseWeight = FormField.IntInput(0, 9999);
        Body.AddChild(FormField.LabeledRow("Base Weight", _baseWeight));

        var addModBtn = new Button { Text = "+ Add Modifier" };
        addModBtn.Pressed += AddModifier;
        Body.AddChild(addModBtn);

        _modifiersContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        Body.AddChild(_modifiersContainer);
    }

    public void LoadFromDto(AiWeightDto? dto)
    {
        _baseWeight.Value = dto?.Base ?? 10;
        _modifiers = dto?.Modifiers != null ? new List<AiWeightModifierDto>(dto.Modifiers) : [];
        RebuildModifiers();
    }

    public AiWeightDto? WriteToDto()
    {
        var mods = new List<AiWeightModifierDto>();
        foreach (var row in _modifierRows)
            mods.Add(row.WriteToDto());

        return new AiWeightDto
        {
            Base = (int)_baseWeight.Value,
            Modifiers = mods.Count > 0 ? mods : null,
        };
    }

    private void AddModifier()
    {
        _modifiers.Add(new AiWeightModifierDto());
        RebuildModifiers();
    }

    private void RemoveModifier(int index)
    {
        // Save state first
        SaveModifierState();
        if (index >= 0 && index < _modifiers.Count)
        {
            _modifiers.RemoveAt(index);
            RebuildModifiers();
        }
    }

    private void SaveModifierState()
    {
        var saved = new List<AiWeightModifierDto>();
        foreach (var row in _modifierRows)
            saved.Add(row.WriteToDto());
        for (int i = 0; i < Math.Min(saved.Count, _modifiers.Count); i++)
            _modifiers[i] = saved[i];
    }

    private void RebuildModifiers()
    {
        foreach (var child in _modifiersContainer.GetChildren())
            child.QueueFree();
        _modifierRows.Clear();

        for (var i = 0; i < _modifiers.Count; i++)
        {
            var idx = i;
            var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            var toolbar = new HBoxContainer();
            toolbar.AddChild(new Label
            {
                Text = $"Modifier #{i + 1}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            var remove = new Button { Text = "✕", CustomMinimumSize = new Vector2(30, 0) };
            remove.Pressed += () => RemoveModifier(idx);
            toolbar.AddChild(remove);
            container.AddChild(toolbar);

            var row = new AiWeightModifierRow();
            row.Initialize(_modifiers[i]);
            _modifierRows.Add(row);
            container.AddChild(row);

            container.AddChild(new HSeparator());
            _modifiersContainer.AddChild(container);
        }
    }
}

/// <summary>
/// Single AI weight modifier row: condition + add amount.
/// </summary>
public partial class AiWeightModifierRow : VBoxContainer
{
    private SpinBox _add = null!;
    private ConditionFormSection _conditionForm = null!;
    private AiWeightModifierDto _dto = new();

    public void Initialize(AiWeightModifierDto dto)
    {
        _dto = dto;
    }

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _add = FormField.IntInput(-9999, 9999);
        _add.Value = _dto.Add;
        AddChild(FormField.LabeledRow("Add", _add));

        _conditionForm = new ConditionFormSection();
        _conditionForm.Initialize(_dto.Condition ?? new ConditionDto { Type = "has_tag" });
        AddChild(_conditionForm);
    }

    public AiWeightModifierDto WriteToDto()
    {
        return new AiWeightModifierDto
        {
            Add = (int)_add.Value,
            Condition = _conditionForm.WriteToDto(),
        };
    }
}
