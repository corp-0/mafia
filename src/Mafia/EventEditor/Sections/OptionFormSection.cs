using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// Single option form: type dropdown (standard/skill_check/random) determines visible sub-sections.
/// </summary>
public partial class OptionFormSection : VBoxContainer
{
    private OptionButton _typeDropdown = null!;
    private LineEdit _id = null!;
    private LineEdit _displayTextKey = null!;

    // Standard outcome (flat fields)
    private OutcomeSection _standardOutcome = null!;

    // Skill check fields
    private VBoxContainer _skillCheckFields = null!;
    private LineEdit _statPath = null!;
    private LineEdit _statName = null!;
    private SpinBox _difficulty = null!;
    private OutcomeSection _successOutcome = null!;
    private OutcomeSection _failureOutcome = null!;

    // Random outcomes
    private VBoxContainer _randomFields = null!;
    private VBoxContainer _weightedOutcomesContainer = null!;
    private readonly List<WeightedOutcomeRow> _weightedOutcomeRows = [];
    private List<WeightedOutcomeDto> _weightedOutcomes = [];

    // Shared
    private AiWeightSection _aiWeight = null!;
    private ConditionFormSection? _visibilityCondition;
    private CheckBox _hasVisibilityCondition = null!;
    private VBoxContainer _visibilityContainer = null!;

    private static readonly string[] OptionTypes = ["standard", "skill_check", "random"];

    private OptionDto _dto = null!;

    public void Initialize(OptionDto dto)
    {
        _dto = dto;
    }

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _typeDropdown = FormField.Dropdown(OptionTypes);
        _typeDropdown.ItemSelected += _ => UpdateTypeVisibility();
        AddChild(FormField.LabeledRow("Option Type", _typeDropdown));

        _id = FormField.TextInput("e.g. have_a_drink");
        AddChild(FormField.LabeledRow("ID", _id));

        _displayTextKey = FormField.TextInput("Button text / localization key");
        AddChild(FormField.LabeledRow("Display Text", _displayTextKey));

        // Standard outcome
        _standardOutcome = new OutcomeSection();
        _standardOutcome.Title ="Standard Outcome";
        AddChild(_standardOutcome);

        // Skill check
        _skillCheckFields = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _statPath = FormField.TextInput("e.g. root");
        _skillCheckFields.AddChild(FormField.LabeledRow("Stat Path", _statPath));
        _statName = FormField.TextInput("e.g. nerve, charm");
        _skillCheckFields.AddChild(FormField.LabeledRow("Stat Name", _statName));
        _difficulty = FormField.IntInput(1, 30);
        _skillCheckFields.AddChild(FormField.LabeledRow("Difficulty", _difficulty));
        _successOutcome = new OutcomeSection();
        _successOutcome.Title = "Success";
        _skillCheckFields.AddChild(_successOutcome);
        _failureOutcome = new OutcomeSection();
        _failureOutcome.Title = "Failure";
        _skillCheckFields.AddChild(_failureOutcome);
        AddChild(_skillCheckFields);

        // Random outcomes
        _randomFields = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var addOutcomeBtn = new Button { Text = "+ Add Weighted Outcome" };
        addOutcomeBtn.Pressed += AddWeightedOutcome;
        _randomFields.AddChild(addOutcomeBtn);
        _weightedOutcomesContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _randomFields.AddChild(_weightedOutcomesContainer);
        AddChild(_randomFields);

        // AI Weight
        _aiWeight = new AiWeightSection();
        AddChild(_aiWeight);

        // Visibility condition
        _hasVisibilityCondition = FormField.Toggle("Has visibility condition");
        _hasVisibilityCondition.Toggled += on => _visibilityContainer.Visible = on;
        AddChild(_hasVisibilityCondition);

        _visibilityContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        AddChild(_visibilityContainer);

