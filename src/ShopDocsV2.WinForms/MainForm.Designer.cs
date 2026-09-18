namespace ShopDocsV2.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem fileMenuItem = null!;
    private ToolStripMenuItem newJobMenuItem = null!;
    private ToolStripMenuItem openJobMenuItem = null!;
    private ToolStripMenuItem duplicateJobMenuItem = null!;
    private ToolStripMenuItem saveMenuItem = null!;
    private ToolStripMenuItem exitMenuItem = null!;
    private ToolStripMenuItem roomMenuItem = null!;
    private ToolStripMenuItem addRoomMenuItem = null!;
    private ToolStripMenuItem renameRoomMenuItem = null!;
    private ToolStripMenuItem removeRoomMenuItem = null!;
    private ToolStripMenuItem exportMenuItem = null!;
    private ToolStripMenuItem printPreviewMenuItem = null!;
    private ToolStripMenuItem printMenuItem = null!;
    private ToolStripMenuItem copyRoomMenuItem = null!;
    private ToolStripMenuItem exportTextMenuItem = null!;
    private ToolStripMenuItem exportOrdxMenuItem = null!;
    private ToolStripMenuItem toolsMenuItem = null!;
    private ToolStripMenuItem catalogManagerMenuItem = null!;
    private ToolStripMenuItem reloadQuestionsMenuItem = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private JobInfoPanel jobInfoPanel = null!;
    private RoomsTabControl roomsTabControl = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        menuStrip = new MenuStrip();
        fileMenuItem = new ToolStripMenuItem();
        newJobMenuItem = new ToolStripMenuItem();
        openJobMenuItem = new ToolStripMenuItem();
        duplicateJobMenuItem = new ToolStripMenuItem();
        saveMenuItem = new ToolStripMenuItem();
        exitMenuItem = new ToolStripMenuItem();
        roomMenuItem = new ToolStripMenuItem();
        addRoomMenuItem = new ToolStripMenuItem();
        renameRoomMenuItem = new ToolStripMenuItem();
        removeRoomMenuItem = new ToolStripMenuItem();
        exportMenuItem = new ToolStripMenuItem();
        printPreviewMenuItem = new ToolStripMenuItem();
        printMenuItem = new ToolStripMenuItem();
        copyRoomMenuItem = new ToolStripMenuItem();
        exportTextMenuItem = new ToolStripMenuItem();
        exportOrdxMenuItem = new ToolStripMenuItem();
        toolsMenuItem = new ToolStripMenuItem();
        catalogManagerMenuItem = new ToolStripMenuItem();
        reloadQuestionsMenuItem = new ToolStripMenuItem();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        jobInfoPanel = new JobInfoPanel();
        roomsTabControl = new RoomsTabControl();

        SuspendLayout();

        // fileMenuItem
        fileMenuItem.Text = "&File";
        fileMenuItem.DropDownItems.AddRange(
        [
            newJobMenuItem,
            openJobMenuItem,
            duplicateJobMenuItem,
            new ToolStripSeparator(),
            saveMenuItem,
            new ToolStripSeparator(),
            exitMenuItem
        ]);

        newJobMenuItem.Text = "&New Job";
        newJobMenuItem.ShortcutKeys = Keys.Control | Keys.N;
        newJobMenuItem.Click += NewJobMenuItem_Click;

        openJobMenuItem.Text = "&Open Job...";
        openJobMenuItem.ShortcutKeys = Keys.Control | Keys.O;
        openJobMenuItem.Click += OpenJobMenuItem_Click;

        duplicateJobMenuItem.Text = "Dup&licate as New Job";
        duplicateJobMenuItem.Click += DuplicateJobMenuItem_Click;

        saveMenuItem.Text = "&Save";
        saveMenuItem.ShortcutKeys = Keys.Control | Keys.S;
        saveMenuItem.Click += SaveMenuItem_Click;

        exitMenuItem.Text = "E&xit";
        exitMenuItem.Click += ExitMenuItem_Click;

        // roomMenuItem
        roomMenuItem.Text = "&Room";
        roomMenuItem.DropDownItems.AddRange(
        [
            addRoomMenuItem,
            renameRoomMenuItem,
            removeRoomMenuItem
        ]);

        addRoomMenuItem.Text = "&Add Room";
        addRoomMenuItem.Click += AddRoomMenuItem_Click;

        renameRoomMenuItem.Text = "Re&name Room...";
        renameRoomMenuItem.Click += RenameRoomMenuItem_Click;

        removeRoomMenuItem.Text = "&Remove Room";
        removeRoomMenuItem.Click += RemoveRoomMenuItem_Click;

        // exportMenuItem
        exportMenuItem.Text = "&Export";
        exportMenuItem.DropDownItems.AddRange(
        [
            printPreviewMenuItem,
            printMenuItem,
            new ToolStripSeparator(),
            copyRoomMenuItem,
            exportTextMenuItem,
            exportOrdxMenuItem
        ]);

        printPreviewMenuItem.Text = "Print Pre&view...";
        printPreviewMenuItem.Click += PrintPreviewMenuItem_Click;

        printMenuItem.Text = "&Print...";
        printMenuItem.ShortcutKeys = Keys.Control | Keys.P;
        printMenuItem.Click += PrintMenuItem_Click;

        copyRoomMenuItem.Text = "&Copy Room to Clipboard";
        copyRoomMenuItem.Click += CopyRoomMenuItem_Click;

        exportTextMenuItem.Text = "Export &Job as .txt...";
        exportTextMenuItem.Click += ExportTextMenuItem_Click;

        exportOrdxMenuItem.Text = "Export &ORDX...";
        exportOrdxMenuItem.Click += ExportOrdxMenuItem_Click;

        // toolsMenuItem
        toolsMenuItem.Text = "&Tools";
        toolsMenuItem.DropDownItems.AddRange(
        [
            catalogManagerMenuItem,
            reloadQuestionsMenuItem
        ]);

        catalogManagerMenuItem.Text = "&Catalog Manager...";
        catalogManagerMenuItem.Click += CatalogManagerMenuItem_Click;

        reloadQuestionsMenuItem.Text = "&Reload Questions";
        reloadQuestionsMenuItem.Click += ReloadQuestionsMenuItem_Click;

        // menuStrip
        menuStrip.Items.AddRange([fileMenuItem, roomMenuItem, exportMenuItem, toolsMenuItem]);
        menuStrip.Dock = DockStyle.Top;

        // statusStrip
        statusLabel.Text = "No job open";
        statusLabel.Spring = true;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusStrip.Items.Add(statusLabel);
        statusStrip.Dock = DockStyle.Bottom;

        // jobInfoPanel
        jobInfoPanel.Dock = DockStyle.Top;

        // roomsTabControl
        roomsTabControl.Dock = DockStyle.Fill;

        // MainForm
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(900, 650);
        Controls.Add(roomsTabControl);
        Controls.Add(jobInfoPanel);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Text = "Shop Docs";
        MinimumSize = new Size(700, 500);
        Icon = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);

        ResumeLayout(false);
        PerformLayout();
    }
}
