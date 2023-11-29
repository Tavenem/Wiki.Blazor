using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components;
using Tavenem.Blazor.Framework;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// A control to select one or more wiki users and/or groups.
/// </summary>
public partial class UserSelector : OfflineSupportComponent
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
    /// CSS class(es) to apply to this component.
    /// </summary>
    [Parameter] public string? Class { get; set; }

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
    [Parameter] public List<IWikiOwner> DeselectedUsers { get; set; } = [];

    /// <summary>
    /// The list of deselected users.
    /// </summary>
    [Parameter] public EventCallback<List<IWikiOwner>> DeselectedUsersChanged { get; set; }

    /// <summary>
    /// Help text to display under the control.
    /// </summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>
    /// A semicolon-delimited collection of user queries to preselect.
    /// </summary>
    [Parameter] public string? InitialUsers { get; set; }

    /// <summary>
    /// A label for the control.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The list of selected users.
    /// </summary>
    [Parameter] public List<IWikiOwner> SelectedUsers { get; set; } = [];

    /// <summary>
    /// The list of selected users.
    /// </summary>
    [Parameter] public EventCallback<List<IWikiOwner>> SelectedUsersChanged { get; set; }

    /// <summary>
    /// CSS style(s) to apply to this component.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// Whether more than one user may be selected.
    /// </summary>
    [Parameter] public bool Multiple { get; set; }

    private string? CssClass => new CssBuilder(Class)
        .Add("d-flex flex-column align-self-stretch gap-1")
        .ToString();

    private bool InputDisabled => Disabled || (!Multiple && SelectedUsers.Count > 0);

    private string? InputText { get; set; }

    private bool IsInteractive { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(InitialUsers))
        {
            return;
        }
        foreach (var query in InitialUsers.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            await OnAddUserAsync(query);
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            IsInteractive = true;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Adds a user matching the given search <paramref name="query"/>.
    /// </summary>
    /// <param name="query">A wiki user ID or username.</param>
    /// <returns>
    /// The added user, if any.
    /// </returns>
    /// <remarks>
    /// Has no effect if no user matches the <paramref name="query"/>.
    /// </remarks>
    public async Task<IWikiOwner?> OnAddUserAsync(string query)
    {
        var wikiOwner = await FetchDataAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/wikiowner?query={query}",
            WikiJsonSerializerContext.Default.IWikiOwner,
            async user =>
            {
                IWikiOwner? wikiOwner = await WikiDataManager.GetWikiUserAsync(user, query);
                wikiOwner ??= await WikiDataManager.GetWikiGroupAsync(user, query);
                return wikiOwner;
            });
        if (wikiOwner is not null)
        {
            await OnAddUserAsync(wikiOwner);
        }
        return wikiOwner;
    }

    private static string GetItemStyle(IWikiOwner user)
        => $"background-color:hsl({20 + (user.Id.GetHashCode() % 320)}, --tavenem-color-default-saturation, --tavenem-color-default-lightness)";

    private async Task OnAddUserAsync(IWikiOwner user)
    {
        if (InputDisabled)
        {
            return;
        }

        if (!AllowGroups && user is IWikiGroup)
        {
            SnackbarService.Add("Cannot select a group here", ThemeColor.Warning);
            return;
        }

        InputText = null;
        SelectedUsers.Add(user);
        await SelectedUsersChanged.InvokeAsync(SelectedUsers);
        StateHasChanged();
    }

    private async Task OnItemClickAsync(IWikiOwner user)
    {
        if (!AllowToggle)
        {
            return;
        }

        if (SelectedUsers.Remove(user))
        {
            DeselectedUsers.Add(user);
        }
        else if (DeselectedUsers.Remove(user))
        {
            SelectedUsers.Add(user);
        }
        else
        {
            return;
        }
        await SelectedUsersChanged.InvokeAsync(SelectedUsers);
        await DeselectedUsersChanged.InvokeAsync(DeselectedUsers);
    }

    private async Task OnRemoveUserAsync(IWikiOwner user)
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

        await OnAddUserAsync(InputText.Trim());
    }
}