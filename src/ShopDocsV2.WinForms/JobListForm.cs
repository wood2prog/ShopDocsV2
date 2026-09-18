using ShopDocsV2.Application;

namespace ShopDocsV2.WinForms;

public partial class JobListForm : Form
{
    private readonly IJobRepository _jobRepository;
    private List<JobSummary> _allJobs = [];

    public Guid? SelectedJobId { get; private set; }

    public JobListForm(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
        InitializeComponent();

        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenSelected(); };
        filterTextBox.TextChanged += (_, _) => ApplyFilter();
        openButton.Click += (_, _) => OpenSelected();
        deleteButton.Click += async (_, _) => await DeleteSelectedAsync();
        Load += async (_, _) => await LoadJobsAsync();
    }

    private async Task LoadJobsAsync()
    {
        _allJobs = (await _jobRepository.ListJobsAsync()).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filter = filterTextBox.Text.Trim();
        var filtered = filter.Length == 0
            ? _allJobs
            : _allJobs.Where(j =>
                (j.CustomerName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (j.Address?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

        grid.Rows.Clear();
        foreach (var job in filtered)
        {
            var rowIndex = grid.Rows.Add(
                job.CustomerName,
                job.Address ?? "",
                job.DateCreated.ToShortDateString(),
                job.DueDate?.ToShortDateString() ?? "",
                job.UpdatedAt.ToString("g"));
            grid.Rows[rowIndex].Tag = job.Id;
        }
    }

    private void OpenSelected()
    {
        if (grid.CurrentRow?.Tag is not Guid id)
        {
            return;
        }

        SelectedJobId = id;
        DialogResult = DialogResult.OK;
        Close();
    }

    private async Task DeleteSelectedAsync()
    {
        if (grid.CurrentRow?.Tag is not Guid id)
        {
            return;
        }

        var name = grid.CurrentRow.Cells[0].Value as string;
        var confirm = MessageBox.Show(this,
            $"Delete job \"{(string.IsNullOrWhiteSpace(name) ? "(unnamed)" : name)}\"? This cannot be undone.",
            "Delete Job", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await _jobRepository.DeleteJobAsync(id);
        await LoadJobsAsync();
    }
}
