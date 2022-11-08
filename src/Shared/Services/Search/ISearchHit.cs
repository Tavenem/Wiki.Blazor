namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// A search hit.
/// </summary>
public interface ISearchHit
{
    /// <summary>
    /// The domain of the matching wiki item (if any).
    /// </summary>
    string? Domain { get; set; }

    /// <summary>
    /// An excerpt from the matching article (optional).
    /// </summary>
    string? Excerpt { get; set; }

    /// <summary>
    /// Gets the full title of this item (including namespace if the namespace is not
    /// <see cref="WikiOptions.DefaultNamespace"/>).
    /// </summary>
    string FullTitle { get; set; }

    /// <summary>
    /// The title of the matching wiki item.
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// The namespace of the matching wiki item.
    /// </summary>
    string WikiNamespace { get; set; }
}