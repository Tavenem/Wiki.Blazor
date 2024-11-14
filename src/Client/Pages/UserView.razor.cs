using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The user page.
/// </summary>
public partial class UserView
{
    private UserPage? UserPage { get; set; }

    private MarkupString Content { get; set; }

    [Inject, NotNull] private ClientWikiDataService? WikiDataService { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(WikiState.WikiTitle)
            || !string.Equals(
            WikiState.WikiNamespace,
            WikiOptions.GroupNamespace,
            StringComparison.OrdinalIgnoreCase))
        {
            UserPage = null;
            Content = default;
            return;
        }

        UserPage = await WikiDataService.GetUserPageAsync(WikiState.WikiTitle);
        Content = string.IsNullOrEmpty(UserPage?.DisplayHtml)
            ? default
            : new MarkupString(UserPage.DisplayHtml);
        if (!string.IsNullOrEmpty(UserPage?.DisplayTitle))
        {
            WikiState.UpdateTitle(UserPage.DisplayTitle);
        }
    }
}