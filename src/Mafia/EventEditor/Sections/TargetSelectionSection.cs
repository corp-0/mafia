using Godot;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.EventEditor.Controls;

namespace Mafia.EventEditor.Sections;

/// <summary>
/// Target selection: pool, filter condition, selection mode.
/// </summary>
public partial class TargetSelectionSection : CollapsibleSection
{
    private CheckBox _enabled = null!;
    private VBoxContainer _fields = null!;
    private LineEdit _pool = null!;
    private LineEdit _selectionMode = null!;
    private LineEdit _requiredTrait = null!;

    public TargetSelectionSection() { Title = "Target Selection"; _expanded = false; }

    public override void _Ready()
    {
        base._Ready();

        _enabled = FormField.Toggle("Has target selection");
        _enabled.Toggled += on => _fields.Visible = on;
        Body.AddChild(_enabled);

        _fields = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };

        _pool = FormField.TextInput("e.g. root_crew, root_family, same_territory");
        _fields.AddChild(FormField.LabeledRow("Pool", _pool));

        _selectionMode = FormField.TextInput("e.g. random, highest_stat:respect");
        _fields.AddChild(FormField.LabeledRow("Selection Mode", _selectionMode));

        _requiredTrait = FormField.TextInput("(optional)");
        _fields.AddChild(FormField.LabeledRow("Required Trait", _requiredTrait));

        Body.AddChild(_fields);
    }

    public void LoadFromDto(EventDto dto)
    {
        var ts = dto.TargetSelection;
        var hasTarget = ts != null;
        _enabled.ButtonPressed = hasTarget;
        _fields.Visible = hasTarget;

        if (ts != null)
        {
            _pool.Text = ts.Pool ?? "";
            _selectionMode.Text = ts.SelectionMode ?? "random";
            _requiredTrait.Text = ts.RequiredTrait ?? "";
        }
        else
        {
            _pool.Text = "";
            _selectionMode.Text = "random";
            _requiredTrait.Text = "";
        }
    }

    public void WriteToDto(EventDto dto)
    {
        if (!_enabled.ButtonPressed)
        {
            dto.TargetSelection = null;
            return;
        }

        dto.TargetSelection = new TargetSelectionDto
        {
            Pool = NullIfEmpty(_pool.Text),
            SelectionMode = NullIfEmpty(_selectionMode.Text),
            RequiredTrait = NullIfEmpty(_requiredTrait.Text),
        };
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
