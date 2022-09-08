namespace Tavenem.Wiki.Blazor.Client.Internal.Models;

/// <summary>
/// Used by javascript interop.
/// </summary>
public class GifInfo
{
    /// <summary>
    /// Tenor result identifier.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The title of the post.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// A URL to the media source.
    /// </summary>
    public string? Url { get; set; }
}
