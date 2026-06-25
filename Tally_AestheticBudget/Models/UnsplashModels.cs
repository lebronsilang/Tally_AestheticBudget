namespace Tally_AestheticBudget.Models;

/// <summary>
/// A single photo result from the Unsplash Search API.
/// ThumbUrl is loaded in the in-drawer thumbnail grid; RegularUrl is
/// downloaded to disk when the user taps a thumbnail.
/// </summary>
public record UnsplashPhoto(
    string Id,
    string ThumbUrl,
    string RegularUrl,
    string AltDescription
);

/// <summary>
/// Wraps a page of Unsplash search results together with the total
/// page count so the ViewModel can decide whether to show "Load more".
/// </summary>
public record UnsplashResult(List<UnsplashPhoto> Photos, int TotalPages);