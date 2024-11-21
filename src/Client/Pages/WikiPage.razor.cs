using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// A wiki page.
/// </summary>
public partial class WikiPage : IDisposable
{
    private bool _disposedValue;

    private string ArticleType => IsCategory ? "Category" : "Article";

    /// <summary>
    /// Whether any current search is sorted in descending order.
    /// </summary>
    [Parameter] public bool Descending { get; set; }

    /// <summary>
    /// Any requested editor filter.
    /// </summary>
    [Parameter] public string? Editor { get; set; }

    /// <summary>
    /// The last requested result in a paged set.
    /// </summary>
    [Parameter] public string? End { get; set; }

    /// <summary>
    /// Any requested text filter.
    /// </summary>
    [Parameter] public string? Filter { get; set; }

    /// <summary>
    /// Whether the requested page should be loaded without following any redirects.
    /// </summary>
    [Parameter] public bool NoRedirect { get; set; }

    /// <summary>
    /// The requested page number in a paged set.
    /// </summary>
    [Parameter] public int? PageNumber { get; set; }

    /// <summary>
    /// The requested page size for a paged set.
    /// </summary>
    [Parameter] public int? PageSize { get; set; }

    /// <summary>
    /// The timestamp of a requested revision.
    /// </summary>
    [Parameter] public string[]? Revisions { get; set; }

    /// <summary>
    /// The requested route.
    /// </summary>
    [Parameter] public string? Route { get; set; }

    /// <summary>
    /// The domain filter of a search.
    /// </summary>
    [Parameter] public string? SearchDomain { get; set; }

    /// <summary>
    /// The namespace filter of a search.
    /// </summary>
    [Parameter] public string? SearchNamespace { get; set; }

    /// <summary>
    /// The page owner filter of a search.
    /// </summary>
    [Parameter] public string? SearchOwner { get; set; }

    /// <summary>
    /// The sort property of a search.
    /// </summary>
    [Parameter] public string? Sort { get; set; }

    /// <summary>
    /// The first requested result in a paged set.
    /// </summary>
    [Parameter] public string? Start { get; set; }

    /// <summary>
    /// Whether the current user has not yet been authenticated.
    /// </summary>
    [Parameter] public bool Unauthenticated { get; set; }

    private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    private bool _canCreate;
    private bool CanCreate
    {
        get => !WikiState.NotAuthorized && _canEdit && _canCreate;
        set => _canCreate = value;
    }

    private bool _canEdit;
    private bool CanEdit
    {
        get => !WikiState.NotAuthorized && _canEdit;
        set => _canEdit = value;
    }

    private bool _canRename;
    private bool CanRename
    {
        get => !WikiState.NotAuthorized && _canRename;
        set => _canRename = value;
    }

    private MarkupString Content { get; set; }

    private string Id { get; } = Guid.NewGuid().ToHtmlId();

    private bool IsAllSpecials { get; set; }

    private bool IsCategory { get; set; }

    private bool IsDiff { get; set; }

    private bool IsRevisionRequested => RequestedDiff
        || RequestedFirstTime.HasValue
        || RequestedSecondTime.HasValue;

    private bool IsFile { get; set; }

    private bool IsGroupPage { get; set; }

    private bool IsSearch { get; set; }

    private bool IsSpecial { get; set; }

    private bool IsSpecialList { get; set; }

    private bool IsUpload { get; set; }

    private bool IsUserPage { get; set; }

    [Inject, NotNull] private NavigationManager? NavigationManager { get; set; }

    private bool RequestedDiff { get; set; }

    private DateTimeOffset? RequestedFirstTime { get; set; }

    private DateTimeOffset? RequestedSecondTime { get; set; }

    [Inject, NotNull] private IServiceProvider? ServiceProvider { get; set; }

    private SpecialListType SpecialListType { get; set; }

    private string? TargetDomain { get; set; }

    private string? TargetNamespace { get; set; }

    private string? TargetTitle { get; set; }

    [Inject, NotNull] private WikiBlazorOptions? WikiBlazorClientOptions { get; set; }

    [Inject, NotNull] private ClientWikiDataService? WikiDataService { get; set; }

