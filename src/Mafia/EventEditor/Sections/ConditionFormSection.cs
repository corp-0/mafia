using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// Single condition form: type dropdown determines which fields are visible.
/// </summary>
public partial class ConditionFormSection : VBoxContainer
{
    private OptionButton _typeDropdown = null!;
    private readonly Dictionary<string, Control[]> _fieldsByType = new();

    // Shared fields
    private LineEdit _path = null!;
    private LineEdit _pathA = null!;
    private LineEdit _pathB = null!;
    private LineEdit _tag = null!;
    private LineEdit _stat = null!;
    private OptionButton _comparison = null!;
    private SpinBox _value = null!;
    private LineEdit _rank = null!;
    private LineEdit _from = null!;
    private LineEdit _to = null!;
    private LineEdit _kind = null!;
    private LineEdit _eventId = null!;

    // Rows (so we can show/hide)
    private readonly List<Control> _allFieldRows = [];

    private static readonly string[] ConditionTypes =
    [
        "stat_threshold", "has_tag", "has_relationship", "has_minimum_rank",
        "same_location", "event_fired", "all_of", "any_of", "none_of"
    ];

    private static readonly string[] ComparisonOps = [">=", "<=", ">", "<", "==", "!="];

    private ConditionDto _dto = new();

    /// <summary>
    /// Nested conditions list for composite types (all_of, any_of, none_of).
    /// </summary>
    private ConditionListSection? _nestedConditions;
    private int _depth;

    public void Initialize(ConditionDto dto, int depth = 0)
    {
        _dto = dto;
        _depth = depth;
    }

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _typeDropdown = FormField.Dropdown(ConditionTypes);
        _typeDropdown.ItemSelected += _ => UpdateFieldVisibility();
        AddChild(FormField.LabeledRow("Type", _typeDropdown));

        // Create all possible fields
        _path = FormField.TextInput("e.g. root, target");
        AddFieldRow("Path", _path, "stat_threshold", "has_tag", "has_minimum_rank", "event_fired");

        _pathA = FormField.TextInput("e.g. root");
        AddFieldRow("Path A", _pathA, "same_location");

        _pathB = FormField.TextInput("e.g. target");
        AddFieldRow("Path B", _pathB, "same_location");

        _tag = FormField.TextInput("e.g. alcoholic");
        AddFieldRow("Tag", _tag, "has_tag");

        _stat = FormField.TextInput("e.g. stress, wealth");
        AddFieldRow("Stat", _stat, "stat_threshold");

        _comparison = FormField.Dropdown(ComparisonOps);
        AddFieldRow("Comparison", _comparison, "stat_threshold", "event_fired");

        _value = FormField.IntInput(-99999, 99999);
        AddFieldRow("Value", _value, "stat_threshold", "event_fired");

        _rank = FormField.TextInput("e.g. soldier, capo");
        AddFieldRow("Rank", _rank, "has_minimum_rank");

        _from = FormField.TextInput("e.g. root");
        AddFieldRow("From", _from, "has_relationship");

        _to = FormField.TextInput("e.g. target");
        AddFieldRow("To", _to, "has_relationship");

        _kind = FormField.TextInput("e.g. SubordinateOf");
        AddFieldRow("Kind", _kind, "has_relationship");

        _eventId = FormField.TextInput("e.g. stress_lvl1");
        AddFieldRow("Event ID", _eventId, "event_fired");

        // Nested conditions for composite types (capped at depth 3)
        if (_depth < 3)
        {
            _nestedConditions = new ConditionListSection();
            _nestedConditions.Initialize(_depth + 1);
            _nestedConditions.Visible = false;
            AddChild(_nestedConditions);
            _fieldsByType["all_of"] = [_nestedConditions];
            _fieldsByType["any_of"] = [_nestedConditions];
            _fieldsByType["none_of"] = [_nestedConditions];
        }

        LoadFromDto(_dto);
    }

    private void AddFieldRow(string label, Control control, params string[] types)
    {
        var row = FormField.LabeledRow(label, control);
        row.Visible = false;
        AddChild(row);
        _allFieldRows.Add(row);

        foreach (var type in types)
        {
            if (!_fieldsByType.ContainsKey(type))
                _fieldsByType[type] = [];
            _fieldsByType[type] = [.. _fieldsByType[type], row];
        }
    }

    private void UpdateFieldVisibility()
    {
        var selectedType = ConditionTypes[_typeDropdown.Selected >= 0 ? _typeDropdown.Selected : 0];

        // Hide all
        foreach (var row in _allFieldRows)
            row.Visible = false;
        if (_nestedConditions != null)
            _nestedConditions.Visible = false;

        // Show relevant ones
        if (_fieldsByType.TryGetValue(selectedType, out var fields))
        {
            foreach (var field in fields)
                field.Visible = true;
        }
    }

    public void LoadFromDto(ConditionDto dto)
    {
        _dto = dto;
        var typeIdx = Array.IndexOf(ConditionTypes, dto.Type ?? "stat_threshold");
        _typeDropdown.Selected = typeIdx >= 0 ? typeIdx : 0;

        _path.Text = dto.Path ?? "";
        _pathA.Text = dto.PathA ?? "";
        _pathB.Text = dto.PathB ?? "";
        _tag.Text = dto.Tag ?? "";
        _stat.Text = dto.Stat ?? "";
        SelectComparison(dto.Comparison);
        _value.Value = dto.Value ?? 0;
        _rank.Text = dto.Rank ?? "";
        _from.Text = dto.From ?? "";
        _to.Text = dto.To ?? "";
        _kind.Text = dto.Kind ?? "";
        _eventId.Text = dto.EventId ?? "";

        _nestedConditions?.LoadFromDtos(dto.Conditions);

        UpdateFieldVisibility();
    }

    public ConditionDto WriteToDto()
    {
        var selectedType = ConditionTypes[_typeDropdown.Selected >= 0 ? _typeDropdown.Selected : 0];

        var dto = new ConditionDto { Type = selectedType };

        switch (selectedType)
        {
            case "stat_threshold":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Stat = NullIfEmpty(_stat.Text);
                dto.Comparison = ComparisonOps[_comparison.Selected >= 0 ? _comparison.Selected : 0];
                dto.Value = (int)_value.Value;
                break;
            case "has_tag":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Tag = NullIfEmpty(_tag.Text);
                break;
            case "has_relationship":
                dto.From = NullIfEmpty(_from.Text);
                dto.To = NullIfEmpty(_to.Text);
                dto.Kind = NullIfEmpty(_kind.Text);
                break;
            case "has_minimum_rank":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Rank = NullIfEmpty(_rank.Text);
                break;
            case "same_location":
                dto.PathA = NullIfEmpty(_pathA.Text);
                dto.PathB = NullIfEmpty(_pathB.Text);
                break;
            case "event_fired":
                dto.Path = NullIfEmpty(_path.Text);
                dto.EventId = NullIfEmpty(_eventId.Text);
                dto.Comparison = ComparisonOps[_comparison.Selected >= 0 ? _comparison.Selected : 0];
                dto.Value = (int)_value.Value;
                break;
            case "all_of" or "any_of" or "none_of":
                dto.Conditions = _nestedConditions?.WriteToDtos();
                break;
        }

        return dto;
    }

    private void SelectComparison(string? comparison)
    {
        if (comparison == null) { _comparison.Selected = 0; return; }
        var idx = Array.IndexOf(ComparisonOps, comparison);
        _comparison.Selected = idx >= 0 ? idx : 0;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
