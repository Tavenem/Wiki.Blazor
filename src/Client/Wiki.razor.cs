using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using Tavenem.Blazor.Framework;

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
public partial class Wiki : IAsyncDisposable
{
    private bool _disposedValue;
    private DotNetObjectReference<Wiki>? _dotNetObjectReference;
    private bool _initialized;
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

    internal string? SearchNamespace { get; set; }

    internal string? SearchOwner { get; set; }

    internal string? Sort { get; set; }

    internal long? Start { get; set; }

    internal bool Unauthenticated { get; set; }

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

    private MarkupString? Content { get; set; }

    private string? Diff { get; set; }

    [Inject] private HttpClient HttpClient { get; set; } = default!;

    private string Id { get; } = Guid.NewGuid().ToHtmlId();

    private bool IsAllSpecials { get; set; }

    private bool IsCategory { get; set; }

    private bool IsDiff { get; set; }

    private bool IsRevisionRequested => RequestedDiffCurrent
        || RequestedDiffPrevious
        || RequestedDiffTimestamp.HasValue
        || RequestedTimestamp.HasValue;

    private bool IsEditing { get; set; }

    private bool IsFile { get; set; }

    private bool IsGroupPage { get; set; }

    private bool IsSearch { get; set; }

    private bool IsSpecialList { get; set; }

    private bool IsUpload { get; set; }

