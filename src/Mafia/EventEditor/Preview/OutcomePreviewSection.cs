using Godot;
using Mafia.Core.Content.Parsers.Dtos;

namespace Mafia.EventEditor.Preview;

/// <summary>
/// Renders a single <see cref="OptionOutcomeDto"/>: resolution text key + effect list.
/// trigger_event effects are rendered as clickable links.
/// </summary>
public partial class OutcomePreviewSection : VBoxContainer
{
    public Action<string>? OnTriggerEventClicked;

    public void Display(OptionOutcomeDto outcome)
    {
        foreach (var child in GetChildren()) child.QueueFree();

        if (!string.IsNullOrWhiteSpace(outcome.ResolutionTextKey))
        {
            var resLabel = new Label
            {
                Text = outcome.ResolutionTextKey,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            resLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
            AddChild(resLabel);
        }

        if (outcome.Effects == null || outcome.Effects.Count == 0)
            return;

        foreach (var effect in outcome.Effects)
        {
            if (effect.Type?.ToLowerInvariant() == "trigger_event" && !string.IsNullOrWhiteSpace(effect.EventId))
            {
                var link = new LinkButton
                {
                    Text = $"→ {DtoDescriber.DescribeEffect(effect)}",
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    Underline = LinkButton.UnderlineMode.OnHover,
                };
                link.AddThemeColorOverride("font_color", new Color(0.4f, 0.7f, 1f));
                link.AddThemeColorOverride("font_hover_color", new Color(0.6f, 0.85f, 1f));
                var eventId = effect.EventId;
                link.Pressed += () => OnTriggerEventClicked?.Invoke(eventId);
                AddChild(link);
            }
            else
            {
                var label = new Label
                {
                    Text = $"• {DtoDescriber.DescribeEffect(effect)}",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                AddChild(label);
            }
        }
    }
}
