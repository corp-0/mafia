using Godot;
using Mafia.Core.Content.Parsers.Dtos;

namespace Mafia.EventEditor.Preview;

/// <summary>
/// Top-level preview panel managing breadcrumb navigation for chained events.
/// Entry point: <see cref="PreviewEvent"/>.
/// </summary>
public partial class EventPreviewPanel : VBoxContainer
{
    private readonly List<(string id, EventDto dto)> _stack = [];
    private EventFileResolver _fileResolver = null!;

    private HBoxContainer _breadcrumbBar = null!;
    private Label _errorLabel = null!;
    private EventPreviewCard _card = null!;

    public void Init(EventFileResolver fileResolver)
    {
        _fileResolver = fileResolver;
    }

    public override void _Ready()
    {
        _breadcrumbBar = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        AddChild(_breadcrumbBar);

        _errorLabel = new Label
        {
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _errorLabel.AddThemeColorOverride("font_color", new Color(1f, 0.4f, 0.4f));
        AddChild(_errorLabel);

        _card = new EventPreviewCard
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _card.OnTriggerEventClicked += NavigateToEvent;
        AddChild(_card);
    }

    public void PreviewEvent(EventDto dto)
    {
        _stack.Clear();
        _stack.Add((dto.Id, dto));
        ShowCurrentEvent();
    }

    private void NavigateToEvent(string eventId)
    {
        // Circular chain detection
        if (_stack.Any(entry => entry.id == eventId))
        {
            ShowError($"Circular chain detected: '{eventId}' is already in the navigation stack.");
            return;
        }

        if (!_fileResolver.TryResolve(eventId, out var dto) || dto == null)
        {
            ShowError($"Could not resolve event '{eventId}'. File not found or invalid.");
            return;
        }

        _stack.Add((eventId, dto));
        ShowCurrentEvent();
    }

    private void NavigateBack(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= _stack.Count) return;

        // Remove everything after the target
        _stack.RemoveRange(targetIndex + 1, _stack.Count - targetIndex - 1);
        ShowCurrentEvent();
    }

    private void ShowCurrentEvent()
    {
        ClearError();
        RebuildBreadcrumbs();

        var (_, dto) = _stack[^1];
        _card.Display(dto);
    }

    private void RebuildBreadcrumbs()
    {
        foreach (var child in _breadcrumbBar.GetChildren()) child.QueueFree();

        for (var i = 0; i < _stack.Count; i++)
        {
            if (i > 0)
            {
                var sep = new Label { Text = " → " };
                sep.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
                _breadcrumbBar.AddChild(sep);
            }

            var (id, _) = _stack[i];
            var displayName = string.IsNullOrWhiteSpace(id) ? "(unsaved)" : id;

            if (i == _stack.Count - 1)
            {
                // Current event: plain label
                var label = new Label { Text = displayName };
                label.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.8f));
                _breadcrumbBar.AddChild(label);
            }
            else
            {
                // Previous event: clickable link
                var link = new LinkButton
                {
                    Text = displayName,
                    Underline = LinkButton.UnderlineMode.OnHover,
                };
                link.AddThemeColorOverride("font_color", new Color(0.4f, 0.7f, 1f));
                link.AddThemeColorOverride("font_hover_color", new Color(0.6f, 0.85f, 1f));
                var targetIndex = i;
                link.Pressed += () => NavigateBack(targetIndex);
                _breadcrumbBar.AddChild(link);
            }
        }
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Visible = true;
    }

    private void ClearError()
    {
        _errorLabel.Text = "";
        _errorLabel.Visible = false;
    }
}
