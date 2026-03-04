using Godot;
using Mafia.Core.Content.Parsers.Dtos;

namespace Mafia.EventEditor.Preview;

/// <summary>
/// Expandable card displaying a single event option.
/// Click header to toggle outcome details.
/// </summary>
public partial class OptionPreviewRow : VBoxContainer
{
    public Action<string>? OnTriggerEventClicked;

    private VBoxContainer? _outcomeArea;
    private bool _expanded;

    public void Display(OptionDto option)
    {
        foreach (var child in GetChildren()) child.QueueFree();
        _expanded = false;

        var type = option.Type?.ToLowerInvariant() switch
        {
            "skill_check" => "skill_check",
            "random" => "random",
            _ => "standard",
        };

        // Header button
        var header = new Button
        {
            Text = $"▶ [{type}] {option.DisplayTextKey}",
            Flat = true,
            Alignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        header.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        AddChild(header);

        // Visibility condition label
        if (option.VisibilityConditions != null)
        {
            var condLabel = new Label
            {
                Text = $"  [Conditional] {DtoDescriber.DescribeCondition(option.VisibilityConditions)}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            condLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.55f, 0.4f));
            AddChild(condLabel);
        }

        // Outcome area (initially hidden)
        var indent = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        indent.AddThemeConstantOverride("margin_left", 20);
        AddChild(indent);

        _outcomeArea = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        indent.AddChild(_outcomeArea);

        BuildOutcomes(type, option);

        header.Pressed += () =>
        {
            _expanded = !_expanded;
            _outcomeArea.Visible = _expanded;
            header.Text = (_expanded ? "▼" : "▶") + header.Text[1..];
        };

        // Separator
        AddChild(new HSeparator());
    }

    private void BuildOutcomes(string type, OptionDto option)
    {
        switch (type)
        {
            case "standard":
                BuildStandardOutcome(option);
                break;
            case "skill_check":
                BuildSkillCheckOutcome(option);
                break;
            case "random":
                BuildRandomOutcomes(option);
                break;
        }
    }

    private void BuildStandardOutcome(OptionDto option)
    {
        var outcome = option.Outcome ?? new OptionOutcomeDto
        {
            ResolutionTextKey = option.ResolutionTextKey,
            Effects = option.Effects,
        };

        var section = CreateOutcomeSection();
        section.Display(outcome);
        _outcomeArea!.AddChild(section);
    }

    private void BuildSkillCheckOutcome(OptionDto option)
    {
        var infoLabel = new Label
        {
            Text = $"Stat: {option.StatName ?? option.StatPath ?? "?"} | Difficulty: {option.Difficulty ?? 0}",
        };
        infoLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
        _outcomeArea!.AddChild(infoLabel);

        if (option.Success != null)
        {
            var successLabel = new Label { Text = "✓ Success:" };
            successLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.9f, 0.4f));
            _outcomeArea.AddChild(successLabel);

            var successSection = CreateOutcomeSection();
            successSection.Display(option.Success);
            _outcomeArea.AddChild(successSection);
        }

        if (option.Failure != null)
        {
            var failLabel = new Label { Text = "✗ Failure:" };
            failLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.4f, 0.4f));
            _outcomeArea.AddChild(failLabel);

            var failSection = CreateOutcomeSection();
            failSection.Display(option.Failure);
            _outcomeArea.AddChild(failSection);
        }
    }

    private void BuildRandomOutcomes(OptionDto option)
    {
        if (option.Outcomes == null || option.Outcomes.Count == 0) return;

        var totalWeight = option.Outcomes.Sum(o => o.Weight);

        for (var i = 0; i < option.Outcomes.Count; i++)
        {
            var wo = option.Outcomes[i];
            var pct = totalWeight > 0 ? wo.Weight * 100.0 / totalWeight : 0;

            var header = new Label
            {
                Text = $"Outcome {i + 1} — {pct:F1}% (weight {wo.Weight})",
            };
            header.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.85f));
            _outcomeArea!.AddChild(header);

            var section = CreateOutcomeSection();
            section.Display(wo);
            _outcomeArea.AddChild(section);
        }
    }

    private OutcomePreviewSection CreateOutcomeSection()
    {
        var section = new OutcomePreviewSection
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        section.OnTriggerEventClicked += id => OnTriggerEventClicked?.Invoke(id);
        return section;
    }
}
