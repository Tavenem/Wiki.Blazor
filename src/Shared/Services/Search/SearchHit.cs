namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// A search hit.
/// </summary>
/// <param name="title">
/// The title of the matching wiki item.
/// </param>
/// <param name="excerpt">
/// An excerpt from the matching article (optional).
/// </param>
public class SearchHit(PageTitle title, string? excerpt = null) : ISearchHit
{
    /// <summary>
    /// An excerpt from the matching article (optional).
    /// </summary>
    public string? Excerpt { get; set; } = excerpt;

    /// <summary>
    /// The title of the matching wiki item.
    /// </summary>
    public PageTitle Title { get; set; } = title;
}
