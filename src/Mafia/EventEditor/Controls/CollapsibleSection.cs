using Godot;

namespace Mafia.EventEditor.Controls;

/// <summary>
/// Reusable expand/collapse container with a toggle button header.
/// </summary>
public partial class CollapsibleSection : VBoxContainer
{
    private Button _header = null!;
    private VBoxContainer _body = null!;
    private string _title = "";
    protected bool _expanded = true;

    public string Title
    {
        get => _header != null ? (_header.Text?.TrimStart('▼', '▶', ' ') ?? "") : _title;
        set
        {
            _title = value;
            UpdateHeaderText(value);
        }
    }

    public VBoxContainer Body => _body;

    public bool Expanded
    {
        get => _expanded;
        set
        {
            _expanded = value;
            if (_body != null)
            {
                _body.Visible = value;
                UpdateHeaderText(Title);
            }
        }
    }

    public override void _Ready()
    {
        _header = new Button
        {
            Flat = true,
            Alignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _header.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        _header.Pressed += ToggleExpanded;
        AddChild(_header);

        _body = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = _expanded,
        };

        var indent = new MarginContainer();
        indent.AddThemeConstantOverride("margin_left", 12);
        indent.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        indent.AddChild(_body);
        AddChild(indent);

        UpdateHeaderText(_title);
    }

    private void ToggleExpanded()
    {
        Expanded = !_expanded;
    }

    private void UpdateHeaderText(string title)
    {
        if (_header == null) return;
        var icon = _expanded ? "▼" : "▶";
        _header.Text = $"{icon} {title}";
    }
}
