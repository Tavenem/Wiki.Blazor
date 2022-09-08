namespace Tavenem.Wiki.Blazor.Client.Internal.Models;

/// <summary>
/// Used by javascript interop.
/// </summary>
public class GifCategory
{
    /// <summary>
    /// A URL to the media source for the category's example GIF.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// The display text.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The search term that corresponds to the category. The search term is translated to match the
    /// locale of the corresponding request.
    /// </summary>
    public string? SearchTerm { get; set; }
}