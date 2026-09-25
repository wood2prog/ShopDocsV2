using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

public interface IJobRepository
{
    Task<Guid> CreateJobAsync(Job job);
    Task<Job?> GetJobAsync(Guid id);

    /// <summary>Full upsert of the job graph (job + rooms + answers + list items). Used by both autosave and the explicit Save action.</summary>
    Task SaveJobAsync(Job job);

    Task DeleteJobAsync(Guid id);
    Task<IReadOnlyList<JobSummary>> ListJobsAsync();

    /// <summary>Clones a job's entire graph into a new job record. Replaces the original app's file-based "Save As".</summary>
    Task<Guid> DuplicateJobAsync(Guid sourceJobId);
}

public sealed record JobSummary(Guid Id, string CustomerName, string? Address, DateTime DateCreated, DateTime? DueDate, DateTime UpdatedAt);