    private Page? Page { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
        => RefreshAsync();

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        AuthenticationStateProvider = ServiceProvider.GetService<AuthenticationStateProvider>();
        if (AuthenticationStateProvider is not null)
        {
            AuthenticationStateProvider.AuthenticationStateChanged += OnStateChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            NavigationManager.LocationChanged += OnLocationChanged;
            StateHasChanged();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing && AuthenticationStateProvider is not null)
            {
                AuthenticationStateProvider.AuthenticationStateChanged -= OnStateChanged;
            }

            _disposedValue = true;
        }
    }

    private async Task GetWikiItemAsync()
    {
        var title = WikiState.GetCurrentPageTitle();

        var item = WikiState.IsEditing
            ? await WikiDataService.GetEditInfoAsync(title)
            : await WikiDataService.GetItemAsync(
                title,
                NoRedirect,
                RequestedFirstTime,
                RequestedSecondTime,
                RequestedDiff);
        if (item is null)
        {
            CanCreate = false;
            CanEdit = false;
            Content = default;
            Page = null;
            IsDiff = false;
            WikiState.UpdateTitle(null);
            return;
        }

        Page = item;
        CanCreate = item.Permission.HasFlag(WikiPermission.Create);
        CanEdit = Page?.Exists != true
            ? item.Permission.HasFlag(WikiPermission.Create)
            : item.Permission.HasFlag(WikiPermission.Write);
        CanRename = CanEdit && item.CanRename;
        if (!CanEdit && WikiState.IsEditing)
        {
            WikiState.NotAuthorized = true;

            var state = AuthenticationStateProvider is null
                ? null
                : await AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (state?.User.Identity?.IsAuthenticated != true)
            {
                if (string.IsNullOrEmpty(WikiBlazorClientOptions.LoginPath))
                {
                    NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Unauthenticated), true));
                }
                else
                {
                    var path = new StringBuilder(WikiBlazorClientOptions.LoginPath)
                        .Append(WikiBlazorClientOptions.LoginPath.Contains('?') ? '&' : '?')
                        .Append("returnUrl=")
                        .Append(Uri.EscapeDataString(NavigationManager.Uri));
                    Uri? uri = null;
                    try
                    {
                        uri = new Uri(path.ToString());
                    }
                    catch { }
                    if (uri?.IsAbsoluteUri != false)
                    {
                        NavigationManager.NavigateTo(NavigationManager
                            .GetUriWithQueryParameter(nameof(Unauthenticated), true));
                    }
                    else
                    {
                        NavigationManager.NavigateTo(uri.ToString());
                    }
                }
            }
        }
        Content = new MarkupString(item.DisplayHtml ?? string.Empty);
        IsDiff = item.IsDiff;
        WikiState.UpdateTitle(item.DisplayTitle);
        StateHasChanged();
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        => await RefreshAsync();

    private async void OnStateChanged(object? sender) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        Reset();
        await SetRouteProperties();
        if (!IsSpecialList
            && !IsSearch
            && !IsAllSpecials
            && !IsUpload
            && !WikiState.ShowWhatLinksHere)
        {
            await GetWikiItemAsync();
        }
        StateHasChanged();
    }

    private void Reset()
    {
        CanCreate = false;
        CanEdit = false;
        Content = default;
        IsAllSpecials = false;
        IsCategory = false;
        IsFile = false;
        IsGroupPage = false;
        IsSearch = false;
        IsSpecial = false;
        IsSpecialList = false;
        IsUserPage = false;
        NoRedirect = false;
        RequestedDiff = false;
        RequestedFirstTime = null;
        RequestedSecondTime = null;
        SpecialListType = SpecialListType.None;
        Page = null;
    }

    private async Task SetRouteProperties()
    {
        (
            WikiState.WikiTitle,
            WikiState.WikiNamespace,
            WikiState.WikiDomain
        ) = PageTitle.Parse(Route);
        WikiState.DefaultNamespace = string.IsNullOrEmpty(WikiState.WikiNamespace);

        IsCategory = !WikiState.DefaultNamespace
            && string.Equals(
                WikiState.WikiNamespace,
                WikiOptions.CategoryNamespace,
                StringComparison.OrdinalIgnoreCase);
        WikiState.IsSystem = !WikiState.DefaultNamespace
            && !IsCategory
            && string.Equals(
                WikiState.WikiNamespace,
                WikiOptions.SystemNamespace,
                StringComparison.OrdinalIgnoreCase);

        if (Unauthenticated)
        {
            WikiState.NotAuthorized = true;
            return;
        }

        if (WikiState.ShowWhatLinksHere)
        {
            IsSpecialList = true;
            SpecialListType = SpecialListType.What_Links_Here;
            TargetDomain = WikiState.WikiDomain;
            TargetNamespace = WikiState.WikiNamespace;
            TargetTitle = WikiState.WikiTitle;
            WikiState.UpdateTitle($"{WikiState.WikiTitle} {SpecialListType.What_Links_Here.ToHumanReadable()}");
            return;
        }

        if (WikiState.IsSystem)
        {
            IsSearch = string.Equals(WikiState.WikiTitle, "Search", StringComparison.OrdinalIgnoreCase);
            if (IsSearch)
            {
                IsSpecial = true;
                return;
            }

            IsAllSpecials = string.Equals(WikiState.WikiTitle, "Special", StringComparison.OrdinalIgnoreCase);
            if (IsAllSpecials)
            {
                IsSpecial = true;
                return;
            }

            IsUpload = string.Equals(WikiState.WikiTitle, "Upload", StringComparison.OrdinalIgnoreCase);
            if (IsUpload)
            {
                IsSpecial = true;
                return;
            }

            if (Enum.TryParse<SpecialListType>(WikiState.WikiTitle, ignoreCase: true, out var type))
            {
                IsSpecial = true;
                IsSpecialList = true;
                SpecialListType = type;
                WikiState.UpdateTitle(WikiState.WikiTitle.Replace('_', ' '));
                return;
            }
        }

        WikiState.User = await WikiDataService.GetWikiUserAsync();
        if (WikiState.IsSystem && WikiState.User is not null)
        {
            CanEdit = WikiState.User.IsWikiAdmin;
        }

        IsFile = !WikiState.DefaultNamespace
            && !IsCategory
            && !WikiState.IsSystem
            && string.Equals(
                WikiState.WikiNamespace,
                WikiOptions.FileNamespace,
                StringComparison.OrdinalIgnoreCase);
        IsUserPage = !WikiState.DefaultNamespace
            && !IsCategory
            && !WikiState.IsSystem
            && !IsFile
            && string.Equals(
                WikiState.WikiNamespace,
                WikiOptions.UserNamespace,
                StringComparison.OrdinalIgnoreCase);
        IsGroupPage = !WikiState.DefaultNamespace
            && !IsCategory
            && !WikiState.IsSystem
            && !IsFile
            && !IsUserPage
            && string.Equals(
                WikiState.WikiNamespace,
                WikiOptions.GroupNamespace,
                StringComparison.OrdinalIgnoreCase);

        if (Revisions?.Length > 0)
        {
            var prev = false;
            List<DateTimeOffset>? requestedTimes = null;
            foreach (var revision in Revisions)
            {
                if (string.Equals(revision, "prev", StringComparison.OrdinalIgnoreCase))
                {
                    prev = true;
                    RequestedDiff = true;
                }
                else if (string.Equals(revision, "cur", StringComparison.OrdinalIgnoreCase))
                {
                    RequestedDiff = true;
                }
                if (DateTimeOffset.TryParse(revision, out var timestamp))
                {
                    (requestedTimes ??= []).Add(timestamp);
                }
                else if (long.TryParse(revision, out var ticks))
                {
                    (requestedTimes ??= []).Add(new DateTimeOffset(ticks, TimeSpan.Zero));
                }
            }

            requestedTimes?.Sort();
            RequestedFirstTime = requestedTimes?.FirstOrDefault();
            if (RequestedFirstTime is not null)
            {
                RequestedSecondTime = requestedTimes?.Skip(1).FirstOrDefault();
                if (RequestedSecondTime is not null)
                {
                    RequestedDiff = true;
                }
                else if (prev)
                {
                    RequestedSecondTime = RequestedFirstTime;
                    RequestedFirstTime = null;
                }
            }
        }
    }
}