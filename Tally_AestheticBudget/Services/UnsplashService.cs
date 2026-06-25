using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Tally_AestheticBudget.Models;

namespace Tally_AestheticBudget.Services;

/// <summary>
/// Wraps the Unsplash Search and Download APIs.
/// Registered as a typed HttpClient in MauiProgram.cs:
///   BaseAddress = https://api.unsplash.com/
///   Authorization = Client-ID {Secrets.UnsplashAccessKey}
/// </summary>
/// 
public interface IUnsplashService
{
    /// <summary>
    /// Searches Unsplash for <paramref name="query"/> and returns one page of results.
    /// </summary>
    Task<UnsplashResult> SearchAsync(string query, int page, int perPage = 20);

    /// <summary>
    /// Fires the Unsplash tracking endpoint for <paramref name="photo"/>,
    /// downloads the regular-resolution image, saves it to AppDataDirectory
    /// with a Guid filename, and returns the absolute local path.
    /// Returns null on failure.
    /// </summary>
    Task<string?> DownloadAndSaveAsync(UnsplashPhoto photo);
}


public class UnsplashService : IUnsplashService
{
    private readonly HttpClient _http;

    public UnsplashService()
    {
        _http = new System.Net.Http.HttpClient
        {
            BaseAddress = new Uri("https://api.unsplash.com/")
        };
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Client-ID", Secrets.UnsplashAccessKey);
    }

    public async Task<UnsplashResult> SearchAsync(string query, int page, int perPage = 20)
    {
        var endpoint = $"search/photos?query={Uri.EscapeDataString(query)}&page={page}&per_page={perPage}";
        var response = await _http.GetFromJsonAsync<UnsplashSearchResponse>(endpoint);

        if (response is null) return new UnsplashResult([], 0);

        var photos = response.Results.Select(r => new UnsplashPhoto(
            r.Id,
            r.Urls.Thumb,
            r.Urls.Regular,
            r.AltDescription ?? string.Empty
        )).ToList();

        return new UnsplashResult(photos, response.TotalPages);
    }

    public async Task<string?> DownloadAndSaveAsync(UnsplashPhoto photo)
    {
        // Required by Unsplash API guidelines: fire the tracking endpoint first.
        // Failure is non-fatal — the download proceeds regardless.
        try { await _http.GetAsync($"photos/{photo.Id}/download"); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unsplash tracking ping failed: {ex.Message}");
        }

        // photo.RegularUrl is an absolute CDN URL; it overrides BaseAddress on HttpClient.
        var bytes = await _http.GetByteArrayAsync(photo.RegularUrl);
        var localPath = Path.Combine(FileSystem.AppDataDirectory, $"{Guid.NewGuid():N}.jpg");
        await File.WriteAllBytesAsync(localPath, bytes);
        return localPath;
    }

    // ── Private DTOs ─────────────────────────────────────────────────────────

    private sealed record UnsplashSearchResponse(
        [property: JsonPropertyName("results")] List<UnsplashPhotoDto> Results,
        [property: JsonPropertyName("total_pages")] int TotalPages
    );

    private sealed record UnsplashPhotoDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("urls")] UnsplashUrls Urls,
        [property: JsonPropertyName("alt_description")] string? AltDescription
    );

    private sealed record UnsplashUrls(
        [property: JsonPropertyName("thumb")] string Thumb,
        [property: JsonPropertyName("regular")] string Regular
    );
}