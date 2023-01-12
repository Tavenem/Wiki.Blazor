namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// A search hit.
/// </summary>
public class SearchHit : ISearchHit
{
    /// <summary>
    /// An excerpt from the matching article (optional).
    /// </summary>
    public string? Excerpt { get; set; }

    /// <summary>
    /// The title of the matching wiki item.
    /// </summary>
    public PageTitle Title { get; set; }

    /// <summary>
    /// Initialize a new instance of <see cref="SearchHit"/>.
    /// </summary>
    /// <param name="title">
    /// The title of the matching wiki item.
    /// </param>
    /// <param name="excerpt">
    /// An excerpt from the matching article (optional).
    /// </param>
    public SearchHit(PageTitle title, string? excerpt = null)
    {
        Excerpt = excerpt;
        Title = title;
    }
}
