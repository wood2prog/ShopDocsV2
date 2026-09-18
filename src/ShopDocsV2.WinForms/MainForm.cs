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
    private readonly System.Windows.Forms.Timer _saveDebounceTimer;
    private Job? _currentJob;
    private QuestionSet _questionSet = new();
    private bool _closingAfterFlush;

    public MainForm(
        IJobRepository jobRepository,
        IQuestionSetProvider questionSetProvider,
        ICatalogRepository catalogRepository,
        ISpecFormattingService specFormattingService,
        IJobPrintContentBuilder jobPrintContentBuilder,
        IPaintColorLookupService paintColorLookupService,
        IOrdxExportService ordxExportService)
    {
        _jobRepository = jobRepository;
        _questionSetProvider = questionSetProvider;
        _catalogRepository = catalogRepository;
        _specFormattingService = specFormattingService;
        _jobPrintContentBuilder = jobPrintContentBuilder;
        _paintColorLookupService = paintColorLookupService;
        _ordxExportService = ordxExportService;
        InitializeComponent();

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
            _questionSet = await _questionSetProvider.LoadAsync();
        }
        catch (QuestionSetLoadException ex)
        {
            MessageBox.Show(this,
                $"Could not load questions.json:\n{ex.Message}\n\nRooms will show only their name until this is fixed.",
                "Question Set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _questionSet = new QuestionSet();
        }
        roomsTabControl.SetQuestionSet(_questionSet);
    }

    private async void NewJobMenuItem_Click(object? sender, EventArgs e)
    {
        await FlushPendingSaveAsync();

        var job = new Job { Id = Guid.NewGuid(), DateCreated = DateTime.Today, UpdatedAt = DateTime.Now };
        await _jobRepository.CreateJobAsync(job);
        SetCurrentJob(job);
        SetStatus("Created new job");
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

    private void AddRoomMenuItem_Click(object? sender, EventArgs e) => roomsTabControl.AddRoom();

    private void RenameRoomMenuItem_Click(object? sender, EventArgs e) => roomsTabControl.RenameSelectedRoom();

    private void RemoveRoomMenuItem_Click(object? sender, EventArgs e) => roomsTabControl.RemoveSelectedRoom();

    private void PrintPreviewMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentJob is null)
        {
            return;
        }

        using var printDocument = BuildPrintDocument(_currentJob);
        using var previewDialog = new PrintPreviewDialog { Document = printDocument, Width = 900, Height = 700 };
        previewDialog.ShowDialog(this);
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

        Clipboard.SetText(_specFormattingService.BuildRoomText(room, _questionSet));
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

        File.WriteAllText(dialog.FileName, _specFormattingService.BuildJobText(_currentJob, _questionSet));
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
        using var catalogManagerForm = new CatalogManagerForm(_catalogRepository, _paintColorLookupService);
        catalogManagerForm.ShowDialog(this);

        // Catalog edits are committed live by the manager's grids; refresh so open room tabs pick them up.
        roomsTabControl.SetCatalog(await CatalogSnapshot.LoadAsync(_catalogRepository));
    }

    private async void ReloadQuestionsMenuItem_Click(object? sender, EventArgs e)
    {
        try
        {
            _questionSet = await _questionSetProvider.LoadAsync();
            roomsTabControl.SetQuestionSet(_questionSet);
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
            _questionSet = await _questionSetProvider.LoadAsync(openDialog.FileName);
            roomsTabControl.SetQuestionSet(_questionSet);
            SetStatus("Reloaded questions.json");
        }
        catch (QuestionSetLoadException ex)
        {
            MessageBox.Show(this, $"Could not load '{openDialog.FileName}':\n{ex.Message}", "Reload Questions",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetCurrentJob(Job? job)
    {
        _saveDebounceTimer.Stop();
        _currentJob = job;

        jobInfoPanel.LoadJob(job);
        roomsTabControl.LoadRooms(job?.Rooms);

        var hasJob = job is not null;
        saveMenuItem.Enabled = hasJob;
        duplicateJobMenuItem.Enabled = hasJob;
        addRoomMenuItem.Enabled = hasJob;
        renameRoomMenuItem.Enabled = hasJob;
        removeRoomMenuItem.Enabled = hasJob;
        printPreviewMenuItem.Enabled = hasJob;
        printMenuItem.Enabled = hasJob;
        copyRoomMenuItem.Enabled = hasJob;
        exportTextMenuItem.Enabled = hasJob;
        exportOrdxMenuItem.Enabled = hasJob;
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

        SetStatus("Saving...");
        await _jobRepository.SaveJobAsync(_currentJob);
        SetStatus($"Saved {DateTime.Now:t}");
    }

    private void SetStatus(string text) => statusLabel.Text = text;
}
