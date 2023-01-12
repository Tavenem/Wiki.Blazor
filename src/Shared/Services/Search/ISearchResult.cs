using Tavenem.DataStorage;

namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// The result of a search operation.
/// </summary>
public interface ISearchResult
{
    /// <summary>
    /// Whether the results are in descending order.
    /// </summary>
    bool Descending { get; set; }

    /// <summary>
    /// The originally specified wiki namespace.
    /// </summary>
    string? Namespace { get; set; }

    /// <summary>
    /// The original search query.
    /// </summary>
    string? Query { get; set; }

    /// <summary>
    /// An <see cref="IPagedList{T}"/> of <see cref="ISearchHit"/> representing the results.
    /// </summary>
    IPagedList<ISearchHit> SearchHits { get; set; }

    /// <summary>
    /// The originally specified sort property.
    /// </summary>
    string? Sort { get; set; }

    /// <summary>
    /// The originally specified owner.
    /// </summary>
    string? Owner { get; set; }
}
