using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor;

/// <summary>
/// A service which performs searches for wiki content.
/// </summary>
public interface ISearchClient
{
    /// <summary>
    /// Search for wiki content which matches the given search criteria.
    /// </summary>
    /// <param name="request">
    /// A <see cref="SearchRequest"/> instance with search criteria.
    /// </param>
    /// <param name="user">
    /// The <see cref="IWikiUser"/> making the request.
    /// </param>
    /// <returns>A <see cref="SearchResult"/> instance with search results.</returns>
    Task<SearchResult> SearchAsync(SearchRequest request, IWikiUser? user);
}
