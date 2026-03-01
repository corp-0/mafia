using Godot;

namespace Mafia.EventEditor;

/// <summary>
/// Left panel: Tree of .toml files in content/Base/Events/.
/// </summary>
public partial class EventEditorFileBrowser : VBoxContainer
{
    private Tree _tree = null!;
    private string _rootPath = "";
    private Window? _folderDialog;

    /// <summary>
    /// Fired when a .toml file is selected. Passes the full file path.
    /// </summary>
    public event Action<string>? FileSelected;

    /// <summary>
    /// Fired when the user clicks "New".
    /// </summary>
    public event Action? NewRequested;

    /// <summary>
    /// Fired when the user clicks "Delete" on the currently selected file.
    /// </summary>
    public event Action<string>? DeleteRequested;

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        CustomMinimumSize = new Vector2(250, 0);

        // Toolbar
        var toolbar = new HBoxContainer();

        var newBtn = new Button { Text = "New" };
        newBtn.Pressed += () => NewRequested?.Invoke();
        toolbar.AddChild(newBtn);

        var folderBtn = new Button { Text = "Folder" };
        folderBtn.Pressed += OnNewFolderPressed;
        toolbar.AddChild(folderBtn);

        var deleteBtn = new Button { Text = "Delete" };
        deleteBtn.Pressed += OnDeletePressed;
        toolbar.AddChild(deleteBtn);

        var refreshBtn = new Button { Text = "Refresh" };
        refreshBtn.Pressed += () => Refresh();
        toolbar.AddChild(refreshBtn);

        AddChild(toolbar);

        // Tree
        _tree = new Tree
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HideRoot = true,
        };
        _tree.ItemSelected += OnItemSelected;
        AddChild(_tree);
    }

    public void SetRootPath(string rootPath)
    {
        _rootPath = rootPath;
        Refresh();
    }

    public void Refresh()
    {
        _tree.Clear();
        if (string.IsNullOrEmpty(_rootPath) || !Directory.Exists(_rootPath))
            return;

        var root = _tree.CreateItem();

        // Each subdirectory of the root is a content pack.
        // For each pack that has an Events/ folder, show the pack name
        // and populate from its Events/ directory.
        try
        {
            foreach (var packDir in Directory.EnumerateDirectories(_rootPath).OrderBy(d => d))
            {
                var eventsDir = Path.Combine(packDir, "Events");
                if (!Directory.Exists(eventsDir)) continue;

                var packName = Path.GetFileName(packDir);
                var packItem = _tree.CreateItem(root);
                packItem.SetText(0, packName);
                packItem.SetMetadata(0, eventsDir);
                packItem.Collapsed = false;
                PopulateTree(packItem, eventsDir);
            }
        }
        catch (Exception) { /* directory access denied, skip */ }
    }

    private void PopulateTree(TreeItem parent, string dirPath)
    {
        // Subdirectories first
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(dirPath).OrderBy(d => d))
            {
                var dirName = Path.GetFileName(dir);
                var dirItem = _tree.CreateItem(parent);
                dirItem.SetText(0, $"📁 {dirName}");
                dirItem.SetMetadata(0, dir);
                dirItem.Collapsed = false;
                PopulateTree(dirItem, dir);
            }
        }
        catch (Exception) { /* directory access denied, skip */ }

        // Then .toml files
        try
        {
            foreach (var file in Directory.EnumerateFiles(dirPath, "*.toml").OrderBy(f => f))
            {
                var fileName = Path.GetFileName(file);
                var fileItem = _tree.CreateItem(parent);
                fileItem.SetText(0, fileName);
                fileItem.SetMetadata(0, file);
            }
        }
        catch (Exception) { /* file access denied, skip */ }
    }

    private void OnItemSelected()
    {
        var selected = _tree.GetSelected();
        if (selected == null) return;

        var path = selected.GetMetadata(0).AsString();
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".toml"))
            FileSelected?.Invoke(path);
    }

    private void OnDeletePressed()
    {
        var selected = _tree.GetSelected();
        if (selected == null) return;

        var path = selected.GetMetadata(0).AsString();
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".toml"))
            DeleteRequested?.Invoke(path);
    }

    public string? GetSelectedPath()
    {
        var selected = _tree.GetSelected();
        if (selected == null) return null;
        var path = selected.GetMetadata(0).AsString();
        return !string.IsNullOrEmpty(path) && path.EndsWith(".toml") ? path : null;
    }

    /// <summary>
    /// Returns the directory path of the currently selected item.
    /// If a file is selected, returns its parent directory.
    /// Falls back to the root path.
    /// </summary>
    public string GetSelectedDirectory()
    {
        var selected = _tree.GetSelected();
        if (selected == null) return _rootPath;

        var meta = selected.GetMetadata(0).AsString();
        if (string.IsNullOrEmpty(meta)) return _rootPath;

        if (meta.EndsWith(".toml"))
            return Path.GetDirectoryName(meta) ?? _rootPath;

        return meta; // it's a directory path
    }

    private void OnNewFolderPressed()
    {
        if (_folderDialog != null)
        {
            _folderDialog.QueueFree();
            _folderDialog = null;
        }

        _folderDialog = new Window
        {
            Title = "New Folder",
            Size = new Vector2I(350, 120),
            Exclusive = true,
            Transient = true,
            Unresizable = true,
        };

        var vbox = new VBoxContainer();
        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 8);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddChild(vbox);
        _folderDialog.AddChild(margin);

        var label = new Label { Text = $"Create folder in: {Path.GetFileName(GetSelectedDirectory())}/" };
        vbox.AddChild(label);

        var nameInput = new LineEdit
        {
            PlaceholderText = "Folder name",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        vbox.AddChild(nameInput);

        var btnRow = new HBoxContainer();
        btnRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }); // spacer

        var createBtn = new Button { Text = "Create" };
        createBtn.Pressed += () =>
        {
            var folderName = nameInput.Text.Trim();
            if (string.IsNullOrEmpty(folderName)) return;

            var parentDir = GetSelectedDirectory();
            var newDir = Path.Combine(parentDir, folderName);

            if (!Directory.Exists(newDir))
                Directory.CreateDirectory(newDir);

            _folderDialog!.QueueFree();
            _folderDialog = null;
            Refresh();
        };
        btnRow.AddChild(createBtn);

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Pressed += () =>
        {
            _folderDialog!.QueueFree();
            _folderDialog = null;
        };
        btnRow.AddChild(cancelBtn);

        vbox.AddChild(btnRow);

        _folderDialog.CloseRequested += () =>
        {
            _folderDialog.QueueFree();
            _folderDialog = null;
        };

        AddChild(_folderDialog);
        _folderDialog.PopupCentered();
        nameInput.GrabFocus();
    }
}
