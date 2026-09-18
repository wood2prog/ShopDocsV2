using System.Net.Http.Json;
using System.Text.Json;
using ShopDocsV2.Application;

namespace ShopDocsV2.Infrastructure.Apis;

/// <summary>Ported from the original app's fetchPaintColorHex, minus its per-keystroke cache (this is now a one-time, user-triggered lookup).</summary>
public sealed class CompositePaintColorClient(HttpClient httpClient) : IPaintColorLookupService
{
    private const string SearchUrl = "https://compositepaint.com/api/colors/search";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<string?> LookupHexAsync(string colorName, CancellationToken ct = default)
    {
        var query = colorName.Trim();
        if (query.Length == 0)
        {
            return null;
        }

        try
        {
            var url = $"{SearchUrl}?q={Uri.EscapeDataString(query)}&limit=1";
            using var response = await httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<SearchResponseDto>(JsonOptions, ct);
            return result?.Results is { Length: > 0 } results ? results[0].Hex : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    private sealed class SearchResponseDto
    {
        public ColorResultDto[]? Results { get; set; }
    }

    private sealed class ColorResultDto
    {
        public string? Hex { get; set; }
    }
}
