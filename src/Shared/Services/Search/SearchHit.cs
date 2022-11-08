namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// A search hit.
/// </summary>
public class SearchHit : ISearchHit
{
    /// <summary>
    /// The domain of the matching wiki item (if any).
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// An excerpt from the matching article (optional).
    /// </summary>
    public string? Excerpt { get; set; }

    /// <summary>
    /// Gets the full title of this item (including namespace if the namespace is not
    /// <see cref="WikiOptions.DefaultNamespace"/>).
    /// </summary>
    public string FullTitle { get; set; }

    /// <summary>
    /// The title of the matching wiki item.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The namespace of the matching wiki item.
    /// </summary>
    public string WikiNamespace { get; set; }

    /// <summary>
    /// Initialize a new instance of <see cref="SearchHit"/>.
    /// </summary>
    /// <param name="title">
    /// The title of the matching wiki item.
    /// </param>
    /// <param name="wikiNamespace">
    /// The namespace of the matching wiki item.
    /// </param>
    /// <param name="domain">
    /// The domain of the matching wiki item (if any).
    /// </param>
    /// <param name="fullTitle">
    /// The full title of the matching wiki item.
    /// </param>
    /// <param name="excerpt">
    /// An excerpt from the matching article (optional).
    /// </param>
    public SearchHit(string title, string wikiNamespace, string? domain, string fullTitle, string? excerpt = null)
    {
        Excerpt = excerpt;
        FullTitle = fullTitle;
        Title = title;
        WikiNamespace = wikiNamespace;
        Domain = domain;
    }
}
