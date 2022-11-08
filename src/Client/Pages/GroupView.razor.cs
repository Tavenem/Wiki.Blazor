using Microsoft.AspNetCore.Components;
using System.Text;
using Tavenem.Wiki.Blazor.Client.Shared;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The group page.
/// </summary>
public partial class GroupView : OfflineSupportComponent
{
    private GroupPageInfo? GroupPageInfo { get; set; }

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
            GroupPageInfo = null;
            Content = null;
            return;
        }

        GroupPageInfo = await FetchDataAsync(
            new StringBuilder(WikiBlazorClientOptions.WikiServerApiRoute)
                .Append("/group?title=")
                .Append(WikiState.WikiTitle)
                .ToString(),
            WikiBlazorJsonSerializerContext.Default.GroupPageInfo,
            user => WikiDataManager.GetGroupPageAsync(user, WikiState.WikiTitle));
        Content = string.IsNullOrEmpty(GroupPageInfo?.Item?.Html)
            ? null
            : new MarkupString(GroupPageInfo.Item.Html);
        if (!string.IsNullOrEmpty(GroupPageInfo?.Group?.Entity?.DisplayName))
        {
            WikiState.UpdateTitle(GroupPageInfo.Group.Entity.DisplayName);
        }
    }
}