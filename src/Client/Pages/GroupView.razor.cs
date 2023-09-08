using Microsoft.AspNetCore.Components;
using System.Text;
using Tavenem.Wiki.Blazor.Client.Shared;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The group page.
/// </summary>
public partial class GroupView : OfflineSupportComponent
{
    private GroupPage? GroupPage { get; set; }

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
            WikiOptions.GroupNamespace,
            StringComparison.OrdinalIgnoreCase))
        {
            GroupPage = null;
            Content = null;
            return;
        }

        GroupPage = await FetchDataAsync(
            new StringBuilder(WikiBlazorClientOptions.WikiServerApiRoute)
                .Append("/group?title=")
                .Append(WikiState.WikiTitle)
                .ToString(),
            WikiJsonSerializerContext.Default.GroupPage,
            async user => await WikiDataManager.GetGroupPageAsync(user, WikiState.WikiTitle));
        Content = string.IsNullOrEmpty(GroupPage?.DisplayHtml)
            ? null
            : new MarkupString(GroupPage.DisplayHtml);
        if (!string.IsNullOrEmpty(GroupPage?.DisplayTitle))
        {
            WikiState.UpdateTitle(GroupPage.DisplayTitle);
        }
    }
}