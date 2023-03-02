using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Blazor.Client.Shared;

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
public partial class Wiki : OfflineSupportComponent, IAsyncDisposable
{
    private bool _disposedValue;
    private DotNetObjectReference<Wiki>? _dotNetObjectReference;
    private IJSObjectReference? _module;

    /// <summary>
    /// <para>
    /// Whether to show the compact view of the wiki.
    /// </para>
    /// <para>
    /// This is normally supplied by a query parameter, or route value, but you can override these
    /// mechanisms by setting it to <see langword="true"/> here.
    /// </para>
    /// </summary>
    [Parameter] public bool Compact { get; set; }

    internal bool Descending { get; set; }

    internal string? Editor { get; set; }

    internal long? End { get; set; }

    internal string? Filter { get; set; }

    internal int? PageNumber { get; set; }

    internal int? PageSize { get; set; }

    internal string? SearchDomain { get; set; }

    internal string? SearchNamespace { get; set; }

    internal string? SearchOwner { get; set; }

    internal string? Sort { get; set; }

    internal long? Start { get; set; }

    internal bool Unauthenticated { get; set; }

    private string ArticleType => IsCategory ? "Category" : "Article";

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

    private MarkupString? Content { get; set; }

    private string? Diff { get; set; }

    private string? Fragment { get; set; }

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

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private string? LastPreviewLink { get; set; }

    private bool NoRedirect { get; set; }

    private bool PendingPreview { get; set; }

    private MarkupString Preview { get; set; }

    private bool PreviewDisplayed { get; set; }

    private int PreviewX { get; set; }

    private int PreviewY { get; set; }

    private bool RequestedDiff { get; set; }

    private DateTimeOffset? RequestedFirstTime { get; set; }

    private DateTimeOffset? RequestedSecondTime { get; set; }

    private string? Revision { get; set; }

    private string? Route { get; set; }

    [Inject] ISearchClient SearchClient { get; set; } = default!;

    private string? SearchText { get; set; }

    private bool ShowHistory { get; set; }

    private bool ShowWhatLinksHere { get; set; }

    private SpecialListType SpecialListType { get; set; }

    private string? TargetDomain { get; set; }

    private string? TargetNamespace { get; set; }

    private string? TargetTitle { get; set; }

    private Page? WikiPage { get; set; }

    private IWikiUser? User { get; set; }

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
        => RefreshAsync();

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetObjectReference = DotNetObjectReference.Create(this);
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/Tavenem.Wiki.Blazor.Client/Wiki.razor.js");
            await _module.InvokeVoidAsync("initialize", Id, _dotNetObjectReference);
            await RefreshAsync();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Do not change this code. Put cleanup code in 'DisposeAsync(bool disposing)' method
        await DisposeAsync(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (!_disposedValue)
        {
            Dispose(disposing);

            if (disposing)
            {
                _dotNetObjectReference?.Dispose();
                if (_module is not null)
                {
                    await _module.DisposeAsync();
                }
            }

            _disposedValue = true;
        }
    }

    /// <summary>
    /// Invoked by javascript interop.
    /// </summary>
    [JSInvokable]
    public void HidePreview()
    {
        PendingPreview = false;
        PreviewDisplayed = false;
        StateHasChanged();
    }

