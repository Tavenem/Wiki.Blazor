using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Web;
using Tavenem.Wiki.Blazor.Client.Pages;
using Tavenem.Wiki.Blazor.Client.Services;
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
public class Wiki : ComponentBase, IDisposable
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

    private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    [Inject, NotNull] private NavigationManager? NavigationManager { get; set; }

    private MarkupString Preview { get; set; }

    private string? Route { get; set; }

    [Inject, NotNull] private IServiceProvider? ServiceProvider { get; set; }

    [Inject, NotNull] private WikiBlazorOptions? WikiBlazorClientOptions { get; set; }

    [Inject, NotNull] private ClientWikiDataService? WikiDataService { get; set; }

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
    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2111:RequiresUnreferencedCode",
        Justification = "OpenComponent already has the right set of attributes")]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Microsoft.AspNetCore.Components.Web.PageTitle>(0);
        builder.AddContent(1, WikiState.PageTitle);
        builder.CloseComponent();

        if (WikiState.IsPreview)
        {
            builder.AddContent(2, Preview);
            return;
        }

        builder.OpenComponent<LayoutView>(3);
        builder.AddComponentParameter(
            4,
            nameof(LayoutView.Layout),
            typeof(WikiLayout));
        builder.AddComponentParameter(5, nameof(LayoutView.ChildContent), (RenderFragment)(builder =>
        {
            builder.OpenComponent<WikiPage>(6);
            builder.AddComponentParameter(7, nameof(WikiPage.Descending), Descending);
            builder.AddComponentParameter(8, nameof(WikiPage.Editor), Editor);
            builder.AddComponentParameter(9, nameof(WikiPage.End), End);
            builder.AddComponentParameter(10, nameof(WikiPage.Filter), Filter);
            builder.AddComponentParameter(11, nameof(WikiPage.NoRedirect), NoRedirect);
            builder.AddComponentParameter(12, nameof(WikiPage.PageNumber), PageNumber);
            builder.AddComponentParameter(13, nameof(WikiPage.PageSize), PageSize);
            builder.AddComponentParameter(14, nameof(WikiPage.Revisions), Revisions);
            builder.AddComponentParameter(15, nameof(WikiPage.Route), Route);
            builder.AddComponentParameter(16, nameof(WikiPage.SearchDomain), SearchDomain);
            builder.AddComponentParameter(17, nameof(WikiPage.SearchNamespace), SearchNamespace);
            builder.AddComponentParameter(18, nameof(WikiPage.SearchOwner), SearchOwner);
            builder.AddComponentParameter(19, nameof(WikiPage.Sort), Sort);
            builder.AddComponentParameter(20, nameof(WikiPage.Start), Start);
            builder.AddComponentParameter(21, nameof(WikiPage.Unauthenticated), Unauthenticated);
            builder.CloseComponent();
        }));
        builder.CloseComponent();
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

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        => await RefreshAsync();

    private async void OnStateChanged(object? sender) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        Reset();
        SetIsCompact();
        SetRoute();
        if (WikiState.IsPreview)
        {
            var title = WikiState.GetCurrentPageTitle();
            Preview = new(await WikiDataService.GetPreviewAsync(title) ?? string.Empty);
        }
        StateHasChanged();
    }

    private void Reset()
    {
        Preview = new(string.Empty);
        WikiState.IsEditing = false;
        WikiState.IsPreview = false;
        WikiState.IsSystem = false;
        WikiState.IsTalk = false;
        WikiState.LoadError = false;
        WikiState.ShowHistory = false;
        WikiState.ShowWhatLinksHere = false;
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
                WikiState.IsEditing = true;
                break;
            case { } when actionString.Equals("history", StringComparison.OrdinalIgnoreCase):
                WikiState.ShowHistory = true;
                break;
            case { } when actionString.Equals("preview", StringComparison.OrdinalIgnoreCase):
                WikiState.IsPreview = true;
                break;
            case { } when actionString.Equals("whatlinkshere", StringComparison.OrdinalIgnoreCase):
                WikiState.ShowWhatLinksHere = true;
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

        (
            WikiState.WikiTitle,
            WikiState.WikiNamespace,
            WikiState.WikiDomain
        ) = PageTitle.Parse(Route);
        WikiState.DefaultNamespace = string.IsNullOrEmpty(WikiState.WikiNamespace);
    }
}