using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Web;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client;

/// <summary>
/// <para>
/// A component which renders a Tavenem wiki.
/// </para>
/// <para>
/// Should normally be placed in the <c>NotFound</c> template of your <c>Router</c>, and also as the
/// only content of a routable page whose route is the same as <see
/// cref="WikiOptions.WikiLinkPrefix"/> followed by "/{*route}".
/// </para>
/// </summary>
public partial class Wiki : IDisposable
{
    internal const string DescendingParameter = "pg-d";
    internal const string EndParameter = "h-e";
    internal const string FilterParameter = "pg-f";
    internal const string PageNumberParameter = "pg-p";
    internal const string PageSizeParameter = "pg-ps";
    internal const string SortParameter = "pg-s";
    internal const string SearchDomainParameter = "s-d";
    internal const string SearchNamespaceParameter = "s-n";
    internal const string SearchOwnerParameter = "s-o";
    internal const string StartParameter = "h-s";
    internal const string EditorParameter = "h-ed";

    private const string RevisionParameter = "rev";

    private bool _disposedValue;

    /// <summary>
    /// <para>
    /// Whether to show the compact view of the wiki.
    /// </para>
    /// <para>
    /// This is normally supplied by a query parameter or route value, but you can also set this to
    /// <see langword="true"/> explicitly.
    /// </para>
    /// </summary>
    [Parameter, SupplyParameterFromQuery] public bool Compact { get; set; }

    /// <summary>
    /// Whether any current search is sorted in descending order.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = DescendingParameter)]
    public bool Descending { get; set; }

    /// <summary>
    /// Any requested editor filter.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = EditorParameter)]
    public string? Editor { get; set; }

    /// <summary>
    /// The last requested result in a paged set.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = EndParameter)]
    public string? End { get; set; }

    /// <summary>
    /// Any requested text filter.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = FilterParameter)]
    public string? Filter { get; set; }

    /// <summary>
    /// Whether the requested page should be loaded without following any redirects.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery]
    public bool NoRedirect { get; set; }

    /// <summary>
    /// The requested page number in a paged set.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = PageNumberParameter)]
    public int? PageNumber { get; set; }

    /// <summary>
    /// The requested page size for a paged set.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = PageSizeParameter)]
    public int? PageSize { get; set; }

    /// <summary>
    /// The timestamp of a requested revision.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = RevisionParameter)]
    public string[]? Revisions { get; set; }

    /// <summary>
    /// The domain filter of a search.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = SearchDomainParameter)]
    public string? SearchDomain { get; set; }

    /// <summary>
    /// The namespace filter of a search.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = SearchNamespaceParameter)]
    public string? SearchNamespace { get; set; }

    /// <summary>
    /// The page owner filter of a search.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = SearchOwnerParameter)]
    public string? SearchOwner { get; set; }

    /// <summary>
    /// The sort property of a search.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = SortParameter)]
    public string? Sort { get; set; }

    /// <summary>
    /// The first requested result in a paged set.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery(Name = StartParameter)]
    public string? Start { get; set; }

    /// <summary>
    /// Whether the current user has not yet been authenticated.
    /// </summary>
    /// <remarks>
    /// Expected to be provided by query string, not set explicitly.
    /// </remarks>
    [SupplyParameterFromQuery]
    public bool Unauthenticated { get; set; }

    private string ArticleType => IsCategory ? "Category" : "Article";

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

    private bool IsEditing { get; set; }

    private bool IsFile { get; set; }

    private bool IsGroupPage { get; set; }

    private bool IsSearch { get; set; }

    private bool IsSpecial { get; set; }

    private bool IsSpecialList { get; set; }

    private bool IsUpload { get; set; }

    private bool IsUserPage { get; set; }

    [Inject, NotNull] private NavigationManager? NavigationManager { get; set; }

    private MarkupString Preview { get; set; }

    private bool PreviewDisplayed { get; set; }

    private bool RequestedDiff { get; set; }

    private DateTimeOffset? RequestedFirstTime { get; set; }

    private DateTimeOffset? RequestedSecondTime { get; set; }

    private string? Route { get; set; }

    [Inject, NotNull] private IServiceProvider? ServiceProvider { get; set; }

    private bool ShowHistory { get; set; }

    private bool ShowWhatLinksHere { get; set; }

    private SpecialListType SpecialListType { get; set; }

    private string? TargetDomain { get; set; }

    private string? TargetNamespace { get; set; }

    private string? TargetTitle { get; set; }

    [Inject, NotNull] private WikiBlazorOptions? WikiBlazorClientOptions { get; set; }

