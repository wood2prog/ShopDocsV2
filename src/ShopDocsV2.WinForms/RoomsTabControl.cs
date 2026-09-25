using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

public partial class RoomsTabControl : UserControl
{
    private readonly Dictionary<TabPage, Room> _roomsByTab = new();
    private readonly HashSet<TabPage> _builtPages = new();
    private List<Room>? _rooms;
    private QuestionSet _questionSet = new();
    private CatalogSnapshot _catalog = new();
    private TabPage? _dragPage;
    private Point _dragStart;
    private bool _dragMoved;

    public event EventHandler? RoomsChanged;

    public Room? SelectedRoom => tabControl.SelectedTab is { } page && _roomsByTab.TryGetValue(page, out var room) ? room : null;

    public RoomsTabControl()
    {
        InitializeComponent();
        tabControl.MouseDoubleClick += TabControl_MouseDoubleClick;
        tabControl.MouseDown += TabControl_MouseDown;
        tabControl.MouseMove += TabControl_MouseMove;
        tabControl.MouseUp += TabControl_MouseUp;
        tabControl.SelectedIndexChanged += (_, _) =>
        {
            if (tabControl.SelectedTab is { } page)
            {
                BuildTabContent(page);
            }
        };
    }

    /// <summary>Rebuilds tab content so it reflects the newly loaded schema (e.g. the initial async load completing after rooms were already shown).</summary>
    public void SetQuestionSet(QuestionSet questionSet)
    {
        _questionSet = questionSet;
        ResetTabContent();
    }

    internal void SetCatalog(CatalogSnapshot catalog)
    {
        _catalog = catalog;
        ResetTabContent();
    }

    /// <summary>Discards every tab's built content, rebuilding the selected tab now; the others rebuild when next selected.</summary>
    private void ResetTabContent()
    {
        foreach (TabPage page in tabControl.TabPages)
        {
            page.Controls.Clear();
        }
        _builtPages.Clear();

        if (tabControl.SelectedTab is { } selected)
        {
            BuildTabContent(selected);
        }
    }

    public void LoadRooms(List<Room>? rooms)
    {
        tabControl.TabPages.Clear();
        _roomsByTab.Clear();
        _builtPages.Clear();
        _rooms = rooms;

        if (rooms is null)
        {
            return;
        }

        foreach (var room in rooms.OrderBy(r => r.SortOrder))
        {
            AddTabForRoom(room);
        }

        if (tabControl.SelectedTab is { } selected)
        {
            BuildTabContent(selected);
        }
    }

    public void AddRoom()
    {
        if (_rooms is null)
        {
            return;
        }

        var room = new Room { Id = Guid.NewGuid(), SortOrder = _rooms.Count };
        _rooms.Add(room);
        var page = AddTabForRoom(room);
        tabControl.SelectedTab = page;
        BuildTabContent(page);
        RoomsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveSelectedRoom()
    {
        if (tabControl.SelectedTab is not { } page || !_roomsByTab.TryGetValue(page, out var room))
        {
            return;
        }

        var confirmName = string.IsNullOrWhiteSpace(room.Name) ? "this room" : room.Name;
        var result = MessageBox.Show(FindForm(), $"Remove \"{confirmName}\"?", "Remove Room",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _rooms!.Remove(room);
        _roomsByTab.Remove(page);
        _builtPages.Remove(page);
        tabControl.TabPages.Remove(page);
        RenumberSortOrders();
        RoomsChanged?.Invoke(this, EventArgs.Empty);
    }

    private TabPage AddTabForRoom(Room room)
    {
        var page = new TabPage(TabTitle(room));
        _roomsByTab[page] = room;
        tabControl.TabPages.Add(page);
        return page;
    }

    private void BuildTabContent(TabPage page)
    {
        if (_builtPages.Contains(page) || !_roomsByTab.TryGetValue(page, out var room))
        {
            return;
        }

        page.SuspendLayout();
        var content = RoomFormBuilder.Build(room, _questionSet, _catalog, () => RoomsChanged?.Invoke(this, EventArgs.Empty));
        content.Dock = DockStyle.Fill;
        page.Controls.Add(content);
        page.ResumeLayout();

        _builtPages.Add(page);
    }

    public void RenameSelectedRoom()
    {
        if (tabControl.SelectedTab is { } page)
        {
            RenameRoom(page);
        }
    }

    private void TabControl_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        for (var i = 0; i < tabControl.TabPages.Count; i++)
        {
            if (!tabControl.GetTabRect(i).Contains(e.Location))
            {
                continue;
            }

            RenameRoom(tabControl.TabPages[i]);
            return;
        }
    }

    private int TabIndexAt(Point location)
    {
        for (var i = 0; i < tabControl.TabPages.Count; i++)
        {
            if (tabControl.GetTabRect(i).Contains(location))
            {
                return i;
            }
        }
        return -1;
    }

    private void TabControl_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var index = TabIndexAt(e.Location);
        _dragPage = index >= 0 ? tabControl.TabPages[index] : null;
        _dragStart = e.Location;
        _dragMoved = false;
    }

    private void TabControl_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_dragPage is null || e.Button != MouseButtons.Left)
        {
            return;
        }

        // Ignore small jitters so plain clicks and double-click-to-rename aren't treated as drags.
        if (!_dragMoved)
        {
            var dragSize = SystemInformation.DragSize;
            if (Math.Abs(e.X - _dragStart.X) < dragSize.Width && Math.Abs(e.Y - _dragStart.Y) < dragSize.Height)
            {
                return;
            }
        }

        var from = tabControl.TabPages.IndexOf(_dragPage);
        var to = TabIndexAt(e.Location);
        if (to < 0 || to == from)
        {
            return;
        }

        // Only move once the pointer would land inside the dragged tab's new slot; otherwise, with tabs of
        // different widths, the swap puts the pointer back over the other tab and the two flip-flop.
        var target = tabControl.GetTabRect(to);
        var draggedWidth = tabControl.GetTabRect(from).Width;
        if (to > from ? e.X < target.Right - draggedWidth : e.X > target.Left + draggedWidth)
        {
            return;
        }

        tabControl.SuspendLayout();
        tabControl.TabPages.Remove(_dragPage);
        tabControl.TabPages.Insert(to, _dragPage);
        tabControl.SelectedTab = _dragPage;
        tabControl.ResumeLayout();
        _dragMoved = true;
    }

    private void TabControl_MouseUp(object? sender, MouseEventArgs e)
    {
        var moved = _dragMoved;
        _dragPage = null;
        _dragMoved = false;

        if (moved)
        {
            RenumberSortOrders();
            RoomsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RenameRoom(TabPage page)
    {
        if (!_roomsByTab.TryGetValue(page, out var room))
        {
            return;
        }

        var newName = PromptDialog.ShowInput(FindForm(), "Rename Room", "Room name:", room.Name);
        if (newName is null)
        {
            return;
        }

        room.Name = newName.Trim();
        page.Text = TabTitle(room);
        RoomsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RenumberSortOrders()
    {
        var order = 0;
        foreach (TabPage page in tabControl.TabPages)
        {
            _roomsByTab[page].SortOrder = order++;
        }
    }

    private static string TabTitle(Room room) => string.IsNullOrWhiteSpace(room.Name) ? "(unnamed room)" : room.Name;
}
