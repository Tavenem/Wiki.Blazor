using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text;
using Tavenem.Blazor.Framework;
using Tavenem.DataStorage;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// A common page component which displays system lists.
/// </summary>
public partial class SpecialList
{
    /// <summary>
    /// Whether a requested sort is descending.
    /// </summary>
    [Parameter] public bool Descending { get; set; }

    /// <summary>
    /// The requested filter criteria.
    /// </summary>
    [Parameter] public string? Filter { get; set; }

    /// <summary>
    /// The requested page number.
    /// </summary>
    [Parameter] public int? PageNumber { get; set; }

    /// <summary>
    /// The requested page size.
    /// </summary>
    [Parameter] public int? PageSize { get; set; }

    /// <summary>
    /// The requested sort criteria.
    /// </summary>
    [Parameter] public string? Sort { get; set; }

    /// <summary>
    /// The requested list type.
    /// </summary>
    [Parameter] public SpecialListType SpecialListType { get; set; }

    /// <summary>
    /// The target wiki item's namespace.
    /// </summary>
    [Parameter] public string? TargetNamespace { get; set; }

    /// <summary>
    /// The target wiki item's title.
    /// </summary>
    [Parameter] public string? TargetTitle { get; set; }

    private bool CurrentDescending { get; set; }

    private string? CurrentFilter { get; set; }

    private ulong CurrentPageNumber { get; set; }

    private int CurrentPageSize { get; set; } = 50;

    private string? CurrentSort { get; set; }

    private string? Description { get; set; }

    [Inject] private HttpClient HttpClient { get; set; } = default!;

    private IPagedList<LinkInfo>? Items { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string? SecondaryDescription { get; set; }

    [Inject] private SnackbarService SnackbarService { get; set; } = default!;

    [Inject] private IWikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        CurrentDescending = Descending;
        CurrentFilter = Filter;
        CurrentPageNumber = (ulong)Math.Max(0, (PageNumber ?? 1) - 1);
        CurrentPageSize = PageSize ?? 50;
        CurrentSort = Sort;
        Description = null;
        Items = null;
        SecondaryDescription = null;

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;

        if (SpecialListType == SpecialListType.What_Links_Here)
        {
            Description = $"The following pages link to {Article.GetFullTitle(WikiOptions, TargetTitle ?? WikiOptions.MainPageTitle, TargetNamespace)}.";

            try
            {
                var response = await HttpClient.PostAsJsonAsync(
                    $"{serverApi}/whatlinkshere",
                    new WhatLinksHereRequest(
                        TargetTitle,
                        TargetNamespace,
                        (int)CurrentPageNumber,
                        CurrentPageSize,
                        Descending,
                        Sort,
                        Filter),
                    WikiBlazorJsonSerializerContext.Default.WhatLinksHereRequest);
                if (response.IsSuccessStatusCode)
                {
                    var listResponse = await response.Content.ReadFromJsonAsync(WikiBlazorJsonSerializerContext.Default.ListResponse);
                    Items = listResponse?.Links.ToPagedList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
            }
        }
        else
        {
            Description = SpecialListType switch
            {
                SpecialListType.All_Categories => "This page lists all categories, either alphabetically or by most recent update.",
                SpecialListType.All_Files => "This page lists all files, either alphabetically or by most recent update.",
                SpecialListType.All_Pages => "This page lists all articles, either alphabetically or by most recent update.",
                SpecialListType.All_Redirects => "This page lists all articles which redirect to another page, either alphabetically or by most recent update.",
                SpecialListType.Broken_Redirects => "This page lists all articles which redirect to an article that does not exist, either alphabetically or by most recent update.",
                SpecialListType.Double_Redirects => "This page lists all articles which redirect to a page that redirects someplace else, either alphabetically or by most recent update.",
                SpecialListType.Missing_Pages => "This page lists all pages which are linked but do not exist.",
                SpecialListType.Uncategorized_Articles => "This page lists all articles which are not categorized, either alphabetically or by most recent update.",
                SpecialListType.Uncategorized_Categories => "This page lists all categories which are not categorized, either alphabetically or by most recent update.",
                SpecialListType.Uncategorized_Files => "This page lists all files which are not categorized, either alphabetically or by most recent update.",
                SpecialListType.Unused_Categories => "This page lists all categories which have no articles or subcategories, either alphabetically or by most recent update.",
                _ => null,
            };
            SecondaryDescription = GetSecondaryDescription();

            try
            {
                var response = await HttpClient.PostAsJsonAsync(
                    $"{serverApi}/list",
                    new SpecialListRequest(
                        SpecialListType,
                        (int)CurrentPageNumber,
                        CurrentPageSize,
                        Descending,
                        Sort,
                        Filter),
                    WikiBlazorJsonSerializerContext.Default.SpecialListRequest);
                var listResponse = await response.Content.ReadFromJsonAsync(WikiBlazorJsonSerializerContext.Default.ListResponse);
                Items = listResponse?.Links.ToPagedList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
            }
        }
    }

    private string? GetSecondaryDescription()
    {
        if (SpecialListType is SpecialListType.All_Categories
            or SpecialListType.All_Files
            or SpecialListType.All_Pages)
        {
            if (!string.IsNullOrEmpty(WikiOptions.ContentsPageTitle))
            {
                return new StringBuilder("For a more organized overview you may wish to check the <a href=\"")
                    .Append(WikiState.Link(WikiOptions.ContentsPageTitle, WikiOptions.SystemNamespace))
                    .Append("\" class=\"wiki-link wiki-link-exists\">")
                    .Append(WikiOptions.ContentsPageTitle)
                    .Append("</a> page.")
                    .ToString();
            }
        }
        else if (SpecialListType == SpecialListType.Uncategorized_Categories)
        {
            var sb = new StringBuilder("Note that top-level categories might show up in this list deliberately, and may not require categorization.");
            if (!string.IsNullOrEmpty(WikiOptions.ContentsPageTitle))
            {
                sb.Append("Top-level categories are typically linked on the <a href=\"")
                    .Append(WikiState.Link(WikiOptions.ContentsPageTitle, WikiOptions.SystemNamespace))
                    .Append("\" class=\"wiki-link wiki-link-exists\">")
                    .Append(WikiOptions.ContentsPageTitle)
                    .Append("</a>, or in some other prominent place (such as the <a href=\"")
                    .Append(WikiState.Link())
                    .Append("\">")
                    .Append(WikiOptions.MainPageTitle)
                    .Append("</a>).");
            }
            return sb.ToString();
        }
        else if (SpecialListType == SpecialListType.Unused_Categories)
        {
            return "Note that some categories may be intended to classify articles with problems, and might show up in this list deliberately when no such issues currently exist. These types of categories should not be removed even when empty.";
        }
        return null;
    }

    private void OnDescendingChanged()
        => Navigation.NavigateTo(Navigation.GetUriWithQueryParameter(nameof(Wiki.Descending), CurrentDescending));

    private void OnFilterChanged()
        => Navigation.NavigateTo(Navigation.GetUriWithQueryParameter(nameof(Wiki.Filter), CurrentFilter));

    private void OnNextRequested()
    {
        if (Items?.HasNextPage == true)
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

    private void OnSortChanged()
        => Navigation.NavigateTo(Navigation.GetUriWithQueryParameter(
            nameof(Wiki.Sort),
            string.Equals(CurrentSort, "timestamp")
                ? CurrentSort
                : null));
}