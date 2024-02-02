using Tavenem.Wiki.Blazor.Services.Search;

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
    /// An <see cref="ISearchRequest"/> instance with search criteria.
    /// </param>
    /// <param name="user">
    /// The <see cref="IWikiUser"/> making the request.
    /// </param>
    /// <returns>An <see cref="ISearchResult"/> instance with search results.</returns>
    Task<ISearchResult> SearchAsync(ISearchRequest request, IWikiUser? user);
}
