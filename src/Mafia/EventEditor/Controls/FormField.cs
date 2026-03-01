using System.Net.Mime;
using Godot;

namespace Mafia.EventEditor.Controls;

/// <summary>
/// Helper to create consistently styled form field rows (label + control).
/// </summary>
public static class FormField
{
    public static HBoxContainer LabeledRow(string label, Control control, string tooltip = "")
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

        var lbl = new Label
        {
            Text = label,
            CustomMinimumSize = new Vector2(180, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TooltipText = tooltip,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        row.AddChild(lbl);

        control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(control);

        return row;
    }

    public static LineEdit TextInput(string placeholder = "")
    {
        return new LineEdit
        {
            PlaceholderText = placeholder,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
    }

    public static TextEdit TextAreaInput(string placeholder = "")
    {
        return new TextEdit
        {
            PlaceholderText = placeholder,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 80),
            WrapMode = TextEdit.LineWrappingMode.Boundary,
            ScrollFitContentHeight = true,
        };
    }

    public static SpinBox IntInput(int min = int.MinValue, int max = int.MaxValue, int step = 1)
    {
        return new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
    }

    public static SpinBox DoubleInput(double min = -1e9, double max = 1e9, double step = 0.1)
    {
        return new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
    }

    public static OptionButton Dropdown(params string[] items)
    {
        var dropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (var item in items)
            dropdown.AddItem(item);
        return dropdown;
    }

    public static CheckBox Toggle(string label = "")
    {
        return new CheckBox { Text = label };
    }
}
