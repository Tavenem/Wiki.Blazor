using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The group page.
/// </summary>
public partial class GroupView
{
    private GroupPage? GroupPage { get; set; }

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
            GroupPage = null;
            Content = default;
            return;
        }

        GroupPage = await WikiDataService.GetGroupPageAsync(WikiState.WikiTitle);
        Content = string.IsNullOrEmpty(GroupPage?.DisplayHtml)
            ? default
            : new MarkupString(GroupPage.DisplayHtml);
        if (!string.IsNullOrEmpty(GroupPage?.DisplayTitle))
        {
            WikiState.UpdateTitle(GroupPage.DisplayTitle);
        }
    }
}