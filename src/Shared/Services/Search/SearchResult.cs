using Tavenem.DataStorage;

namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// The result of a search operation.
/// </summary>
/// <param name="Descending">
/// Whether the results are in descending order.
/// </param>
/// <param name="Query">
/// The original search query.
/// </param>
/// <param name="SearchHits">
/// A <see cref="PagedList{T}" /> of <see cref="SearchHit" /> instances representing the results.
/// </param>
/// <param name="Sort">
/// The originally specified sort property.
/// </param>
/// <param name="Owner">
/// The originally specified owner.
/// </param>
/// <param name="Namespace">
/// An originally specified wiki namespace.
/// </param>
/// <param name="Domain">
/// An originally specified wiki domain.
/// </param>
/// <param name="ExactMatch"></param>
public record SearchResult(
    bool Descending = false,
    string? Query = null,
    PagedList<SearchHit>? SearchHits = null,
    string? Sort = null,
    string? Owner = null,
    string? Namespace = null,
    string? Domain = null,
    Page? ExactMatch = null) : ISearchResult
{
    /// <summary>
    /// An <see cref="IPagedList{T}" /> of <see cref="ISearchHit" /> representing the results.
    /// </summary>
    IPagedList<ISearchHit> ISearchResult.SearchHits
        => SearchHits ?? new PagedList<SearchHit>(null, 1, 50, 0);
}
