namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// A search hit.
/// </summary>
public interface ISearchHit
{
    /// <summary>
    /// An excerpt from the matching article (optional).
    /// </summary>
    string? Excerpt { get; set; }

    /// <summary>
    /// The title of the matching wiki item.
    /// </summary>
    PageTitle Title { get; set; }
}