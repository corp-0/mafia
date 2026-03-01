using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// Outcome: resolution_text_key + EffectListSection.
/// Reused in standard options, skill check success/failure, and weighted outcomes.
/// </summary>
public partial class OutcomeSection : CollapsibleSection
{
    private TextEdit _resolutionTextKey = null!;
    private EffectListSection _effects = null!;
    
    public override void _Ready()
    {
        base._Ready();

        _resolutionTextKey = FormField.TextAreaInput("Resolution text / localization key");
        Body.AddChild(FormField.LabeledRow("Resolution Text", _resolutionTextKey));

        _effects = new EffectListSection();
        _effects.Title = "Effects";
        Body.AddChild(_effects);
    }

    public void LoadFromDto(OptionOutcomeDto? dto)
    {
        _resolutionTextKey.Text = dto?.ResolutionTextKey ?? "";
        _effects.LoadFromDtos(dto?.Effects);
    }

    /// <summary>
    /// Load from flat option fields (resolution_text_key + effects directly on the option).
    /// </summary>
    public void LoadFlat(string? resolutionTextKey, List<EffectDto>? effects)
    {
        _resolutionTextKey.Text = resolutionTextKey ?? "";
        _effects.LoadFromDtos(effects);
    }

    public OptionOutcomeDto? WriteToDto()
    {
        var effects = _effects.WriteToDtos();
        var text = NullIfEmpty(_resolutionTextKey.Text);

        if (text == null && effects == null)
            return null;

        return new OptionOutcomeDto
        {
            ResolutionTextKey = text,
            Effects = effects,
        };
    }

    /// <summary>
    /// Write flat fields back to an OptionDto (for standard options that use shorthand).
    /// </summary>
    public void WriteFlat(OptionDto dto)
    {
        dto.ResolutionTextKey = NullIfEmpty(_resolutionTextKey.Text);
        dto.Effects = _effects.WriteToDtos();
        dto.Outcome = null;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