        LoadFromDto(_dto);
    }

    private void UpdateTypeVisibility()
    {
        var type = OptionTypes[_typeDropdown.Selected >= 0 ? _typeDropdown.Selected : 0];
        _standardOutcome.Visible = type == "standard";
        _skillCheckFields.Visible = type == "skill_check";
        _randomFields.Visible = type == "random";
    }

    public void LoadFromDto(OptionDto dto)
    {
        _dto = dto;

        // Type
        var typeStr = dto.Type ?? "standard";
        var typeIdx = Array.IndexOf(OptionTypes, typeStr);
        _typeDropdown.Selected = typeIdx >= 0 ? typeIdx : 0;

        _id.Text = dto.Id;
        _displayTextKey.Text = dto.DisplayTextKey;

        // Standard outcome - use flat fields if present, otherwise nested Outcome
        if (dto.Outcome != null)
            _standardOutcome.LoadFromDto(dto.Outcome);
        else
            _standardOutcome.LoadFlat(dto.ResolutionTextKey, dto.Effects);

        // Skill check
        _statPath.Text = dto.StatPath ?? "root";
        _statName.Text = dto.StatName ?? "";
        _difficulty.Value = dto.Difficulty ?? 10;
        _successOutcome.LoadFromDto(dto.Success);
        _failureOutcome.LoadFromDto(dto.Failure);

        // Random
        _weightedOutcomes = dto.Outcomes != null ? new List<WeightedOutcomeDto>(dto.Outcomes) : [];
        RebuildWeightedOutcomes();

        // AI Weight
        _aiWeight.LoadFromDto(dto.AiWeight);

        // Visibility
        var hasVis = dto.VisibilityConditions != null;
        _hasVisibilityCondition.ButtonPressed = hasVis;
        _visibilityContainer.Visible = hasVis;
        RebuildVisibilityCondition(dto.VisibilityConditions);

        UpdateTypeVisibility();
    }

    public OptionDto WriteToDto()
    {
        var type = OptionTypes[_typeDropdown.Selected >= 0 ? _typeDropdown.Selected : 0];

        var dto = new OptionDto
        {
            Type = type,
            Id = _id.Text,
            DisplayTextKey = _displayTextKey.Text,
            AiWeight = _aiWeight.WriteToDto(),
        };

        switch (type)
        {
            case "standard":
                // Use flat fields for simpler TOML output
                _standardOutcome.WriteFlat(dto);
                break;
            case "skill_check":
                dto.StatPath = NullIfEmpty(_statPath.Text);
                dto.StatName = NullIfEmpty(_statName.Text);
                dto.Difficulty = (int)_difficulty.Value;
                dto.Success = _successOutcome.WriteToDto();
                dto.Failure = _failureOutcome.WriteToDto();
                break;
            case "random":
                SaveWeightedOutcomeState();
                dto.Outcomes = _weightedOutcomes.Count > 0 ? new List<WeightedOutcomeDto>(_weightedOutcomes) : null;
                break;
        }

        if (_hasVisibilityCondition.ButtonPressed && _visibilityCondition != null)
            dto.VisibilityConditions = _visibilityCondition.WriteToDto();

        return dto;
    }

    private void RebuildVisibilityCondition(ConditionDto? dto)
    {
        foreach (var child in _visibilityContainer.GetChildren())
            child.QueueFree();

        _visibilityCondition = new ConditionFormSection();
        _visibilityCondition.Initialize(dto ?? new ConditionDto { Type = "has_tag" });
        _visibilityContainer.AddChild(_visibilityCondition);
    }

    // ── Weighted outcomes ──

    private void AddWeightedOutcome()
    {
        SaveWeightedOutcomeState();
        _weightedOutcomes.Add(new WeightedOutcomeDto { Weight = 50 });
        RebuildWeightedOutcomes();
    }

    private void RemoveWeightedOutcome(int index)
    {
        SaveWeightedOutcomeState();
        if (index >= 0 && index < _weightedOutcomes.Count)
        {
            _weightedOutcomes.RemoveAt(index);
            RebuildWeightedOutcomes();
        }
    }

    private void SaveWeightedOutcomeState()
    {
        var saved = new List<WeightedOutcomeDto>();
        foreach (var row in _weightedOutcomeRows)
            saved.Add(row.WriteToDto());
        for (int i = 0; i < Math.Min(saved.Count, _weightedOutcomes.Count); i++)
            _weightedOutcomes[i] = saved[i];
    }

    private void RebuildWeightedOutcomes()
    {
        foreach (var child in _weightedOutcomesContainer.GetChildren())
            child.QueueFree();
        _weightedOutcomeRows.Clear();

        for (var i = 0; i < _weightedOutcomes.Count; i++)
        {
            var idx = i;
            var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            var toolbar = new HBoxContainer();
            toolbar.AddChild(new Label
            {
                Text = $"Outcome #{i + 1}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            var remove = new Button { Text = "✕", CustomMinimumSize = new Vector2(30, 0) };
            remove.Pressed += () => RemoveWeightedOutcome(idx);
            toolbar.AddChild(remove);
            container.AddChild(toolbar);

            var row = new WeightedOutcomeRow();
            row.Initialize(_weightedOutcomes[i]);
            _weightedOutcomeRows.Add(row);
            container.AddChild(row);

            container.AddChild(new HSeparator());
            _weightedOutcomesContainer.AddChild(container);
        }
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}

/// <summary>
/// Single weighted outcome: weight + resolution_text_key + effects.
/// </summary>
public partial class WeightedOutcomeRow : VBoxContainer
{
    private SpinBox _weight = null!;
    private OutcomeSection _outcome = null!;
    private WeightedOutcomeDto _dto = new();

    public void Initialize(WeightedOutcomeDto dto)
    {
        _dto = dto;
    }

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _weight = FormField.IntInput(1, 9999);
        _weight.Value = _dto.Weight;
        AddChild(FormField.LabeledRow("Weight", _weight));

        _outcome = new OutcomeSection { Title = "Outcome" };
        AddChild(_outcome);
        _outcome.LoadFromDto(_dto);
    }

    public WeightedOutcomeDto WriteToDto()
    {
        var outcomeDto = _outcome.WriteToDto();
        return new WeightedOutcomeDto
        {
            Weight = (int)_weight.Value,
            ResolutionTextKey = outcomeDto?.ResolutionTextKey,
            Effects = outcomeDto?.Effects,
        };
    }
}
