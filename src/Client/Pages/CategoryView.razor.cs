using Microsoft.AspNetCore.Components;
using System.Text;
using Tavenem.Wiki.Blazor.Client.Shared;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The category page.
/// </summary>
public partial class CategoryView : OfflineSupportComponent
{
    private CategoryInfo? CategoryInfo { get; set; }

    private MarkupString? Content { get; set; }

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
        => RefreshAsync();

    /// <inheritdoc/>
    protected override async Task RefreshAsync()
    {
        if (string.IsNullOrEmpty(WikiState.WikiTitle)
            || !string.Equals(
            WikiState.WikiNamespace,
            WikiOptions.CategoryNamespace,
            StringComparison.OrdinalIgnoreCase))
        {
            CategoryInfo = null;
            Content = null;
            return;
        }

        CategoryInfo = await FetchDataAsync(
            new StringBuilder(WikiBlazorClientOptions.WikiServerApiRoute)
                .Append("/category?title=")
                .Append(WikiState.WikiTitle)
                .ToString(),
            WikiBlazorJsonSerializerContext.Default.CategoryInfo,
            user => WikiDataManager.GetCategoryAsync(
                user,
                WikiState.WikiTitle,
                WikiState.WikiDomain));
        Content = string.IsNullOrEmpty(CategoryInfo?.Item?.Html)
            ? null
            : new MarkupString(CategoryInfo.Item.Html);
    }
}