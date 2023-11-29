using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Tavenem.Blazor.Framework;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Client.Shared;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// A common page component which displays system lists.
/// </summary>
public partial class SpecialList : OfflineSupportComponent
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
    [Parameter] public long? PageNumber { get; set; }

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
    /// The target wiki item's domain.
    /// </summary>
    [Parameter] public string? TargetDomain { get; set; }

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

    [CascadingParameter] private bool IsInteractive { get; set; }

    private IPagedList<LinkInfo>? Items { get; set; }

    [Inject, NotNull] private QueryStateService? QueryStateService { get; set; }

    private string? SecondaryDescription { get; set; }

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
            CurrentFilter = filters[0];
        }
    }

    /// <inheritdoc/>
    protected override async Task RefreshAsync()
    {
        CurrentDescending = Descending;
        CurrentFilter = Filter;
        CurrentPageNumber = (ulong)Math.Max(1, PageNumber ?? 1);
        CurrentPageSize = Math.Clamp(PageSize ?? 50, 5, 500);
        CurrentSort = string.Equals(Sort, "timestamp")
            ? Sort
            : "alpha";
        Description = null;
        Items = null;
        SecondaryDescription = null;

        if (SpecialListType == SpecialListType.What_Links_Here)
        {
            var targetTitle = new PageTitle(TargetTitle, TargetNamespace, TargetDomain);

            Description = $"The following pages link to {targetTitle}.";

            var request = new TitleRequest(
                targetTitle,
                (int)CurrentPageNumber,
                CurrentPageSize,
                Descending,
                Sort,
                Filter);
            Items = await PostAsync(
                $"{WikiBlazorClientOptions.WikiServerApiRoute}/whatlinkshere",
                request,
                WikiJsonSerializerContext.Default.TitleRequest,
                WikiBlazorJsonSerializerContext.Default.PagedListLinkInfo,
                async user => await WikiDataManager.GetWhatLinksHereAsync(request));
        }
        else
        {
            Description = SpecialListType switch
            {
                SpecialListType.All_Categories => "This page lists all categories, either alphabetically or by most recent update.",
                SpecialListType.All_Files => "This page lists all files, either alphabetically or by most recent update.",
                SpecialListType.All_Articles => "This page lists all articles, either alphabetically or by most recent update.",
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

            var request = new SpecialListRequest(
                SpecialListType,
                (int)CurrentPageNumber,
                CurrentPageSize,
                Descending,
                Sort,
                Filter);
            Items = await PostAsync(
                $"{WikiBlazorClientOptions.WikiServerApiRoute}/list",
                request,
                WikiJsonSerializerContext.Default.SpecialListRequest,
                WikiBlazorJsonSerializerContext.Default.PagedListLinkInfo,
                async user => await WikiDataManager.GetListAsync(request));
        }
    }

    private string? GetSecondaryDescription()
    {
        if (SpecialListType is SpecialListType.All_Categories
            or SpecialListType.All_Files
            or SpecialListType.All_Articles)
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

    private async Task OnDescendingChangedAsync()
    {
        Descending = CurrentDescending;

        QueryStateService.SetPropertyValue(
            "pg",
            "d",
            Descending);

        await RefreshAsync();
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

    private async Task OnFilterChangedAsync()
    {
        Filter = CurrentFilter;

        QueryStateService.SetPropertyValue(
            "pg",
            "f",
            Filter);

        await RefreshAsync();
    }

    private async Task OnFilterChangedAsync(QueryChangeEventArgs args)
    {
        if (!string.Equals(args.Value, CurrentFilter))
        {
            Filter = args.Value;
            CurrentFilter = args.Value;
            await RefreshAsync();
        }
    }

    private async Task OnNextRequestedAsync()
    {
        if (Items?.HasNextPage == true)
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
            CurrentPageSize = Math.Clamp(pageSize, 5, 500);
            PageSize = CurrentPageSize;
            await RefreshAsync();
        }
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

    private async Task OnSortChangedAsync()
    {
        Sort = string.Equals(CurrentSort, "timestamp")
            ? CurrentSort
            : null;

        QueryStateService.SetPropertyValue(
            "pg",
            "s",
            Sort);

        await RefreshAsync();
    }
}