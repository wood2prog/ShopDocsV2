namespace ShopDocsV2.Application;

/// <summary>
/// One-time, user-triggered hex color lookup used from the Catalog Manager's "Lookup Hex" action.
/// Unlike the original app, this is never called per-keystroke while editing a job.
/// </summary>
public interface IPaintColorLookupService
{
    /// <summary>Returns the hex color (e.g. "#a1b2c3") for the given color name, or null if not found / on any failure.</summary>
    Task<string?> LookupHexAsync(string colorName, CancellationToken ct = default);
}
