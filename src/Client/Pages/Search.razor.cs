using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Shared;
using Tavenem.Wiki.Blazor.Services.Search;
using Tavenem.Wiki.Queries;

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

    private bool CurrentDescending { get; set; }

    private string? CurrentDomain { get; set; }

    private string? CurrentNamespace { get; set; }

    private string? CurrentOwner { get; set; }

    private ulong CurrentPageNumber { get; set; }

    private int CurrentPageSize { get; set; } = 50;

    private string? CurrentQuery { get; set; }

    private string? CurrentSort { get; set; }

    private List<WikiUserInfo> DeselectedOwners { get; set; } = new();

    private Page? ExactMatch { get; set; }

    private ISearchResult? Result { get; set; }

    [Inject] ISearchClient SearchClient { get; set; } = default!;

    private List<WikiUserInfo> SelectedOwners { get; set; } = new();

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
        => RefreshAsync();

    /// <inheritdoc/>
    protected override async Task RefreshAsync()
    {
        CurrentDescending = Descending;
        CurrentDomain = SearchDomain;
        CurrentNamespace = SearchNamespace;
        CurrentOwner = SearchOwner;
        CurrentQuery = Query?.Trim() ?? string.Empty;
        CurrentPageNumber = (ulong)Math.Max(0, (PageNumber ?? 1) - 1);
        CurrentPageSize = PageSize ?? 50;
        CurrentSort = Sort;
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
            WikiBlazorJsonSerializerContext.Default.SearchResponse,
            async user => SearchClient is null
                ? null
                : await WikiDataManager.SearchAsync(SearchClient, user, request));
        ExactMatch = results?.ExactMatch;
        Result = results is null
            ? null
            : new SearchResult
            {
                Descending = results.Descending,
                Owner = results.Owner,
                Query = results.Query,
                SearchHits = results.SearchHits.ToPagedList(),
                Sort = results.Sort,
                Namespace = results.Namespace,
                Domain = results.Domain,
            };
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "List<string> will not be trimmed.")]
    private async Task<IEnumerable<KeyValuePair<string, object>>> GetSearchSuggestions(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Enumerable.Empty<KeyValuePair<string, object>>();
        }

        var suggestions = await FetchDataAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/searchsuggest?input={input}",
            async user => await WikiDataManager.GetSearchSuggestionsAsync(
                SearchClient,
                user,
                input));
        return suggestions?.Select(x => new KeyValuePair<string, object>(x, x))
            ?? Enumerable.Empty<KeyValuePair<string, object>>();
    }

    private void OnNextRequested()
    {
        if (Result?.SearchHits.HasNextPage == true)
        {
            CurrentPageNumber++;
            NavigationManager.NavigateTo(
                NavigationManager.GetUriWithQueryParameter(
                    nameof(Wiki.PageNumber),
                    (int)(CurrentPageNumber + 1)));
        }
    }

    private void OnPageNumberChanged() => NavigationManager.NavigateTo(
        NavigationManager.GetUriWithQueryParameter(
            nameof(Wiki.PageNumber),
            (int)(CurrentPageNumber + 1)));

    private void OnPageSizeChanged() => NavigationManager.NavigateTo(
        NavigationManager.GetUriWithQueryParameter(
            nameof(Wiki.PageSize),
            CurrentPageSize));

    private void OnSearch()
    {
        if (string.IsNullOrWhiteSpace(CurrentQuery))
        {
            return;
        }

        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            { nameof(Wiki.Filter), CurrentQuery.Trim() },
            { nameof(Wiki.PageNumber), 1 },
        }));
    }

    private void OnSetSearch()
    {
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
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            { nameof(Wiki.Descending), CurrentDescending },
            { nameof(Wiki.SearchDomain), CurrentDomain },
            { nameof(Wiki.SearchNamespace), CurrentNamespace },
            { nameof(Wiki.SearchOwner), owners },
            { nameof(Wiki.Sort), string.Equals(CurrentSort, "timestamp")
                ? CurrentSort
                : null },
        }));
    }
}