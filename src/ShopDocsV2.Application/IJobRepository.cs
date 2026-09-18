using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

public interface IJobRepository
{
    Task<Guid> CreateJobAsync(Job job, CancellationToken ct = default);
    Task<Job?> GetJobAsync(Guid id, CancellationToken ct = default);

    /// <summary>Full upsert of the job graph (job + rooms + answers + list items). Used by both autosave and the explicit Save action.</summary>
    Task SaveJobAsync(Job job, CancellationToken ct = default);

    Task DeleteJobAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<JobSummary>> ListJobsAsync(CancellationToken ct = default);

    /// <summary>Clones a job's entire graph into a new job record. Replaces the original app's file-based "Save As".</summary>
    Task<Guid> DuplicateJobAsync(Guid sourceJobId, CancellationToken ct = default);
}

public sealed record JobSummary(Guid Id, string CustomerName, string? Address, DateTime DateCreated, DateTime? DueDate, DateTime UpdatedAt);
