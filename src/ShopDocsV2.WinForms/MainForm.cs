using System.Text.RegularExpressions;
using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

public partial class MainForm : Form
{
    private readonly IJobRepository _jobRepository;
    private readonly IQuestionSetProvider _questionSetProvider;
    private readonly ICatalogRepository _catalogRepository;
    private readonly ISpecFormattingService _specFormattingService;
    private readonly IJobPrintContentBuilder _jobPrintContentBuilder;
    private readonly IPaintColorLookupService _paintColorLookupService;
    private readonly IOrdxExportService _ordxExportService;
    private readonly IKeyBindingStore _keyBindingStore;
    /// <summary>Menu commands that are always present; the Go to Group ones come from the question set (_groupCommands).</summary>
    private readonly List<KeyBindingCommand> _menuCommands;
    private List<KeyBindingCommand> _groupCommands = [];
    private readonly System.Windows.Forms.Timer _saveDebounceTimer;
    private Job? _currentJob;
    /// <summary>False for a New Job that hasn't been written yet; it's inserted on the first save that finds content in it.</summary>
    private bool _currentJobIsPersisted;
    private QuestionSet _questionSet = new();
    private bool _closingAfterFlush;

    public MainForm(
        IJobRepository jobRepository,
        IQuestionSetProvider questionSetProvider,
        ICatalogRepository catalogRepository,
        ISpecFormattingService specFormattingService,
        IJobPrintContentBuilder jobPrintContentBuilder,
        IPaintColorLookupService paintColorLookupService,
        IOrdxExportService ordxExportService,
        IKeyBindingStore keyBindingStore)
    {
        _jobRepository = jobRepository;
        _questionSetProvider = questionSetProvider;
        _catalogRepository = catalogRepository;
        _specFormattingService = specFormattingService;
        _jobPrintContentBuilder = jobPrintContentBuilder;
        _paintColorLookupService = paintColorLookupService;
        _ordxExportService = ordxExportService;
        _keyBindingStore = keyBindingStore;
        InitializeComponent();
        WindowPlacementStore.Restore(this);

        // Defaults are the shortcuts set in the Designer; ids are what settings.json stores, so don't rename them.
        _menuCommands =
        [
            MenuCommand("file.new", "File", newJobMenuItem),
            MenuCommand("file.open", "File", openJobMenuItem),
            MenuCommand("file.duplicate", "File", duplicateJobMenuItem),
            MenuCommand("file.save", "File", saveMenuItem),
            MenuCommand("room.add", "Room", addRoomMenuItem),
            MenuCommand("room.rename", "Room", renameRoomMenuItem),
            MenuCommand("room.remove", "Room", removeRoomMenuItem),
            MenuCommand("room.next", "Room", nextRoomMenuItem),
            MenuCommand("room.previous", "Room", previousRoomMenuItem),
            MenuCommand("export.printPreview", "Export", printPreviewMenuItem),
            MenuCommand("export.print", "Export", printMenuItem),
            MenuCommand("export.copyRoom", "Export", copyRoomMenuItem),
            MenuCommand("export.text", "Export", exportTextMenuItem),
            MenuCommand("export.ordx", "Export", exportOrdxMenuItem),
            MenuCommand("tools.catalogManager", "Tools", catalogManagerMenuItem),
            MenuCommand("tools.reloadQuestions", "Tools", reloadQuestionsMenuItem)
        ];
        ApplyKeyBindings();

        // Registered with the Designer's components container so it's disposed along with the form.
        _saveDebounceTimer = new System.Windows.Forms.Timer(components!) { Interval = 1000 };
        _saveDebounceTimer.Tick += async (_, _) => await SaveCurrentJobAsync();

        jobInfoPanel.FieldsChanged += (_, _) => ScheduleAutosave();
        roomsTabControl.RoomsChanged += (_, _) => ScheduleAutosave();

        Load += async (_, _) =>
        {
            await LoadQuestionSetAsync();
            roomsTabControl.SetCatalog(await CatalogSnapshot.LoadAsync(_catalogRepository));
        };

        FormClosing += MainForm_FormClosing;
        FormClosed += (_, _) => WindowPlacementStore.Save(this);

        SetCurrentJob(null);
    }

