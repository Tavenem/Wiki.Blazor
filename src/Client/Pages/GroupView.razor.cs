using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The group page.
/// </summary>
public partial class GroupView
{
    private GroupPageInfo? GroupPageInfo { get; set; }

    private MarkupString? Content { get; set; }

    [Inject] private HttpClient HttpClient { get; set; } = default!;

    [Inject] private SnackbarService SnackbarService { get; set; } = default!;

    [Inject] private IWikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (!string.Equals(
            WikiState.WikiNamespace,
            WikiOptions.GroupNamespace,
            StringComparison.OrdinalIgnoreCase))
        {
            GroupPageInfo = null;
            Content = null;
            return;
        }

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        try
        {
            var url = new StringBuilder(serverApi)
                .Append("/group?title=")
                .Append(WikiState.WikiTitle);
            var response = await HttpClient.GetAsync(url.ToString());
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                WikiState.NotAuthorized = true;
            }
            else if (response.IsSuccessStatusCode)
            {
                GroupPageInfo = await response
                    .Content
                    .ReadFromJsonAsync(WikiBlazorJsonSerializerContext.Default.GroupPageInfo);
                Content = string.IsNullOrEmpty(GroupPageInfo?.Item?.Html)
                    ? null
                    : new MarkupString(GroupPageInfo.Item.Html);
            }
            else
            {
                WikiState.LoadError = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            SnackbarService.Add("An error occurred", ThemeColor.Danger);
        }

        if (!string.IsNullOrEmpty(GroupPageInfo?.Group?.Entity?.DisplayName))
        {
            WikiState.UpdateTitle(GroupPageInfo.Group.Entity.DisplayName);
        }
    }
}