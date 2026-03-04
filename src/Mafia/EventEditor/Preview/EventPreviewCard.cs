using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Preview;

/// <summary>
/// Renders a complete event as a player-facing preview card:
/// title, description, metadata, conditions, target selection, and options.
/// </summary>
public partial class EventPreviewCard : VBoxContainer
{
    public Action<string>? OnTriggerEventClicked;

    public void Display(EventDto dto)
    {
        foreach (var child in GetChildren()) child.QueueFree();

        AddTitleSection(dto);
        AddDescriptionSection(dto);
        AddMetadataSection(dto);
        AddTargetSelectionSection(dto);
        AddConditionsSection(dto);
        AddOptionsSection(dto);
    }

    private void AddTitleSection(EventDto dto)
    {
        var title = new Label
        {
            Text = dto.TitleKey,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.8f));
        AddChild(title);
    }

    private void AddDescriptionSection(EventDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DescriptionKey)) return;

        var desc = new Label
        {
            Text = dto.DescriptionKey,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        desc.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.85f));
        AddChild(desc);

        AddChild(new HSeparator());
    }

    private void AddMetadataSection(EventDto dto)
    {
        var parts = new List<string> { $"ID: {dto.Id}" };

        if (!string.IsNullOrWhiteSpace(dto.ScopeType))
            parts.Add($"Scope: {dto.ScopeType}");

        if (!string.IsNullOrWhiteSpace(dto.TriggerType))
            parts.Add($"Trigger: {dto.TriggerType}");

        switch (dto.TriggerType?.ToLowerInvariant())
        {
            case "pulse":
                if (dto.MeanTimeToHappenDays.HasValue)
                    parts.Add($"MTTH: {dto.MeanTimeToHappenDays}d");
                break;
            case "on_action":
                if (!string.IsNullOrWhiteSpace(dto.OnActionId))
                    parts.Add($"Action: {dto.OnActionId}");
                break;
            case "story_beat":
                if (!string.IsNullOrWhiteSpace(dto.StoryDate))
                    parts.Add($"Date: {dto.StoryDate}");
                break;
        }

        if (dto.IsOneTimeOnly) parts.Add("One-time");
        if (dto.CooldownDays > 0) parts.Add($"Cooldown: {dto.CooldownDays}d");
        if (dto.Priority != 0) parts.Add($"Priority: {dto.Priority}");

        var meta = new Label
        {
            Text = string.Join("  |  ", parts),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        meta.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.6f));
        meta.AddThemeFontSizeOverride("font_size", 13);
        AddChild(meta);

        AddChild(new HSeparator());
    }

    private void AddTargetSelectionSection(EventDto dto)
    {
        if (dto.TargetSelection == null) return;
        var ts = dto.TargetSelection;

        var section = new CollapsibleSection { Expanded = false };
        AddChild(section);
        section.Title = "Target Selection";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(ts.Pool)) parts.Add($"Pool: {ts.Pool}");
        if (!string.IsNullOrWhiteSpace(ts.SelectionMode)) parts.Add($"Mode: {ts.SelectionMode}");
        if (!string.IsNullOrWhiteSpace(ts.RequiredTrait)) parts.Add($"Trait: {ts.RequiredTrait}");

        if (parts.Count > 0)
        {
            var label = new Label
            {
                Text = string.Join("  |  ", parts),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            section.Body.AddChild(label);
        }

        if (ts.Filter != null)
        {
            var filterLabel = new Label
            {
                Text = $"Filter: {DtoDescriber.DescribeCondition(ts.Filter)}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            section.Body.AddChild(filterLabel);
        }
    }

    private void AddConditionsSection(EventDto dto)
    {
        if (dto.Conditions == null || dto.Conditions.Count == 0) return;

        var section = new CollapsibleSection { Expanded = false };
        AddChild(section);
        section.Title = $"Conditions ({dto.Conditions.Count})";

        foreach (var cond in dto.Conditions)
        {
            var label = new Label
            {
                Text = $"• {DtoDescriber.DescribeCondition(cond)}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            section.Body.AddChild(label);
        }
    }

    private void AddOptionsSection(EventDto dto)
    {
        if (dto.Options == null || dto.Options.Count == 0) return;

        var header = new Label { Text = "Options" };
        header.AddThemeFontSizeOverride("font_size", 18);
        header.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
        AddChild(header);

        foreach (var option in dto.Options)
        {
            var row = new OptionPreviewRow
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            row.OnTriggerEventClicked += id => OnTriggerEventClicked?.Invoke(id);
            AddChild(row);
            row.Display(option);
        }
    }
}
