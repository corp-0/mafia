using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// Trigger-type-specific fields. Shows/hides fields based on the selected trigger type.
/// Pulse: MTTH. OnAction: action ID. StoryBeat: story date.
/// </summary>
public partial class TriggerFieldsSection : CollapsibleSection
{
    private OptionButton _triggerType = null!;

    // Pulse fields
    private HBoxContainer _mtthRow = null!;
    private SpinBox _mtthDays = null!;
    private MtthModifierSection _mtthModifiers = null!;

    // OnAction fields
    private HBoxContainer _actionRow = null!;
    private LineEdit _onActionId = null!;

    // StoryBeat fields
    private HBoxContainer _storyDateRow = null!;
    private LineEdit _storyDate = null!;

    private static readonly string[] TriggerTypes = ["pulse", "on_action", "story_beat", "chained"];

    public string SelectedTriggerType => TriggerTypes[_triggerType.Selected >= 0 ? _triggerType.Selected : 0];

    public event Action<string>? TriggerTypeChanged;

    public TriggerFieldsSection() { Title = "Trigger"; }

    public override void _Ready()
    {
        base._Ready();

        _triggerType = FormField.Dropdown(TriggerTypes);
        _triggerType.ItemSelected += _ => UpdateVisibility();
        Body.AddChild(FormField.LabeledRow("Trigger Type", _triggerType));

        // Pulse
        _mtthDays = FormField.DoubleInput(0.1, 99999, 1);
        _mtthRow = FormField.LabeledRow("MTTH (days)", _mtthDays);
        Body.AddChild(_mtthRow);

        _mtthModifiers = new MtthModifierSection();
        Body.AddChild(_mtthModifiers);

        // OnAction
        _onActionId = FormField.TextInput("e.g. order_hit");
        _actionRow = FormField.LabeledRow("On Action ID", _onActionId);
        Body.AddChild(_actionRow);

        // StoryBeat
        _storyDate = FormField.TextInput("YYYY-MM-DD");
        _storyDateRow = FormField.LabeledRow("Story Date", _storyDate);
        Body.AddChild(_storyDateRow);

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        var type = SelectedTriggerType;
        _mtthRow.Visible = type == "pulse";
        _mtthModifiers.Visible = type == "pulse";
        _actionRow.Visible = type == "on_action";
        _storyDateRow.Visible = type == "story_beat";
        TriggerTypeChanged?.Invoke(type);
    }

    public void LoadFromDto(EventDto dto)
    {
        var idx = Array.IndexOf(TriggerTypes, dto.TriggerType ?? "pulse");
        _triggerType.Selected = idx >= 0 ? idx : 0;

        _mtthDays.Value = dto.MeanTimeToHappenDays ?? 30;
        _mtthModifiers.LoadFromDtos(dto.MtthModifiers);
        _onActionId.Text = dto.OnActionId ?? "";
        _storyDate.Text = dto.StoryDate ?? "";

        UpdateVisibility();
    }

    public void WriteToDto(EventDto dto)
    {
        dto.TriggerType = SelectedTriggerType;

        switch (SelectedTriggerType)
        {
            case "pulse":
                dto.MeanTimeToHappenDays = _mtthDays.Value;
                dto.MtthModifiers = _mtthModifiers.WriteToDtos();
                dto.OnActionId = null;
                dto.StoryDate = null;
                break;
            case "on_action":
                dto.OnActionId = _onActionId.Text;
                dto.MeanTimeToHappenDays = null;
                dto.StoryDate = null;
                break;
            case "story_beat":
                dto.StoryDate = _storyDate.Text;
                dto.MeanTimeToHappenDays = null;
                dto.OnActionId = null;
                break;
            case "chained":
                dto.MeanTimeToHappenDays = null;
                dto.OnActionId = null;
                dto.StoryDate = null;
                break;
        }
    }
}