    /// <summary>The debounce timer can leave up to ~1s of edits unsaved; flush them before the app actually closes.</summary>
    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closingAfterFlush || !_saveDebounceTimer.Enabled)
        {
            return;
        }

        e.Cancel = true;
        await SaveCurrentJobAsync();
        _closingAfterFlush = true;
        Close();
    }

    private async Task LoadQuestionSetAsync()
    {
        try
        {
            ApplyQuestionSet(await _questionSetProvider.LoadAsync());
        }
        catch (QuestionSetLoadException ex)
        {
            MessageBox.Show(this,
                $"Could not load questions.json:\n{ex.Message}\n\nRooms will show only their name until this is fixed.",
                "Question Set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ApplyQuestionSet(new QuestionSet());
        }
    }

    private void ApplyQuestionSet(QuestionSet questionSet)
    {
        _questionSet = questionSet;
        roomsTabControl.SetQuestionSet(questionSet);
        RebuildGoToGroupMenu();
    }

    private static KeyBindingCommand MenuCommand(string id, string menuName, ToolStripMenuItem item) =>
        new(id, $"{menuName}: {item.Text!.Replace("&", "").TrimEnd('.')}", item.ShortcutKeys, item);

    /// <summary>One Go to Group item per List question, defaulting to the shortcut given in questions.json.</summary>
    private void RebuildGoToGroupMenu()
    {
        foreach (var oldItem in goToGroupMenuItem.DropDownItems.Cast<ToolStripItem>().ToList())
        {
            oldItem.Dispose();
        }

        _groupCommands = [];
        foreach (var question in _questionSet.Sections.SelectMany(s => s.Questions).Where(q => q.Type == QuestionType.List))
        {
            var questionId = question.Id;
            var item = new ToolStripMenuItem(question.Label) { Enabled = _currentJob is not null };
            item.Click += (_, _) => roomsTabControl.StartNewListItem(questionId);
            goToGroupMenuItem.DropDownItems.Add(item);

            var defaultKeys = KeyBindings.TryParse(question.Shortcut, out var keys) ? keys : Keys.None;
            _groupCommands.Add(new KeyBindingCommand($"group.{questionId}", $"Go to Group: {question.Label}", defaultKeys, item));
        }
        goToGroupMenuItem.Visible = _groupCommands.Count > 0;

        ApplyKeyBindings();
    }

    private List<KeyBindingCommand> AllKeyBindingCommands() => [.. _menuCommands, .. _groupCommands];

    private void ApplyKeyBindings() => KeyBindings.Apply(AllKeyBindingCommands(), _keyBindingStore.Load());

    private void KeyboardShortcutsMenuItem_Click(object? sender, EventArgs e)
    {
        var commands = AllKeyBindingCommands();
        if (!KeyBindingsDialog.Show(this, commands))
        {
            return;
        }

        // Keep saved bindings for groups that aren't in the current question set (e.g. while a different questions.json is loaded).
        var currentIds = commands.Select(c => c.Id).ToHashSet();
        var bindings = _keyBindingStore.Load().Where(b => !currentIds.Contains(b.Key)).ToDictionary(b => b.Key, b => b.Value);
        foreach (var (id, shortcut) in KeyBindings.Overrides(commands))
        {
            bindings[id] = shortcut;
        }

        try
        {
            _keyBindingStore.Save(bindings);
            SetStatus("Keyboard shortcuts saved");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"The new shortcuts work now, but could not be saved for next time:\n{ex.Message}",
                "Keyboard Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void NewJobMenuItem_Click(object? sender, EventArgs e)
    {
        await FlushPendingSaveAsync();

        // Not written to the database yet: SaveCurrentJobAsync inserts it once something is entered,
        // so opening New Job and walking away doesn't leave an empty job in the list.
        var job = new Job { Id = Guid.NewGuid(), DateCreated = DateTime.Today, UpdatedAt = DateTime.Now };
        SetCurrentJob(job, isPersisted: false);
        SetStatus("New job (saved once you enter information)");
    }

    private async void OpenJobMenuItem_Click(object? sender, EventArgs e)
    {
        await FlushPendingSaveAsync();

        using var listForm = new JobListForm(_jobRepository);
        if (listForm.ShowDialog(this) != DialogResult.OK || listForm.SelectedJobId is not { } selectedId)
        {
            return;
        }

        var job = await _jobRepository.GetJobAsync(selectedId);
        if (job is null)
        {
            MessageBox.Show(this, "That job could not be found. It may have been deleted.", "Open Job",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetCurrentJob(job);
        SetStatus(string.IsNullOrWhiteSpace(job.CustomerName) ? "Opened job" : $"Opened {job.CustomerName}");
    }

    private async void DuplicateJobMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentJob is null)
        {
            return;
        }

        await SaveCurrentJobAsync();
        if (!_currentJobIsPersisted)
        {
            SetStatus("Nothing to duplicate yet - enter some information first");
            return;
        }

        var newId = await _jobRepository.DuplicateJobAsync(_currentJob.Id);
        var duplicate = await _jobRepository.GetJobAsync(newId);
        if (duplicate is null)
        {
            return;
        }

        SetCurrentJob(duplicate);
        SetStatus("Duplicated as new job");
    }

    private async void SaveMenuItem_Click(object? sender, EventArgs e) => await SaveCurrentJobAsync();

    private void ExitMenuItem_Click(object? sender, EventArgs e) => Close();

    private void AboutMenuItem_Click(object? sender, EventArgs e) => AboutDialog.Show(this);

    private void AddRoomMenuItem_Click(object? sender, EventArgs e) => roomsTabControl.AddRoom();

    private void RenameRoomMenuItem_Click(object? sender, EventArgs e) => roomsTabControl.RenameSelectedRoom();

    private void RemoveRoomMenuItem_Click(object? sender, EventArgs e) => roomsTabControl.RemoveSelectedRoom();

    private void NextRoomMenuItem_Click(object? sender, EventArgs e) => roomsTabControl.SelectAdjacentRoom(1);

    private void PreviousRoomMenuItem_Click(object? sender, EventArgs e) => roomsTabControl.SelectAdjacentRoom(-1);

    private void PrintPreviewMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentJob is null)
        {
            return;
        }

        using var printDocument = BuildPrintDocument(_currentJob);
        using var previewDialog = new PrintPreviewDialog { Document = printDocument, Width = 900, Height = 700 };
        AddPageSetupButton(previewDialog);
        previewDialog.ShowDialog(this);
    }

    /// <summary>Adds a Page Setup button to the preview's toolbar so orientation, paper and margins can be changed and re-previewed.</summary>
    private static void AddPageSetupButton(PrintPreviewDialog previewDialog)
    {
        if (previewDialog.Controls.OfType<ToolStrip>().FirstOrDefault() is not { } toolStrip)
        {
            return;
        }

        var pageSetupButton = new ToolStripButton("Page Setup...") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        pageSetupButton.Click += (_, _) =>
        {
            using var pageSetupDialog = new PageSetupDialog { Document = previewDialog.Document, EnableMetric = true };
            if (pageSetupDialog.ShowDialog(previewDialog) == DialogResult.OK)
            {
                previewDialog.PrintPreviewControl.InvalidatePreview();
            }
        };
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(pageSetupButton);
    }

    private void PrintMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentJob is null)
        {
            return;
        }

        using var printDocument = BuildPrintDocument(_currentJob);
        using var printDialog = new PrintDialog { Document = printDocument, AllowSomePages = false, UseEXDialog = true };
        if (printDialog.ShowDialog(this) == DialogResult.OK)
        {
            printDocument.Print();
        }
    }

    private JobPrintDocument BuildPrintDocument(Job job)
    {
        var spec = _jobPrintContentBuilder.Build(job, _questionSet);
        var name = string.IsNullOrWhiteSpace(job.CustomerName) ? "Job Specification" : $"{job.CustomerName} - Job Specification";
        return new JobPrintDocument(spec, name);
    }

    private void CopyRoomMenuItem_Click(object? sender, EventArgs e)
    {
        var room = roomsTabControl.SelectedRoom;
        if (room is null)
        {
            return;
        }

        if (!ClipboardText.TrySet(this, _specFormattingService.BuildRoomText(room, _questionSet)))
        {
            return;
        }
        SetStatus($"Copied \"{(string.IsNullOrWhiteSpace(room.Name) ? "room" : room.Name)}\" specs to clipboard");
    }

    private void ExportTextMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentJob is null)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"{(string.IsNullOrWhiteSpace(_currentJob.CustomerName) ? "Job" : _currentJob.CustomerName)} Spec.txt"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, _specFormattingService.BuildJobText(_currentJob, _questionSet));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Could not save '{dialog.FileName}':\n{ex.Message}", "Export Text",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        SetStatus("Exported job as text");
    }

    private void ExportOrdxMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentJob is null)
        {
            return;
        }

        if (!_currentJob.Rooms.Any(r => !string.IsNullOrWhiteSpace(r.Name)))
        {
            MessageBox.Show(this, "Add at least one named room before exporting to ORDX.", "Export ORDX",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "ORDX files (*.ordx)|*.ordx|All files (*.*)|*.*",
            FileName = SuggestedOrdxFileName(_currentJob)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, _ordxExportService.BuildOrdxXml(_currentJob, _questionSet));
        SetStatus("Exported ORDX file");
    }

    /// <summary>Ported from the original app's suggestedFilename(): trim, strip illegal filename chars, collapse whitespace to underscores.</summary>
    private static string SuggestedOrdxFileName(Job job)
    {
        var name = (job.CustomerName ?? "").Trim();
        name = Regex.Replace(name, "[\\\\/:*?\"<>|]+", "");
        name = Regex.Replace(name, @"\s+", "_");
        return (string.IsNullOrEmpty(name) ? "job" : name) + ".ordx";
    }

    private async void CatalogManagerMenuItem_Click(object? sender, EventArgs e)
    {
        using var catalogManagerForm = new CatalogManagerForm(_catalogRepository, _paintColorLookupService, _questionSet);
        catalogManagerForm.ShowDialog(this);

        // Catalog edits are committed live by the manager's grids; refresh so open room tabs pick them up.
        roomsTabControl.SetCatalog(await CatalogSnapshot.LoadAsync(_catalogRepository));
    }

    private async void ReloadQuestionsMenuItem_Click(object? sender, EventArgs e)
    {
        try
        {
            ApplyQuestionSet(await _questionSetProvider.LoadAsync());
            SetStatus("Reloaded questions.json");
            return;
        }
        catch (QuestionSetLoadException ex)
        {
            var pickAnother = MessageBox.Show(this,
                $"Could not load questions.json:\n{ex.Message}\n\nPick a different file?",
                "Reload Questions", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (pickAnother != DialogResult.Yes)
            {
                return;
            }
        }

        using var openDialog = new OpenFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" };
        if (openDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            ApplyQuestionSet(await _questionSetProvider.LoadAsync(openDialog.FileName));
            SetStatus("Reloaded questions.json");
        }
        catch (QuestionSetLoadException ex)
        {
            MessageBox.Show(this, $"Could not load '{openDialog.FileName}':\n{ex.Message}", "Reload Questions",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetCurrentJob(Job? job, bool isPersisted = true)
    {
        _saveDebounceTimer.Stop();
        _currentJob = job;
        _currentJobIsPersisted = isPersisted;

        jobInfoPanel.LoadJob(job);
        roomsTabControl.LoadRooms(job?.Rooms);

        var hasJob = job is not null;
        ToolStripItem[] jobMenuItems =
        [
            saveMenuItem, duplicateJobMenuItem, addRoomMenuItem, renameRoomMenuItem, removeRoomMenuItem,
            nextRoomMenuItem, previousRoomMenuItem, goToGroupMenuItem,
            printPreviewMenuItem, printMenuItem, copyRoomMenuItem, exportTextMenuItem, exportOrdxMenuItem,
            .. goToGroupMenuItem.DropDownItems.Cast<ToolStripItem>()
        ];
        foreach (var item in jobMenuItems)
        {
            item.Enabled = hasJob;
        }
        jobInfoPanel.Enabled = hasJob;
        roomsTabControl.Enabled = hasJob;

        if (!hasJob)
        {
            SetStatus("No job open");
        }
    }

    private void ScheduleAutosave()
    {
        if (_currentJob is null)
        {
            return;
        }

        SetStatus("Unsaved changes...");
        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
    }

    private async Task FlushPendingSaveAsync()
    {
        if (_saveDebounceTimer.Enabled)
        {
            await SaveCurrentJobAsync();
        }
    }

    private async Task SaveCurrentJobAsync()
    {
        _saveDebounceTimer.Stop();
        if (_currentJob is null)
        {
            return;
        }

        if (!_currentJobIsPersisted && !_currentJob.HasContent)
        {
            SetStatus("New job (saved once you enter information)");
            return;
        }

        SetStatus("Saving...");
        if (_currentJobIsPersisted)
        {
            await _jobRepository.SaveJobAsync(_currentJob);
        }
        else
        {
            // Flag first so an overlapping save (e.g. Ctrl+S during this await) updates instead of inserting twice.
            _currentJobIsPersisted = true;
            try
            {
                await _jobRepository.CreateJobAsync(_currentJob);
            }
            catch
            {
                _currentJobIsPersisted = false;
                throw;
            }
        }
        SetStatus($"Saved {DateTime.Now:t}");
    }

    private void SetStatus(string text) => statusLabel.Text = text;
}
