using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The category page.
/// </summary>
public partial class CategoryView
{
    private Category? Category { get; set; }

    private MarkupString? Content { get; set; }

    [Inject, NotNull] private WikiDataService? WikiDataService { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
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

        Category = await WikiDataService.GetCategoryAsync(WikiState.GetCurrentPageTitle());
        Content = string.IsNullOrEmpty(Category?.DisplayHtml)
            ? null
            : new MarkupString(Category.DisplayHtml);
    }
}