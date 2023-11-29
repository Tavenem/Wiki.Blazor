using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Blazor.Client.Shared;
using Tavenem.Wiki.Blazor.Services.Search;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The search page.
/// </summary>
public partial class Search : OfflineSupportComponent
{
    /// <summary>
    /// Whether a requested sort is descending.
    /// </summary>
    [Parameter] public bool Descending { get; set; }

    /// <summary>
    /// The requested page number.
    /// </summary>
    [Parameter] public long? PageNumber { get; set; }

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

    private bool CurrentDescending { get; set; }

    private string? CurrentDomain { get; set; }

    private string? CurrentNamespace { get; set; }

    private string? CurrentOwner { get; set; }

    private ulong CurrentPageNumber { get; set; }

    private int CurrentPageSize { get; set; } = 50;

    private string? CurrentQuery { get; set; }

    private string? CurrentSort { get; set; }

    private List<IWikiOwner> DeselectedOwners { get; set; } = [];

    [CascadingParameter] private bool IsInteractive { get; set; }

    private Page? ExactMatch { get; set; }

    [Inject, NotNull] private QueryStateService? QueryStateService { get; set; }

    private ISearchResult? Result { get; set; }

    [Inject, NotNull] ISearchClient? SearchClient { get; set; }

    private List<IWikiOwner> SelectedOwners { get; set; } = [];

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
        => RefreshAsync();

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        var pageSizes = QueryStateService.RegisterProperty(
            "pg",
            "ps",
            OnPageSizeChangedAsync,
            50);
        if (pageSizes?.Count > 0
            && int.TryParse(pageSizes[0], out var pageSize))
        {
            CurrentPageSize = Math.Clamp(pageSize, 5, 500);
        }

        var sorts = QueryStateService.RegisterProperty(
            "pg",
            "s",
            OnSortChangedAsync,
            50);
        if (sorts?.Count > 0)
        {
            CurrentSort = sorts[0];
        }

        var descendings = QueryStateService.RegisterProperty(
            "pg",
            "d",
            OnDescendingChangedAsync,
            false);
        if (descendings?.Count > 0
            && bool.TryParse(descendings[0], out var descending))
        {
            CurrentDescending = descending;
        }

