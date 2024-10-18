using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Services;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The search page.
/// </summary>
public partial class Search
{
    /// <summary>
    /// Whether a requested sort is descending.
    /// </summary>
    [Parameter] public bool Descending { get; set; }

    /// <summary>
    /// The requested page number.
    /// </summary>
    [Parameter] public int? PageNumber { get; set; }

    /// <summary>
    /// The requested page size.
    /// </summary>
    [Parameter] public int? PageSize { get; set; }

    /// <summary>
    /// The search query.
    /// </summary>
    [Parameter] public string? Query { get; set; }

    /// <summary>
    /// A domain to search.
    /// </summary>
    [Parameter] public string? SearchDomain { get; set; }

    /// <summary>
    /// A namespace to search.
    /// </summary>
    [Parameter] public string? SearchNamespace { get; set; }

    /// <summary>
    /// An owner to search.
    /// </summary>
    [Parameter] public string? SearchOwner { get; set; }

    /// <summary>
    /// The requested sort criteria.
    /// </summary>
    [Parameter] public string? Sort { get; set; }

    private ulong CurrentPageNumber { get; set; }

    private int CurrentPageSize { get; set; } = 50;

    private string? CurrentQuery { get; set; }

    private Page? ExactMatch { get; set; }

    private SearchResult? Result { get; set; }

    [Inject, NotNull] private WikiDataService? WikiDataService { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        CurrentQuery = Query?.Trim();
        CurrentPageNumber = (ulong)Math.Max(1, PageNumber ?? 1);
        CurrentPageSize = Math.Clamp(PageSize ?? 50, 5, 200);
        Result = null;
        ExactMatch = null;

        if (string.IsNullOrWhiteSpace(CurrentQuery))
        {
            return;
        }

        var results = await WikiDataService.SearchAsync(new SearchRequest(
            CurrentQuery,
            SearchDomain,
            SearchNamespace,
            (int)CurrentPageNumber,
            CurrentPageSize,
            SearchOwner));
        ExactMatch = results?.ExactMatch;
        Result = results;
    }
}