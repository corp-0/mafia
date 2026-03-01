using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// Single effect form: type dropdown determines which fields are visible.
/// </summary>
public partial class EffectFormSection : VBoxContainer
{
    private OptionButton _typeDropdown = null!;
    private readonly Dictionary<string, Control[]> _fieldsByType = new();

    private LineEdit _path = null!;
    private LineEdit _from = null!;
    private LineEdit _to = null!;
    private LineEdit _tag = null!;
    private LineEdit _stat = null!;
    private SpinBox _amount = null!;
    private SpinBox _value = null!;
    private LineEdit _kind = null!;
    private LineEdit _reason = null!;
    private LineEdit _eventId = null!;
    private LineEdit _rank = null!;
    private LineEdit _memoryId = null!;
    private SpinBox _expiresInDays = null!;
    private LineEdit _nickname = null!;
    private LineEdit _category = null!;
    private LineEdit _labelKey = null!;

    private readonly List<Control> _allFieldRows = [];

    private static readonly string[] EffectTypes =
    [
        "modify_stat", "set_stat",
        "add_tag", "remove_tag",
        "add_relationship", "remove_relationship",
        "disable_character", "enable_character",
        "transfer_money", "trigger_event",
        "add_memory", "remove_memory", "clear_memories",
        "change_rank", "change_nickname",
        "modify_wealth_percent", "settle_expenses",
        "add_expense"
    ];

    private EffectDto _dto = new();

    public void Initialize(EffectDto dto)
    {
        _dto = dto;
    }

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _typeDropdown = FormField.Dropdown(EffectTypes);
        _typeDropdown.ItemSelected += _ => UpdateFieldVisibility();
        AddChild(FormField.LabeledRow("Type", _typeDropdown));

        _path = FormField.TextInput("e.g. root, target");
        AddFieldRow("Path", _path,
            "modify_stat", "set_stat", "add_tag", "remove_tag",
            "disable_character", "enable_character",
            "change_rank", "change_nickname",
            "modify_wealth_percent", "settle_expenses", "add_expense");

        _from = FormField.TextInput("e.g. root");
        AddFieldRow("From", _from,
            "add_relationship", "remove_relationship",
            "transfer_money", "add_memory", "remove_memory", "clear_memories");

        _to = FormField.TextInput("e.g. target");
        AddFieldRow("To", _to,
            "add_relationship", "remove_relationship",
            "transfer_money", "add_memory", "remove_memory", "clear_memories");

        _tag = FormField.TextInput("e.g. alcoholic");
        AddFieldRow("Tag", _tag, "add_tag", "remove_tag");

        _stat = FormField.TextInput("e.g. stress, wealth");
        AddFieldRow("Stat", _stat, "modify_stat", "set_stat");

        _amount = FormField.IntInput(-99999, 99999);
        AddFieldRow("Amount", _amount,
            "modify_stat", "transfer_money", "add_memory",
            "modify_wealth_percent", "add_expense");

        _value = FormField.IntInput(-99999, 99999);
        AddFieldRow("Value", _value, "set_stat");

        _kind = FormField.TextInput("e.g. SubordinateOf");
        AddFieldRow("Kind", _kind, "add_relationship", "remove_relationship");

        _reason = FormField.TextInput("e.g. arrested");
        AddFieldRow("Reason", _reason, "disable_character", "enable_character");

        _eventId = FormField.TextInput("e.g. stress_lvl2");
        AddFieldRow("Event ID", _eventId, "trigger_event");

        _rank = FormField.TextInput("e.g. capo");
        AddFieldRow("Rank", _rank, "change_rank");

        _memoryId = FormField.TextInput("e.g. intimidated_by");
        AddFieldRow("Memory ID", _memoryId, "add_memory", "remove_memory");

        _expiresInDays = FormField.IntInput(0, 99999);
        AddFieldRow("Expires In (days)", _expiresInDays, "add_memory");

        _nickname = FormField.TextInput("e.g. The Bull");
        AddFieldRow("Nickname", _nickname, "change_nickname");

        _category = FormField.TextInput("e.g. protection, gambling");
        AddFieldRow("Category", _category, "add_expense");