        var filters = QueryStateService.RegisterProperty(
            "pg",
            "f",
            OnFilterChangedAsync,
            false);
        if (filters?.Count > 0)
        {
            CurrentQuery = filters[0];
        }
    }

    /// <inheritdoc/>
    protected override async Task RefreshAsync()
    {
        CurrentDescending = Descending;
        CurrentDomain = SearchDomain;
        CurrentNamespace = SearchNamespace;
        CurrentOwner = SearchOwner;
        CurrentQuery = Query?.Trim();
        CurrentPageNumber = (ulong)Math.Max(1, PageNumber ?? 1);
        CurrentPageSize = Math.Clamp(PageSize ?? 50, 5, 200);
        CurrentSort = string.Equals(Sort, "timestamp")
            ? Sort
            : "default";
        Result = null;
        ExactMatch = null;

        if (string.IsNullOrWhiteSpace(CurrentQuery))
        {
            return;
        }

        var request = new SearchRequest()
        {
            Descending = Descending,
            Owner = CurrentOwner,
            PageNumber = (int)(CurrentPageNumber + 1),
            PageSize = CurrentPageSize,
            Query = CurrentQuery,
            Sort = CurrentSort,
            Namespace = CurrentNamespace,
            Domain = CurrentDomain,
        };
        var results = await PostAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/search",
            request,
            WikiBlazorJsonSerializerContext.Default.SearchRequest,
            WikiBlazorJsonSerializerContext.Default.SearchResult,
            async user => SearchClient is null
                ? null
                : await WikiDataManager.SearchAsync(SearchClient, user, request));
        ExactMatch = results?.ExactMatch;
        Result = results;
    }

    private async Task<IEnumerable<KeyValuePair<string, object>>> GetSearchSuggestions(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Enumerable.Empty<KeyValuePair<string, object>>();
        }

        var suggestions = await FetchDataAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/searchsuggest?input={input}",
            WikiBlazorJsonSerializerContext.Default.ListString,
            async user => await WikiDataManager.GetSearchSuggestionsAsync(
                SearchClient,
                user,
                input));
        return suggestions?.Select(x => new KeyValuePair<string, object>(x, x))
            ?? Enumerable.Empty<KeyValuePair<string, object>>();
    }

    private async Task OnDescendingChangedAsync(QueryChangeEventArgs args)
    {
        if (bool.TryParse(args.Value, out var descending)
            && descending != CurrentDescending)
        {
            CurrentDescending = descending;
            Descending = descending;
            await RefreshAsync();
        }
    }

    private async Task OnFilterChangedAsync(QueryChangeEventArgs args)
    {
        if (!string.Equals(args.Value, CurrentQuery))
        {
            Query = args.Value;
            CurrentQuery = args.Value;
            await RefreshAsync();
        }
    }

    private async Task OnNextRequestedAsync()
    {
        if (Result?.SearchHits?.HasNextPage == true)
        {
            CurrentPageNumber++;
            PageNumber = (long)CurrentPageNumber;
            await RefreshAsync();
        }
    }

    private async Task OnPageNumberChangedAsync()
    {
        PageNumber = (long)CurrentPageNumber;
        await RefreshAsync();
    }

    private async Task OnPageSizeChangedAsync()
    {
        PageSize = CurrentPageSize;

        QueryStateService.SetPropertyValue(
            "pg",
            "ps",
            CurrentPageSize);

        await RefreshAsync();
    }

    private async Task OnPageSizeChangedAsync(QueryChangeEventArgs args)
    {
        if (int.TryParse(args.Value, out var pageSize)
            && pageSize != CurrentPageSize)
        {
            CurrentPageSize = Math.Clamp(pageSize, 5, 200);
            PageSize = CurrentPageSize;
            await RefreshAsync();
        }
    }

    private async Task OnSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentQuery))
        {
            return;
        }

        Query = CurrentQuery.Trim();
        QueryStateService.SetPropertyValue(
            "pg",
            "f",
            Query);

        PageNumber = 1;
        CurrentPageNumber = 1;
        QueryStateService.SetPropertyValue(
            "pg",
            "p",
            Query);

        await RefreshAsync();
    }

    private async Task OnSetSearchAsync()
    {
        Descending = CurrentDescending;
        QueryStateService.SetPropertyValue(
            "pg",
            "d",
            CurrentDescending);

        Descending = CurrentDescending;
        QueryStateService.SetPropertyValue(
            "pg",
            "d",
            CurrentDescending);

        SearchDomain = CurrentDomain;
        QueryStateService.SetPropertyValue(
            "s",
            "d",
            CurrentDomain);

        SearchNamespace = CurrentNamespace;
        QueryStateService.SetPropertyValue(
            "s",
            "n",
            CurrentNamespace);

        string? owners = null;
        if (SelectedOwners is not null)
        {
            owners = string.Join(';', SelectedOwners.Select(x => x.Id));
        }
        if (DeselectedOwners is not null)
        {
            var nonOwners = string.Join(';', DeselectedOwners.Select(x => $"!{x.Id}"));
            if (owners is null)
            {
                owners = nonOwners;
            }
            else
            {
                owners += ';' + nonOwners;
            }
        }

        CurrentOwner = owners;
        SearchOwner = CurrentOwner;
        QueryStateService.SetPropertyValue(
            "s",
            "o",
            owners);

        Sort = string.Equals(CurrentSort, "timestamp")
            ? CurrentSort
            : null;
        QueryStateService.SetPropertyValue(
            "pg",
            "s",
            Sort);

        await RefreshAsync();
    }

    private async Task OnSortChangedAsync(QueryChangeEventArgs args)
    {
        if (!string.Equals(args.Value, CurrentSort))
        {
            Sort = args.Value;
            CurrentSort = args.Value;
            await RefreshAsync();
        }
    }
}