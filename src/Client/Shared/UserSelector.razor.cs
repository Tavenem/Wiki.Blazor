using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// A control to select one or more wiki users and/or groups.
/// </summary>
public partial class UserSelector
{
    /// <summary>
    /// <para>
    /// Whether users may be toggled to an "off" state without removing them.
    /// </para>
    /// <para>
    /// Users in the "off" state are kept in the <see cref="DeselectedUsers"/> list.
    /// </para>
    /// </summary>
    [Parameter] public bool AllowToggle { get; set; }

    /// <summary>
    /// Whether groups may be selected.
    /// </summary>
    [Parameter] public bool AllowGroups { get; set; }

    /// <summary>
    /// Whether this control is disabled.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Whether the help text should be displayed only when the text input has focus.
    /// </summary>
    [Parameter] public bool DisplayHelpTextOnFocus { get; set; }

    /// <summary>
    /// The list of deselected users.
    /// </summary>
    [Parameter] public List<WikiUserInfo> DeselectedUsers { get; set; } = new();

    /// <summary>
    /// The list of deselected users.
    /// </summary>
    [Parameter] public EventCallback<List<WikiUserInfo>> DeselectedUsersChanged { get; set; }

    /// <summary>
    /// Help text to display under the control.
    /// </summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>
    /// A label for the control.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The list of selected users.
    /// </summary>
    [Parameter] public List<WikiUserInfo> SelectedUsers { get; set; } = new();

    /// <summary>
    /// The list of selected users.
    /// </summary>
    [Parameter] public EventCallback<List<WikiUserInfo>> SelectedUsersChanged { get; set; }

    /// <summary>
    /// Whether more than one user may be selected.
    /// </summary>
    [Parameter] public bool Multiple { get; set; }

    [Inject] private HttpClient HttpClient { get; set; } = default!;

    private bool InputDisabled => Disabled || (!Multiple && SelectedUsers.Count > 0);

    private string? InputText { get; set; }

    [Inject] private SnackbarService SnackbarService { get; set; } = default!;

    [Inject] private IWikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    private static string GetItemStyle(WikiUserInfo user)
        => $"background-color:hsl({20 + (user.Id.GetHashCode() % 320)}, --tavenem-color-default-saturation, --tavenem-color-default-lightness)";

    private async Task OnAddUserAsync(WikiUserInfo? user)
    {
        if (InputDisabled)
        {
            return;
        }

        if (user?.Entity is null)
        {
            SnackbarService.Add("No such user", ThemeColor.Warning);
            return;
        }

        if (!AllowGroups && user.Entity is IWikiGroup)
        {
            SnackbarService.Add("Cannot select a group here", ThemeColor.Warning);
            return;
        }

        InputText = null;
        SelectedUsers.Add(user);
        await SelectedUsersChanged.InvokeAsync(SelectedUsers);
        StateHasChanged();
    }

    private async Task OnItemClickAsync(WikiUserInfo user)
    {
        if (!AllowToggle)
        {
            return;
        }

        if (SelectedUsers.Contains(user))
        {
            SelectedUsers.Remove(user);
            DeselectedUsers.Add(user);
        }
        else if (DeselectedUsers.Contains(user))
        {
            DeselectedUsers.Remove(user);
            SelectedUsers.Add(user);
        }
        else
        {
            return;
        }
        await SelectedUsersChanged.InvokeAsync(SelectedUsers);
        await DeselectedUsersChanged.InvokeAsync(DeselectedUsers);
    }

    private async Task OnRemoveUserAsync(WikiUserInfo user)
    {
        if (SelectedUsers.Remove(user))
        {
            await SelectedUsersChanged.InvokeAsync(SelectedUsers);
        }
    }

    private async Task OnTextEnteredAsync()
    {
        if (InputDisabled
            || string.IsNullOrWhiteSpace(InputText))
        {
            return;
        }

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        try
        {
            var response = await HttpClient.GetAsync($"{serverApi}/wikiuser?query={InputText.Trim()}");
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync(WikiBlazorJsonSerializerContext.Default.WikiUserInfo);
                await OnAddUserAsync(user);
            }
            else
            {
                SnackbarService.Add("There was a problem fetching user info", ThemeColor.Danger);
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}