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
    bool Descending { get; }

    /// <summary>
    /// The originally specified wiki namespace.
    /// </summary>
    string? Namespace { get; }

    /// <summary>
    /// The original search query.
    /// </summary>
    string? Query { get; }

    /// <summary>
    /// An <see cref="IPagedList{T}"/> of <see cref="ISearchHit"/> representing the results.
    /// </summary>
    IPagedList<ISearchHit> SearchHits { get; }

    /// <summary>
    /// The originally specified sort property.
    /// </summary>
    string? Sort { get; }

    /// <summary>
    /// The originally specified owner.
    /// </summary>
    string? Owner { get; }
}
