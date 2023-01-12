using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Services.Search;

namespace Tavenem.Wiki.Blazor.Models;

/// <summary>
/// The result of a search.
/// </summary>
public record SearchResponse(
    bool Descending,
    string? Query,
    PagedListDTO<SearchHit> SearchHits,
    string? Sort,
    string? Owner,
    string? Namespace,
    string? Domain,
    Page? ExactMatch = null);