    private bool IsUserPage { get; set; }

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private string? LastPreviewLink { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool NoRedirect { get; set; }

    private bool PendingPreview { get; set; }

    private MarkupString Preview { get; set; }

    private bool PreviewDisplayed { get; set; }

    private int PreviewX { get; set; }

    private int PreviewY { get; set; }

    private bool RequestedDiffCurrent { get; set; }

    private bool RequestedDiffPrevious { get; set; }

    private DateTimeOffset? RequestedDiffTimestamp { get; set; }

    private DateTimeOffset? RequestedTimestamp { get; set; }

    private string? Revision { get; set; }

    private string? Route { get; set; }

    private string? SearchText { get; set; }

    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;

    private bool ShowHistory { get; set; }

    private bool ShowWhatLinksHere { get; set; }

    [Inject] private SnackbarService SnackbarService { get; set; } = default!;

    private SpecialListType SpecialListType { get; set; }

    private string? TargetNamespace { get; set; }

    private string? TargetTitle { get; set; }

    [Inject] private IWikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    private Article? WikiItem { get; set; }

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    private IWikiUser? User { get; set; }

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync() => ReloadAsync();

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        Navigation.LocationChanged += OnLocationChanged;
        AuthenticationStateProvider = ServiceProvider.GetService<AuthenticationStateProvider>();
        if (AuthenticationStateProvider is not null)
        {
            AuthenticationStateProvider.AuthenticationStateChanged += OnStateChanged;
        }
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
            _initialized = true;
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
            if (disposing)
            {
                _dotNetObjectReference?.Dispose();
                if (_module is not null)
                {
                    await _module.DisposeAsync();
                }
                if (AuthenticationStateProvider is not null)
                {
                    AuthenticationStateProvider.AuthenticationStateChanged -= OnStateChanged;
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

        var (_, _, isTalk, _) = Article.GetTitleParts(WikiOptions, link);
        if (isTalk)
        {
            return;
        }

        Preview = new(string.Empty);

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        try
        {
            var preview = await HttpClient.GetStringAsync($"{serverApi}/previewlink?link={link}");
            Preview = new(preview);
        }
        catch { }

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
        var (_, title, _, _) = Article.GetTitleParts(WikiOptions, input);
        if (string.IsNullOrEmpty(title))
        {
            return Enumerable.Empty<KeyValuePair<string, object>>();
        }

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        List<string>? response = null;
        try
        {
            response = await HttpClient.GetFromJsonAsync<List<string>>($"{serverApi}/searchsuggest?input={input}");
        }
        catch { }
        return response?.Select(x => new KeyValuePair<string, object>(x, x))
            ?? Enumerable.Empty<KeyValuePair<string, object>>();
    }

    private async Task GetWikiItemAsync()
    {
        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        try
        {
            var url = new StringBuilder(serverApi)
                .Append("/item?title=")
                .Append(WikiState.WikiTitle);
            if (!string.IsNullOrEmpty(WikiState.WikiNamespace))
            {
                url.Append("&wikiNamespace=")
                    .Append(WikiState.WikiNamespace);
            }
            if (NoRedirect)
            {
                url.Append("&noRedirect=true");
            }
            if (RequestedDiffCurrent)
            {
                url.Append("&requestedDiffCurrent=true");
            }
            if (RequestedDiffPrevious)
            {
                url.Append("&requestedDiffPrevious");
            }
            if (RequestedDiffTimestamp.HasValue)
            {
                url.Append("&requestedDiffTimestamp=")
                    .Append(RequestedDiffTimestamp.Value.ToUniversalTime().Ticks);
            }
            if (RequestedTimestamp.HasValue)
            {
                url.Append("&requestedTimestamp=")
                    .Append(RequestedTimestamp.Value.ToUniversalTime().Ticks);
            }
            var response = await HttpClient.GetAsync(url.ToString());
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                WikiState.NotAuthorized = true;
            }
            else if (response.IsSuccessStatusCode)
            {
                var item = await response.Content.ReadFromJsonAsync(WikiBlazorJsonSerializerContext.Default.WikiItemInfo);
                WikiItem = item?.Item;
                var permission = item?.Permission ?? WikiPermission.None;
                CanCreate = permission.HasFlag(WikiPermission.Create);
                CanEdit = WikiItem is null
                    ? permission.HasFlag(WikiPermission.Create)
                    : permission.HasFlag(WikiPermission.Write);
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
                            Navigation.NavigateTo(Navigation.GetUriWithQueryParameter(nameof(Unauthenticated), true));
                        }
                        else
                        {
                            var path = new StringBuilder(WikiBlazorClientOptions.LoginPath)
                                .Append(WikiBlazorClientOptions.LoginPath.Contains('?')
                                    ? '&' : '?')
                                .Append("returnUrl=")
                                .Append(Navigation.Uri);
                            Navigation.NavigateTo(path.ToString());
                        }
                    }
                }
                Content = string.IsNullOrEmpty(item?.Html)
                    ? null
                    : new MarkupString(item.Html);
                IsDiff = item?.IsDiff == true;
                WikiState.UpdateTitle(item?.DisplayTitle);
            }
            else
            {
                CanCreate = false;
                CanEdit = false;
                Content = null;
                WikiItem = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            CanCreate = false;
            CanEdit = false;
            Content = null;
            WikiItem = null;
            SnackbarService.Add("An error occurred", ThemeColor.Danger);
            return;
        }
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        => await ReloadAsync();

