using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Sections;

namespace Mafia.EventEditor;

/// <summary>
/// Right panel form: owns all sections, delegates load/save via DTO.
/// </summary>
public partial class EventEditorForm : VBoxContainer
{
    private EventHeaderSection _header = null!;
    private TriggerFieldsSection _trigger = null!;
    private TargetSelectionSection _targetSelection = null!;
    private ConditionListSection _conditions = null!;
    private OptionListSection _options = null!;

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        _header = new EventHeaderSection();
        AddChild(_header);

        _trigger = new TriggerFieldsSection();
        AddChild(_trigger);

        _targetSelection = new TargetSelectionSection();
        AddChild(_targetSelection);

        _conditions = new ConditionListSection();
        AddChild(_conditions);

        _options = new OptionListSection();
        AddChild(_options);
    }

    public void LoadFromDto(EventDto dto)
    {
        _header.LoadFromDto(dto);
        _trigger.LoadFromDto(dto);
        _targetSelection.LoadFromDto(dto);
        _conditions.LoadFromDtos(dto.Conditions);
        _options.LoadFromDtos(dto.Options);
    }

    public EventDto WriteToDto()
    {
        var dto = new EventDto();
        _header.WriteToDto(dto);
        _trigger.WriteToDto(dto);
        _targetSelection.WriteToDto(dto);
        dto.Conditions = _conditions.WriteToDtos();
        dto.Options = _options.WriteToDtos();
        return dto;
    }

    public void Clear()
    {
        LoadFromDto(new EventDto
        {
            TriggerType = "pulse",
            MeanTimeToHappenDays = 30,
            Priority = 100,
        });
    }
}
