namespace Tavenem.Wiki.Blazor.Client.Internal.Models;

/// <summary>
/// Used by javascript interop.
/// </summary>
public class GifSearchResults
{
    /// <summary>
    /// A position identifier to use with the next API query, through the pos field, to retrieve the
    /// next set of results. If there are no further results, next returns an empty string.
    /// </summary>
    public string? Next { get; set; }

    /// <summary>
    /// The results.
    /// </summary>
    public GifInfo[]? Gifs { get; set; }
}
