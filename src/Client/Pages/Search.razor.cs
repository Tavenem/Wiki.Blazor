using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using Tavenem.Blazor.Framework;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Services.Search;
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

    private string? CurrentNamespace { get; set; }

    private string? CurrentOwner { get; set; }

    private ulong CurrentPageNumber { get; set; }

    private int CurrentPageSize { get; set; } = 50;

    private string? CurrentQuery { get; set; }

    private string? CurrentSort { get; set; }

    private List<WikiUserInfo> DeselectedOwners { get; set; } = new();

    private Article? ExactMatch { get; set; }

    [Inject] private HttpClient HttpClient { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private ISearchResult? Result { get; set; }

    private List<WikiUserInfo> SelectedOwners { get; set; } = new();

    [Inject] private SnackbarService SnackbarService { get; set; } = default!;

    [Inject] private IWikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        CurrentDescending = Descending;
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

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        try
        {
            var response = await HttpClient.PostAsJsonAsync(
                $"{serverApi}/search",
                new SearchRequest()
                {
                    Descending = Descending,
                    Owner = CurrentOwner,
                    PageNumber = (int)(CurrentPageNumber + 1),
                    PageSize = CurrentPageSize,
                    Query = CurrentQuery,
                    Sort = CurrentSort,
                    WikiNamespace = CurrentNamespace,
                },
                WikiBlazorJsonSerializerContext.Default.SearchRequest);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                WikiState.NotAuthorized = true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                Result = new SearchResult()
                {
                    Descending = Descending,
                    Owner = CurrentOwner,
                    Query = CurrentQuery,
                    SearchHits = new PagedList<SearchHit>(null, 0, 0, 0),
                    Sort = CurrentSort,
                    WikiNamespace = CurrentNamespace,
                };
            }
            else
            {
                var results = await response.Content.ReadFromJsonAsync(WikiBlazorJsonSerializerContext.Default.SearchResponse);
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
                        WikiNamespace = results.WikiNamespace,
                    };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            SnackbarService.Add("An error occurred", ThemeColor.Danger);
        }
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
        var (_, title, _, _) = Article.GetTitleParts(WikiOptions, input);
        if (string.IsNullOrEmpty(title))
        {
            return Enumerable.Empty<KeyValuePair<string, object>>();
        }

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        List<string>? response = null;
        try
        {
            response = await HttpClient.GetFromJsonAsync<List<string>>($"{serverApi}/searchsuggest?input={input}");
        }
        catch { }
        return response?.Select(x => new KeyValuePair<string, object>(x, x))
            ?? Enumerable.Empty<KeyValuePair<string, object>>();
    }

    private void OnNextRequested()
    {
        if (Result?.SearchHits.HasNextPage == true)
        {
            CurrentPageNumber++;
            Navigation.NavigateTo(
                Navigation.GetUriWithQueryParameter(
                    nameof(Wiki.PageNumber),
                    (int)(CurrentPageNumber + 1)));
        }
    }

    private void OnPageNumberChanged() => Navigation.NavigateTo(
        Navigation.GetUriWithQueryParameter(
            nameof(Wiki.PageNumber),
            (int)(CurrentPageNumber + 1)));

    private void OnPageSizeChanged() => Navigation.NavigateTo(
        Navigation.GetUriWithQueryParameter(
            nameof(Wiki.PageSize),
            CurrentPageSize));

    private void OnSearch()
    {
        if (string.IsNullOrWhiteSpace(CurrentQuery))
        {
            return;
        }

        Navigation.NavigateTo(Navigation.GetUriWithQueryParameters(new Dictionary<string, object?>
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
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            { nameof(Wiki.Descending), CurrentDescending },
            { nameof(Wiki.SearchNamespace), CurrentNamespace },
            { nameof(Wiki.SearchOwner), owners },
            { nameof(Wiki.Sort), string.Equals(CurrentSort, "timestamp")
                ? CurrentSort
                : null },
        }));
    }
}