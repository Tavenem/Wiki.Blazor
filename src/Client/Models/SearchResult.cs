using Tavenem.DataStorage;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Models;

/// <summary>
/// The result of a search operation.
/// </summary>
/// <param name="Request">The original search request.</param>
/// <param name="SearchHits">
/// A <see cref="PagedList{T}" /> of <see cref="SearchHit" /> instances representing the results.
/// </param>
/// <param name="ExactMatch">A page whose title matched the search query exactly.</param>
public record SearchResult(
    SearchRequest Request,
    PagedList<SearchHit> SearchHits,
    Page? ExactMatch = null);