    private void OnSetSearchText()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return;
        }

        Navigation.NavigateTo(WikiState.Link(
            "Search",
            WikiOptions.SystemNamespace,
            query: $"filter={SearchText}"));
    }

    private async void OnStateChanged(object? sender) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        if (_initialized)
        {
            await JSRuntime.InvokeVoidAsync("wikiblazor.hidePreview");
        }
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

    private void Reset()
    {
        CanCreate = false;
        CanEdit = false;
        IsAllSpecials = false;
        IsCategory = false;
        IsEditing = false;
        IsFile = false;
        IsGroupPage = false;
        IsSearch = false;
        IsSpecialList = false;
        IsUserPage = false;
        NoRedirect = false;
        PendingPreview = false;
        RequestedDiffCurrent = false;
        RequestedDiffPrevious = false;
        RequestedDiffTimestamp = null;
        RequestedTimestamp = null;
        SearchText = null;
        ShowHistory = false;
        SpecialListType = SpecialListType.None;
        User = null;
        WikiItem = null;
        WikiState.IsSystem = false;
        WikiState.LoadError = false;
        WikiState.UpdateTitle(null);
    }

    private void SetIsCompact()
    {
        WikiState.SetIsCompact(Compact || Navigation.GetQueryParam<bool>("compact"));
        if (!WikiState.IsCompact)
        {
            var uri = new Uri(Navigation.Uri);
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
        var relativeUri = Navigation.ToBaseRelativePath(Navigation.Uri);
        if (relativeUri.StartsWith(WikiOptions.WikiLinkPrefix, StringComparison.OrdinalIgnoreCase))
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

        Descending = Navigation.GetQueryParam<bool>("descending");
        Diff = Navigation.GetQueryParam<string>("diff");
        Editor = Navigation.GetQueryParam<string>("editor");
        End = Navigation.GetQueryParam<long?>("end");
        Filter = Navigation.GetQueryParam<string>("filter");
        NoRedirect = Navigation.GetQueryParam<bool>("noRedirect");
        PageNumber = Navigation.GetQueryParam<int?>("pageNumber");
        PageSize = Navigation.GetQueryParam<int?>("pageSize");
        Revision = Navigation.GetQueryParam<string>("rev");
        SearchNamespace = Navigation.GetQueryParam<string>("searchNamespace");
        SearchOwner = Navigation.GetQueryParam<string>("searchOwner");
        Sort = Navigation.GetQueryParam<string>("sort");
        Start = Navigation.GetQueryParam<long?>("start");
        Unauthenticated = Navigation.GetQueryParam<bool>("unauthenticated");
    }

    private void SetRouteProperties()
    {
        (
            WikiState.WikiNamespace,
            WikiState.WikiTitle,
            WikiState.IsTalk,
            WikiState.DefaultNamespace
        ) = Article.GetTitleParts(WikiOptions, Route);

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
            TargetNamespace = WikiState.WikiNamespace;
            TargetTitle = WikiState.WikiTitle;
            return;
        }

        if (WikiState.IsSystem)
        {
            IsSearch = string.Equals(WikiState.WikiTitle, "Search", StringComparison.OrdinalIgnoreCase);
            if (IsSearch)
            {
                return;
            }

            IsAllSpecials = string.Equals(WikiState.WikiTitle, "Special", StringComparison.OrdinalIgnoreCase);
            if (IsAllSpecials)
            {
                return;
            }

            IsUpload = string.Equals(WikiState.WikiTitle, "Upload", StringComparison.OrdinalIgnoreCase);
            if (IsUpload)
            {
                return;
            }

            if (Enum.TryParse<SpecialListType>(WikiState.WikiTitle, ignoreCase: true, out var type))
            {
                IsSpecialList = true;
                SpecialListType = type;
                WikiState.UpdateTitle(WikiState.WikiTitle.Replace('_', ' '));
                return;
            }
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
                RequestedTimestamp = timestamp;
            }
            else if (long.TryParse(Revision, out var ticks))
            {
                RequestedTimestamp = new DateTimeOffset(ticks, TimeSpan.Zero);
            }
        }

        if (!string.IsNullOrEmpty(Diff))
        {
            if (string.Equals(Diff, "prev", StringComparison.OrdinalIgnoreCase))
            {
                RequestedDiffPrevious = true;
            }
            else if (string.Equals(Diff, "cur", StringComparison.OrdinalIgnoreCase))
            {
                RequestedDiffCurrent = true;
            }
            else if (DateTimeOffset.TryParse(Diff, out var diffTimestamp))
            {
                RequestedDiffTimestamp = diffTimestamp;
            }
            else if (long.TryParse(Diff, out var diffTicks))
            {
                RequestedDiffTimestamp = new DateTimeOffset(diffTicks, TimeSpan.Zero);
            }
        }
    }
}