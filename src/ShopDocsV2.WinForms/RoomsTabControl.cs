using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

public partial class RoomsTabControl : UserControl
{
    private readonly Dictionary<TabPage, Room> _roomsByTab = new();
    private readonly HashSet<TabPage> _builtPages = new();
    private readonly RoomFormBuilder _formBuilder = new();
    private List<Room>? _rooms;
    private QuestionSet _questionSet = new();
    private CatalogSnapshot _catalog = new();

    public event EventHandler? RoomsChanged;

    public Room? SelectedRoom => tabControl.SelectedTab is { } page && _roomsByTab.TryGetValue(page, out var room) ? room : null;

    public RoomsTabControl()
    {
        InitializeComponent();
        tabControl.MouseDoubleClick += TabControl_MouseDoubleClick;
        tabControl.SelectedIndexChanged += (_, _) =>
        {
            if (tabControl.SelectedTab is { } page)
            {
                BuildTabContent(page);
            }
        };
    }

    /// <summary>Rebuilds any already-built tab content so it reflects the newly loaded schema (e.g. the initial async load completing after rooms were already shown).</summary>
    public void SetQuestionSet(QuestionSet questionSet)
    {
        _questionSet = questionSet;
        RebuildVisibleTab();
    }

    internal void SetCatalog(CatalogSnapshot catalog)
    {
        _catalog = catalog;
        RebuildVisibleTab();
    }

    private void RebuildVisibleTab()
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
        var content = _formBuilder.Build(room, _questionSet, _catalog, () => RoomsChanged?.Invoke(this, EventArgs.Empty));
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