    /// <summary>
    /// Invoked by javascript interop.
    /// </summary>
    [JSInvokable]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "string will not be trimmed.")]
    public async Task ShowPreview(string link, int clientX, int clientY)
    {
        PendingPreview = true;
        if (string.IsNullOrWhiteSpace(link))
        {
            return;
        }

        if (string.Equals(link, LastPreviewLink)
            && !string.IsNullOrEmpty(Preview.Value))
        {
            PreviewX = clientX;
            PreviewY = clientY;
            PreviewDisplayed = true;
            StateHasChanged();
            return;
        }

        Preview = new(string.Empty);

        var preview = await FetchStringAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/previewlink?link={link}",
            user => WikiDataManager.GetPreviewAsync(user, link));
        Preview = new(preview ?? string.Empty);

        if (!PendingPreview
            || string.IsNullOrEmpty(Preview.Value))
        {
            return;
        }

        LastPreviewLink = link;
        PreviewX = clientX;
        PreviewY = clientY;
        PreviewDisplayed = true;
        PendingPreview = false;
        StateHasChanged();
    }

    /// <inheritdoc/>
    protected override async Task RefreshAsync()
    {
        if (_module is null)
        {
            return;
        }
        await JSRuntime.InvokeVoidAsync("wikiblazor.hidePreview");
        Reset();
        SetIsCompact();
        SetRoute();
        SetRouteProperties();
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

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "List<string> will not be trimmed.")]
    private async Task<IEnumerable<KeyValuePair<string, object>>> GetSearchSuggestions(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Enumerable.Empty<KeyValuePair<string, object>>();
        }

        var suggestions = await FetchDataAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/searchsuggest?input={input}",
            async user => await WikiDataManager.GetSearchSuggestionsAsync(
                SearchClient,
                user,
                input));
        return suggestions?.Select(x => new KeyValuePair<string, object>(x, x))
            ?? Enumerable.Empty<KeyValuePair<string, object>>();
    }

    private async Task GetWikiItemAsync()
    {
        var url = new StringBuilder(WikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/item?title=")
            .Append(WikiState.WikiTitle);
        if (!string.IsNullOrEmpty(WikiState.WikiNamespace))
        {
            url.Append("&namespace=")
                .Append(WikiState.WikiNamespace);
        }
        if (!string.IsNullOrEmpty(WikiState.WikiDomain))
        {
            url.Append("&domain=")
                .Append(WikiState.WikiDomain);
        }
        if (NoRedirect)
        {
            url.Append("&noRedirect=true");
        }
        if (RequestedDiff)
        {
            url.Append("&diff=true");
        }
        if (RequestedFirstTime.HasValue)
        {
            url.Append("&firstTime=")
                .Append(RequestedFirstTime.Value.ToUniversalTime().Ticks);
        }
        if (RequestedSecondTime.HasValue)
        {
            url.Append("&secondTime=")
                .Append(RequestedSecondTime.Value.ToUniversalTime().Ticks);
        }

        var item = await FetchDataAsync(
            url.ToString(),
            WikiJsonSerializerContext.Default.WikiPageInfo,
            async user => await WikiDataManager.GetItemAsync(
                user,
                new PageTitle(WikiState.WikiTitle, WikiState.WikiNamespace, WikiState.WikiDomain),
                NoRedirect,
                RequestedFirstTime,
                RequestedSecondTime,
                RequestedDiff));
        if (item is null)
        {
            CanCreate = false;
            CanEdit = false;
            Content = null;
            WikiPage = null;
            IsDiff = false;
            WikiState.UpdateTitle(null);
        }
        else
        {
            WikiPage = item.Page;
            CanCreate = item.Permission.HasFlag(WikiPermission.Create);
            CanEdit = WikiPage?.Exists != true
                ? item.Permission.HasFlag(WikiPermission.Create)
                : item.Permission.HasFlag(WikiPermission.Write);
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
                            .Append(WikiBlazorClientOptions.LoginPath.Contains('?')
                                ? '&' : '?')
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
                            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Unauthenticated), true));
                        }
                        else
                        {
                            NavigationManager.NavigateTo(uri.ToString());
                        }
                    }
                }
            }
            Content = string.IsNullOrEmpty(item.Html)
                ? null
                : new MarkupString(item.Html);
            IsDiff = item.IsDiff;
            WikiState.UpdateTitle(item.DisplayTitle);
            StateHasChanged();
            if (_module is not null && !string.IsNullOrEmpty(Fragment))
            {
                await _module.InvokeVoidAsync("scrollIntoView", Fragment);
            }
        }
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        => await RefreshAsync();

    private void OnSetSearchText()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return;
        }

        NavigationManager.NavigateTo(WikiState.Link(
            "Search",
            WikiOptions.SystemNamespace,
            query: $"filter={SearchText}"));
    }

    private void Reset()
    {
        CanCreate = false;
        CanEdit = false;
        Content = null;
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
        PendingPreview = false;
        RequestedDiff = false;
        RequestedFirstTime = null;
        RequestedSecondTime = null;
        SearchText = null;
        ShowHistory = false;
        SpecialListType = SpecialListType.None;
        User = null;
        WikiPage = null;
        WikiState.IsSystem = false;
        WikiState.LoadError = false;
        WikiState.UpdateTitle(null);
    }

    private void SetIsCompact()
    {
        WikiState.SetIsCompact(Compact || NavigationManager.GetQueryParam<bool>("compact"));
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
        Fragment = null;

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

        if (!string.IsNullOrEmpty(actionString))
        {
            WikiState.IsTalk = string.Equals(actionString, "talk", StringComparison.OrdinalIgnoreCase);
            if (!WikiState.IsTalk)
            {
                IsEditing = string.Equals(actionString, "edit", StringComparison.OrdinalIgnoreCase);
                if (!IsEditing)
                {
                    ShowHistory = string.Equals(actionString, "history", StringComparison.OrdinalIgnoreCase);
                    if (!ShowHistory)
                    {
                        ShowWhatLinksHere = string.Equals(actionString, "whatlinkshere", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }

        if (Route is not null)
        {
            index = Route.IndexOf('#');
            if (index != -1)
            {
                Fragment = Route[(index + 1)..];
                Route = Route[..index];
            }
        }

        Descending = NavigationManager.GetQueryParam<bool>("descending");
        Diff = NavigationManager.GetQueryParam<string>("diff");
        Editor = NavigationManager.GetQueryParam<string>("editor");
        End = NavigationManager.GetQueryParam<long?>("end");
        Filter = NavigationManager.GetQueryParam<string>("filter");
        NoRedirect = NavigationManager.GetQueryParam<bool>("noRedirect");
        PageNumber = NavigationManager.GetQueryParam<int?>("pageNumber");
        PageSize = NavigationManager.GetQueryParam<int?>("pageSize");
        Revision = NavigationManager.GetQueryParam<string>("rev");
        SearchNamespace = NavigationManager.GetQueryParam<string>("searchNamespace");
        SearchOwner = NavigationManager.GetQueryParam<string>("searchOwner");
        Sort = NavigationManager.GetQueryParam<string>("sort");
        Start = NavigationManager.GetQueryParam<long?>("start");
        Unauthenticated = NavigationManager.GetQueryParam<bool>("unauthenticated");
    }

    private async void SetRouteProperties()
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

        User = await FetchDataAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/currentuser",
            WikiJsonSerializerContext.Default.WikiUser,
            WikiDataManager.GetWikiUserAsync);
        if (WikiState.IsSystem && User is not null)
        {
            CanEdit = User.IsWikiAdmin;
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

        if (!string.IsNullOrEmpty(Revision))
        {
            if (DateTimeOffset.TryParse(Revision, out var timestamp))
            {
                RequestedFirstTime = timestamp;
            }
            else if (long.TryParse(Revision, out var ticks))
            {
                RequestedFirstTime = new DateTimeOffset(ticks, TimeSpan.Zero);
            }
        }

        if (!string.IsNullOrEmpty(Diff))
        {
            if (string.Equals(Diff, "prev", StringComparison.OrdinalIgnoreCase))
            {
                if (RequestedFirstTime is not null)
                {
                    RequestedSecondTime = RequestedFirstTime;
                    RequestedFirstTime = null;
                }
            }
            else if (string.Equals(Diff, "cur", StringComparison.OrdinalIgnoreCase))
            {
                RequestedDiff = true;
            }
            else if (DateTimeOffset.TryParse(Diff, out var diffTimestamp))
            {
                RequestedSecondTime = diffTimestamp;
            }
            else if (long.TryParse(Diff, out var diffTicks))
            {
                RequestedSecondTime = new DateTimeOffset(diffTicks, TimeSpan.Zero);
            }
        }
    }
}