    [Inject, NotNull] private ClientWikiDataService? WikiDataService { get; set; }

    private Page? WikiPage { get; set; }

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

        if (PreviewDisplayed)
        {
            Preview = new(await WikiDataService.GetPreviewAsync(title) ?? string.Empty);
            return;
        }

        var item = IsEditing
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
            WikiPage = null;
            IsDiff = false;
            WikiState.UpdateTitle(null);
        }
        else
        {
            WikiPage = item;
            CanCreate = item.Permission.HasFlag(WikiPermission.Create);
            CanEdit = WikiPage?.Exists != true
                ? item.Permission.HasFlag(WikiPermission.Create)
                : item.Permission.HasFlag(WikiPermission.Write);
            CanRename = CanEdit && item.CanRename;
            if (!CanEdit && IsEditing)
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
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        => await RefreshAsync();

    private async void OnStateChanged(object? sender) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        Reset();
        SetIsCompact();
        SetRoute();
        await SetRouteProperties();
        if (!IsSpecialList
            && !IsSearch
            && !IsAllSpecials
            && !IsUpload
            && !ShowWhatLinksHere)
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
        IsEditing = false;
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
        ShowHistory = false;
        SpecialListType = SpecialListType.None;
        WikiPage = null;
        WikiState.IsSystem = false;
        WikiState.LoadError = false;
        WikiState.User = null;
        WikiState.UpdateTitle(null);
    }

    private void SetIsCompact()
    {
        WikiState.SetIsCompact(Compact);
        if (!WikiState.IsCompact)
        {
            var uri = new Uri(NavigationManager.Uri);
            if (WikiBlazorClientOptions.CompactRoutePort.HasValue
                && uri.Port == WikiBlazorClientOptions.CompactRoutePort.Value)
            {
                WikiState.SetIsCompact(true);
            }
            if (!WikiState.IsCompact
                && !string.IsNullOrEmpty(WikiBlazorClientOptions.CompactRouteHostPart))
            {
                var parts = uri.Host.Split('.');
                var position = WikiBlazorClientOptions.CompactRouteHostPosition ?? 0;
                if (parts.Length > position
                    && string.Equals(
                        parts[position],
                        WikiBlazorClientOptions.CompactRouteHostPart,
                        StringComparison.OrdinalIgnoreCase))
                {
                    WikiState.SetIsCompact(true);
                }
            }
        }
        if (WikiState.IsCompact)
        {
            StateHasChanged();
        }
    }

    private void SetRoute()
    {
        var relativeUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        if (!string.IsNullOrEmpty(WikiOptions.WikiLinkPrefix)
            && relativeUri.StartsWith(WikiOptions.WikiLinkPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativeUri = relativeUri[WikiOptions.WikiLinkPrefix.Length..];
        }
        if (relativeUri.StartsWith('/')
            || relativeUri.StartsWith(':'))
        {
            relativeUri = relativeUri[1..];
        }
        var index = relativeUri.IndexOf('?');
        if (index == 0)
        {
            Route = null;
        }
        else if (index > 0)
        {
            Route = relativeUri[..index];
        }
        else
        {
            Route = relativeUri;
        }

        string? actionString = null;
        if (Route is not null)
        {
            index = Route.IndexOf('/');
            if (index != -1)
            {
                actionString = Route[(index + 1)..];
                Route = Route[..index];
            }
        }

        WikiState.IsTalk = false;
        IsEditing = false;
        ShowHistory = false;
        Preview = new(string.Empty);
        PreviewDisplayed = false;
        ShowWhatLinksHere = false;
        switch (actionString)
        {
            case null:
                break;
            case { Length: 0 }:
                break;
            case { } when actionString.Equals("talk", StringComparison.OrdinalIgnoreCase):
                WikiState.IsTalk = true;
                break;
            case { } when actionString.Equals("edit", StringComparison.OrdinalIgnoreCase):
                IsEditing = true;
                break;
            case { } when actionString.Equals("history", StringComparison.OrdinalIgnoreCase):
                ShowHistory = true;
                break;
            case { } when actionString.Equals("preview", StringComparison.OrdinalIgnoreCase):
                PreviewDisplayed = true;
                break;
            case { } when actionString.Equals("whatlinkshere", StringComparison.OrdinalIgnoreCase):
                ShowWhatLinksHere = true;
                break;
        }

        if (Route is not null)
        {
            index = Route.IndexOf('#');
            if (index != -1)
            {
                Route = Route[..index];
            }

            Route = HttpUtility.UrlDecode(Route);
        }
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

        if (ShowWhatLinksHere)
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