using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

internal static class JobHeader
{
    /// <summary>The label/value lines at the top of a printed or text-exported job spec, skipping blank fields.</summary>
    public static IReadOnlyList<(string Label, string Value)> Fields(Job job) =>
        new (string Label, string? Value)[]
        {
            ("Customer", job.CustomerName),
            ("Phone", job.CustomerPhone),
            ("Email", job.CustomerEmail),
            ("Address", job.Address),
            ("Date Created", job.DateCreated.ToString("yyyy-MM-dd")),
            ("Due Date", job.DueDate?.ToString("yyyy-MM-dd"))
        }
        .Where(f => !string.IsNullOrWhiteSpace(f.Value))
        .Select(f => (f.Label, f.Value!))
        .ToList();
}