        _labelKey = FormField.TextInput("expense label key");
        AddFieldRow("Label Key", _labelKey, "add_expense");

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
        var selectedType = EffectTypes[_typeDropdown.Selected >= 0 ? _typeDropdown.Selected : 0];

        foreach (var row in _allFieldRows)
            row.Visible = false;

        if (_fieldsByType.TryGetValue(selectedType, out var fields))
        {
            foreach (var field in fields)
                field.Visible = true;
        }
    }

    public void LoadFromDto(EffectDto dto)
    {
        _dto = dto;
        var typeIdx = Array.IndexOf(EffectTypes, dto.Type ?? "modify_stat");
        _typeDropdown.Selected = typeIdx >= 0 ? typeIdx : 0;

        _path.Text = dto.Path ?? "";
        _from.Text = dto.From ?? "";
        _to.Text = dto.To ?? "";
        _tag.Text = dto.Tag ?? "";
        _stat.Text = dto.Stat ?? "";
        _amount.Value = dto.Amount ?? 0;
        _value.Value = dto.Value ?? 0;
        _kind.Text = dto.Kind ?? "";
        _reason.Text = dto.Reason ?? "";
        _eventId.Text = dto.EventId ?? "";
        _rank.Text = dto.Rank ?? "";
        _memoryId.Text = dto.MemoryId ?? "";
        _expiresInDays.Value = dto.ExpiresInDays ?? 0;
        _nickname.Text = dto.Nickname ?? "";
        _category.Text = dto.Category ?? "";
        _labelKey.Text = dto.LabelKey ?? "";

        UpdateFieldVisibility();
    }

    public EffectDto WriteToDto()
    {
        var selectedType = EffectTypes[_typeDropdown.Selected >= 0 ? _typeDropdown.Selected : 0];
        var dto = new EffectDto { Type = selectedType };

        switch (selectedType)
        {
            case "modify_stat":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Stat = NullIfEmpty(_stat.Text);
                dto.Amount = (int)_amount.Value;
                break;
            case "set_stat":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Stat = NullIfEmpty(_stat.Text);
                dto.Value = (int)_value.Value;
                break;
            case "add_tag" or "remove_tag":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Tag = NullIfEmpty(_tag.Text);
                break;
            case "add_relationship" or "remove_relationship":
                dto.From = NullIfEmpty(_from.Text);
                dto.To = NullIfEmpty(_to.Text);
                dto.Kind = NullIfEmpty(_kind.Text);
                break;
            case "disable_character" or "enable_character":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Reason = NullIfEmpty(_reason.Text);
                break;
            case "transfer_money":
                dto.From = NullIfEmpty(_from.Text);
                dto.To = NullIfEmpty(_to.Text);
                dto.Amount = (int)_amount.Value;
                break;
            case "trigger_event":
                dto.Path = NullIfEmpty(_path.Text);
                dto.EventId = NullIfEmpty(_eventId.Text);
                break;
            case "add_memory":
                dto.From = NullIfEmpty(_from.Text);
                dto.To = NullIfEmpty(_to.Text);
                dto.MemoryId = NullIfEmpty(_memoryId.Text);
                dto.Amount = (int)_amount.Value;
                dto.ExpiresInDays = (int)_expiresInDays.Value;
                break;
            case "remove_memory":
                dto.From = NullIfEmpty(_from.Text);
                dto.To = NullIfEmpty(_to.Text);
                dto.MemoryId = NullIfEmpty(_memoryId.Text);
                break;
            case "clear_memories":
                dto.From = NullIfEmpty(_from.Text);
                dto.To = NullIfEmpty(_to.Text);
                break;
            case "change_rank":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Rank = NullIfEmpty(_rank.Text);
                break;
            case "change_nickname":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Nickname = NullIfEmpty(_nickname.Text);
                break;
            case "modify_wealth_percent":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Amount = (int)_amount.Value;
                break;
            case "settle_expenses":
                dto.Path = NullIfEmpty(_path.Text);
                break;
            case "add_expense":
                dto.Path = NullIfEmpty(_path.Text);
                dto.Category = NullIfEmpty(_category.Text);
                dto.Amount = (int)_amount.Value;
                dto.LabelKey = NullIfEmpty(_labelKey.Text);
                break;
        }

        return dto;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
