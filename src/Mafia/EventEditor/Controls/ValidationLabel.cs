using Godot;

namespace Mafia.EventEditor.Controls;

/// <summary>
/// Red error label that can be shown/hidden.
/// </summary>
public partial class ValidationLabel : Label
{
    public override void _Ready()
    {
        AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
        Visible = false;
        AutowrapMode = TextServer.AutowrapMode.WordSmart;
    }

    public void ShowError(string message)
    {
        Text = message;
        Visible = true;
    }

    public new void Hide()
    {
        Visible = false;
        Text = "";
    }
}
