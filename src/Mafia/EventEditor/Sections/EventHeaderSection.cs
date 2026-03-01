using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// Form section for event header fields: id, title_key, description_key, scope, flags.
/// </summary>
public partial class EventHeaderSection : CollapsibleSection
{
    private LineEdit _id = null!;
    private LineEdit _titleKey = null!;
    private TextEdit _descriptionKey = null!;
    private OptionButton _scopeType = null!;
    private CheckBox _isOneTimeOnly = null!;
    private SpinBox _cooldownDays = null!;
    private SpinBox _priority = null!;
    private OptionButton _presentation = null!;

    private static readonly string[] ScopeTypes = ["character", "location", "relationship", "global"];
    private static readonly string[] PresentationTypes = ["popup", "notification"];

    public EventHeaderSection() { Title = "Event Header"; }

    public override void _Ready()
    {
        base._Ready();

        _id = FormField.TextInput("e.g. stress_lvl1");
        Body.AddChild(FormField.LabeledRow("ID", _id, "Unique id for this event"));

        _titleKey = FormField.TextInput("Event title");
        Body.AddChild(FormField.LabeledRow("Title Key", _titleKey, "Use plain english while dev. Otherwise use a locale key"));

        _descriptionKey = FormField.TextAreaInput("Event description. This is what players will see");
        Body.AddChild(FormField.LabeledRow("Description Key", _descriptionKey, "Use plain english while dev. Otherwise use a locale key"));

        _scopeType = FormField.Dropdown(ScopeTypes);
        Body.AddChild(FormField.LabeledRow("Scope", _scopeType,
            "What entity type is the 'root' of this event.\n" +
            "character: fires once per alive character.\n" +
            "location: fires once per territory/location.\n" +
            "relationship: fires once per relationship pair.\n" +
            "global: fires once with no specific root entity."));

        _isOneTimeOnly = FormField.Toggle("One-time only");
        Body.AddChild(FormField.LabeledRow("Flags", _isOneTimeOnly));

        _cooldownDays = FormField.IntInput(0, 99999);
        Body.AddChild(FormField.LabeledRow("Cooldown (days)", _cooldownDays));

        _priority = FormField.IntInput(0, 9999);
        Body.AddChild(FormField.LabeledRow("Priority", _priority));

        _presentation = FormField.Dropdown(PresentationTypes);
        Body.AddChild(FormField.LabeledRow("Presentation", _presentation));
    }

    public void LoadFromDto(EventDto dto)
    {
        _id.Text = dto.Id;
        _titleKey.Text = dto.TitleKey;
        _descriptionKey.Text = dto.DescriptionKey;
        SelectDropdown(_scopeType, dto.ScopeType, ScopeTypes);
        _isOneTimeOnly.ButtonPressed = dto.IsOneTimeOnly;
        _cooldownDays.Value = dto.CooldownDays;
        _priority.Value = dto.Priority;
        SelectDropdown(_presentation, dto.Presentation, PresentationTypes);
    }

    public void WriteToDto(EventDto dto)
    {
        dto.Id = _id.Text;
        dto.TitleKey = _titleKey.Text;
        dto.DescriptionKey = _descriptionKey.Text;
        dto.ScopeType = ScopeTypes[_scopeType.Selected >= 0 ? _scopeType.Selected : 0];
        dto.IsOneTimeOnly = _isOneTimeOnly.ButtonPressed;
        dto.CooldownDays = (int)_cooldownDays.Value;
        dto.Priority = (int)_priority.Value;
        dto.Presentation = PresentationTypes[_presentation.Selected >= 0 ? _presentation.Selected : 0];
    }

    private static void SelectDropdown(OptionButton dropdown, string? value, string[] options)
    {
        if (value == null) { dropdown.Selected = 0; return; }
        var idx = Array.IndexOf(options, value);
        dropdown.Selected = idx >= 0 ? idx : 0;
    }
}
