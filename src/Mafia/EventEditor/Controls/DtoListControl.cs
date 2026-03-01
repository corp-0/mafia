using Godot;

namespace Mafia.EventEditor.Controls;

/// <summary>
/// Generic add/remove/reorder list for DTO arrays.
/// Each item is rendered by a user-supplied factory that creates a Control for each DTO entry.
/// </summary>
public partial class DtoListControl<TDto> : VBoxContainer where TDto : class, new()
{
    private Func<TDto, int, Control> _buildItem = null!;
    private string _itemLabel = "";
    private VBoxContainer _itemsContainer = null!;
    private readonly List<TDto> _items = [];

    public IReadOnlyList<TDto> Items => _items;

    /// <summary>
    /// Fired when any item is added, removed, or reordered.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Fired when an item requests its fields to be refreshed (e.g. type changed).
    /// </summary>
    public event Action<int>? ItemRefreshRequested;

    public void Initialize(string itemLabel, Func<TDto, int, Control> buildItem)
    {
        _itemLabel = itemLabel;
        _buildItem = buildItem;
    }

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var addBtn = new Button { Text = $"+ Add {_itemLabel}" };
        addBtn.Pressed += AddNewItem;
        AddChild(addBtn);

        _itemsContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        AddChild(_itemsContainer);
    }

    public void Load(List<TDto>? dtos)
    {
        _items.Clear();
        if (dtos != null) _items.AddRange(dtos);
        Rebuild();
    }

    public List<TDto>? Save()
    {
        return _items.Count == 0 ? null : new List<TDto>(_items);
    }

    private void AddNewItem()
    {
        _items.Add(new TDto());
        Rebuild();
        Changed?.Invoke();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        _items.RemoveAt(index);
        Rebuild();
        Changed?.Invoke();
    }

    public void MoveUp(int index)
    {
        if (index <= 0 || index >= _items.Count) return;
        (_items[index - 1], _items[index]) = (_items[index], _items[index - 1]);
        Rebuild();
        Changed?.Invoke();
    }

    public void MoveDown(int index)
    {
        if (index < 0 || index >= _items.Count - 1) return;
        (_items[index], _items[index + 1]) = (_items[index + 1], _items[index]);
        Rebuild();
        Changed?.Invoke();
    }

    private void Rebuild()
    {
        foreach (var child in _itemsContainer.GetChildren())
            child.QueueFree();

        for (var i = 0; i < _items.Count; i++)
        {
            var idx = i;
            var row = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            var toolbar = new HBoxContainer();
            var label = new Label { Text = $"{_itemLabel} #{i + 1}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            toolbar.AddChild(label);

            if (i > 0)
            {
                var up = new Button { Text = "↑", CustomMinimumSize = new Vector2(30, 0) };
                up.Pressed += () => MoveUp(idx);
                toolbar.AddChild(up);
            }

            if (i < _items.Count - 1)
            {
                var down = new Button { Text = "↓", CustomMinimumSize = new Vector2(30, 0) };
                down.Pressed += () => MoveDown(idx);
                toolbar.AddChild(down);
            }

            var remove = new Button { Text = "✕", CustomMinimumSize = new Vector2(30, 0) };
            remove.Pressed += () => RemoveAt(idx);
            toolbar.AddChild(remove);

            row.AddChild(toolbar);

            var itemControl = _buildItem(_items[i], i);
            row.AddChild(itemControl);

            var sep = new HSeparator();
            row.AddChild(sep);

            _itemsContainer.AddChild(row);
        }
    }

    public void RequestItemRefresh(int index) => ItemRefreshRequested?.Invoke(index);
}
