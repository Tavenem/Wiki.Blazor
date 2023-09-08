using Microsoft.AspNetCore.Components;
using System.Text;
using Tavenem.Wiki.Blazor.Client.Shared;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The category page.
/// </summary>
public partial class CategoryView : OfflineSupportComponent
{
    private Category? Category { get; set; }

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
            Category = null;
            Content = null;
            return;
        }

        Category = await FetchDataAsync(
            new StringBuilder(WikiBlazorClientOptions.WikiServerApiRoute)
                .Append("/category?title=")
                .Append(WikiState.WikiTitle)
                .ToString(),
            WikiJsonSerializerContext.Default.Category,
            async user => await WikiDataManager.GetCategoryAsync(
                user,
                new PageTitle(WikiState.WikiTitle, WikiOptions.CategoryNamespace, WikiState.WikiDomain)));
        Content = string.IsNullOrEmpty(Category?.DisplayHtml)
            ? null
            : new MarkupString(Category.DisplayHtml);
    }
}