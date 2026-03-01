using Godot;
using Mafia.Content;
using Mafia.EventEditor.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mafia.EventEditor;

/// <summary>
/// Root node for the Event Editor tool.
/// HSplitContainer layout: file browser (left) + form editor (right).
/// Load/save coordination, Ctrl+S, unsaved changes tracking.
/// </summary>
[GlobalClass]
public partial class EventEditorRoot : Control
{
    private EventEditorFileBrowser _fileBrowser = null!;
    private EventEditorForm _form = null!;
    private Label _statusLabel = null!;
    private ScrollContainer _errorScroll = null!;
    private VBoxContainer _errorList = null!;
    private ILogger _logger = null!;
    private string? _currentFilePath;
    private bool _hasUnsavedChanges;

    private string _contentRootPath = "";

    public override void _Ready()
    {
        AnchorsPreset = (int)LayoutPreset.FullRect;

        _logger = NullLoggerFactory.Instance.CreateLogger<EventEditorRoot>();
        _contentRootPath = ContentBootstrapper.ResolveContentPath();

        // Main layout
        var split = new HSplitContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        split.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(split);

        // Left panel: file browser + error panel
        var leftPanel = new VBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        split.AddChild(leftPanel);

        _fileBrowser = new EventEditorFileBrowser();
        _fileBrowser.FileSelected += OnFileSelected;
        _fileBrowser.NewRequested += OnNewRequested;
        _fileBrowser.DeleteRequested += OnDeleteRequested;
        leftPanel.AddChild(_fileBrowser);

        _errorScroll = new ScrollContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(0, 120),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _errorList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _errorScroll.AddChild(_errorList);
        leftPanel.AddChild(_errorScroll);

        var rightMargin = new MarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        rightMargin.AddThemeConstantOverride("margin_left", 40);
        rightMargin.AddThemeConstantOverride("margin_right", 40);
        rightMargin.AddThemeConstantOverride("margin_top", 40);
        rightMargin.AddThemeConstantOverride("margin_bottom", 40);
        split.AddChild(rightMargin);

        var rightPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        rightMargin.AddChild(rightPanel);

        var toolbar = new HBoxContainer();

        var saveBtn = new Button { Text = "Save (Ctrl+S)" };
        saveBtn.Pressed += Save;
        toolbar.AddChild(saveBtn);

        var validateBtn = new Button { Text = "Validate" };
        validateBtn.Pressed += ValidateCurrentDto;
        toolbar.AddChild(validateBtn);

        _statusLabel = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        toolbar.AddChild(_statusLabel);

        rightPanel.AddChild(toolbar);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        rightPanel.AddChild(scroll);

        var formMargin = new MarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        formMargin.AddThemeConstantOverride("margin_right", 16);
        scroll.AddChild(formMargin);

        _form = new EventEditorForm
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        formMargin.AddChild(_form);

        _fileBrowser.SetRootPath(_contentRootPath);
        SetStatus("Ready. Select a file or create a new event.");
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.S, CtrlPressed: true })
        {
            Save();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnFileSelected(string path)
    {
        if (_hasUnsavedChanges)
        {
            // Simple confirmation via status a dialog would be better but this works
            // For now, just load (Phase 5 will add proper confirmation)
        }

        LoadFile(path);
    }

    private void LoadFile(string path)
    {
        try
        {
            var toml = File.ReadAllText(path);
            var dto = TomlSerializer.Deserialize(toml);
            _form.LoadFromDto(dto);
            _currentFilePath = path;
            _hasUnsavedChanges = false;
            SetStatus($"Loaded: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading: {ex.Message}", true);
            _logger.LogError(ex, "Failed to load {Path}", path);
        }
    }

    private void Save()
    {
        var dto = _form.WriteToDto();

        var errors = EventValidator.Validate(dto);
        if (errors.Count > 0)
        {
            ShowErrors(errors);
            SetStatus($"Validation failed ({errors.Count} error{(errors.Count > 1 ? "s" : "")})", true);
            return;
        }
        ShowErrors([]);

        if (_currentFilePath == null)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
            {
                SetStatus("Cannot save: event ID is required for new files.", true);
                return;
            }
            var saveDir = _fileBrowser.GetSelectedDirectory();
            _currentFilePath = Path.Combine(saveDir, $"{dto.Id}.toml");
        }

        try
        {
            var toml = TomlSerializer.Serialize(dto);
            var dir = Path.GetDirectoryName(_currentFilePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(_currentFilePath, toml);
            _hasUnsavedChanges = false;
            SetStatus($"Saved: {Path.GetFileName(_currentFilePath)}");
            _fileBrowser.Refresh();
        }
        catch (Exception ex)
        {
            SetStatus($"Error saving: {ex.Message}", true);
            _logger.LogError(ex, "Failed to save {Path}", _currentFilePath);
        }
    }

    private void ValidateCurrentDto()
    {
        var dto = _form.WriteToDto();
        var errors = EventValidator.Validate(dto);
        ShowErrors(errors);

        if (errors.Count == 0)
            SetStatus("Validation passed!");
        else
            SetStatus($"Validation failed ({errors.Count} error{(errors.Count > 1 ? "s" : "")})", true);
    }

    private void OnNewRequested()
    {
        _currentFilePath = null;
        _form.Clear();
        _hasUnsavedChanges = false;
        SetStatus("New event. Fill in the fields and save.");
    }

    private void OnDeleteRequested(string path)
    {
        // Confirm via dialog (Phase 5 polish for now just delete)
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                if (_currentFilePath == path)
                {
                    _currentFilePath = null;
                    _form.Clear();
                }
                _fileBrowser.Refresh();
                SetStatus($"Deleted: {Path.GetFileName(path)}");
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error deleting: {ex.Message}", true);
        }
    }

    private void ShowErrors(List<string> errors)
    {
        foreach (var child in _errorList.GetChildren())
            child.QueueFree();

        if (errors.Count == 0)
        {
            _errorScroll.Visible = false;
            return;
        }

        _errorScroll.Visible = true;

        var title = new Label
        {
            Text = $"Errors ({errors.Count})",
        };
        title.AddThemeColorOverride("font_color", new Color(1f, 0.35f, 0.35f));
        _errorList.AddChild(title);

        foreach (var err in errors)
        {
            var lbl = new Label
            {
                Text = $"  {err}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            lbl.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.55f));
            _errorList.AddChild(lbl);
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        _statusLabel.Text = message;
        _statusLabel.AddThemeColorOverride("font_color",
            isError ? new Color(1f, 0.3f, 0.3f) : new Color(0.7f, 0.9f, 0.7f));
    }
}
