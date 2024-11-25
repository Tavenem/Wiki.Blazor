using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client;

/// <summary>
/// Various customization and configuration options for the wiki system.
/// </summary>
public class WikiBlazorOptions : WikiOptions
{
    /// <summary>
    /// The default URL of the wiki's server API.
    /// </summary>
    public const string DefaultWikiServerApiRoute = "/wikiapi";

    /// <summary>
    /// <para>
    /// The type of an optional component which will appear at the top of wiki pages.
    /// </para>
    /// <para>
    /// The type must implement <see cref="IComponent"/>, and require no parameters.
    /// </para>
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? AppBar { get; set; }

    /// <summary>
    /// The render mode to use for the <see cref="AppBar"/> component, or <see langword="null"/> to
    /// use static rendering.
    /// </summary>
    public IComponentRenderMode? AppBarRenderMode { get; set; }

    /// <summary>
    /// The link template to be used for the Blazor wiki system.
    /// </summary>
    public const string DefaultLinkTemplate = "onmousemove=\"wikiblazor.showPreview(event, '{LINK}');\" onmouseleave=\"wikiblazor.hidePreview();\"";

    /// <summary>
    /// <para>
    /// The type of layout used when requesting a compact version of a wiki page. Wiki pages will be
    /// nested within this layout.
    /// </para>
    /// <para>
    /// If omitted, a default layout will be used.
    /// </para>
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? CompactLayout { get; set; }

    /// <summary>
    /// <para>
    /// The host part which will be recognized as indicating a request for the compact version
    /// of the wiki.
    /// </para>
    /// <para>
    /// If left <see langword="null"/> the compact view can only be reached by using the query
    /// parameter "compact".
    /// </para>
    /// </summary>
    public string? CompactRouteHostPart { get; set; }

    /// <summary>
    /// <para>
    /// The position (zero-based) within the parts of the host string which will be examined to
    /// determine a request for the compact version of the wiki.
    /// </para>
    /// <para>
    /// If left <see langword="null"/> position zero will be assumed.
    /// </para>
    /// </summary>
    public int? CompactRouteHostPosition { get; set; }

    /// <summary>
    /// <para>
    /// The port which will be recognized as indicating a request for the compact version of the
    /// wiki.
    /// </para>
    /// <para>
    /// If left <see langword="null"/> the compact view cannot be reached at a particular port.
    /// </para>
    /// </summary>
    public int? CompactRoutePort { get; set; }

    /// <summary>
    /// An optional data store which the client can access directly (i.e. without reaching the
    /// server).
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the <see cref="WikiServerApiRoute"/> has also been defined, the client will try to reach
    /// the server first for all wiki operations. If the server cannot be reached or the requested
    /// content is unavailable at the server, the client will fall back to the local data store.
    /// </para>
    /// <para>
    /// If both the server and the local data store are unavailable, the wiki will remain
    /// operational, but will show no content and will not allow any content to be added.
    /// </para>
    /// <para>
    /// No automatic synchronization occurs from the local data store to the server (for instance
    /// when an offline client reestablishes network connectivity). If your app model requires
    /// synchronization of offline content to a server, that logic must be implemented separately.
    /// </para>
    /// </remarks>
    public virtual IDataStore? DataStore { get; set; }

    /// <summary>
    /// <para>
    /// The minimum permission the user must have in order to create an archive of a domain.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property does not apply when creating an archive for content without a domain, or for
    /// the entire wiki.
    /// </para>
    /// <para>
    /// Since it would be prohibitive to check individual pages' permission, archiving only requires
    /// that a user has this level of permission (defaults to <see cref="WikiPermission.Read"/>) for
    /// the target domain. This could represent a potential security breach, if individual pages
    /// within the domain are further restricted. It is strongly recommended that the ability to
    /// create archives is restricted in your client code in a manner specific to your
    /// implementation's use of domains, which guarantees that only those with the correct
    /// permissions can create archives.
    /// </para>
    /// </remarks>
    public WikiPermission DomainArchivePermission { get; set; } = WikiPermission.Read;

    /// <summary>
    /// <para>
    /// The interactive render mode used by interactive components.
    /// </para>
    /// <para>
    /// This is set to <see cref="RenderMode.InteractiveWebAssembly"/> by default, but may be
    /// assigned null to indicate SSR only (however, note that this disables editing).
    /// </para>
    /// </summary>
    public IComponentRenderMode? InteractiveRenderMode { get; set; } = RenderMode.InteractiveWebAssembly;

    /// <summary>
    /// <para>
    /// The relative path to the site's login page.
    /// </para>
    /// <para>
    /// For security reasons, only a local path is permitted. If your authentication mechanisms
    /// are handled externally, this should point to a local page which redirects to that source
    /// (either automatically or via interaction).
    /// </para>
    /// <para>
    /// A query parameter with the name "returnUrl" whose value is set to the page which
    /// initiated the logic request will be appended to this URL (if provided). Your login page
    /// may ignore this parameter, but to improve user experience it should redirect the user
    /// back to this URL after performing a successful login. Be sure to validate that the value
    /// of the parameter is from a legitimate source to avoid exploits.
    /// </para>
    /// <para>
    /// If this option is omitted, an unauthorized page will be displayed whenever a user who is
    /// not logged in attempts any action which requires an account.
    /// </para>
    /// </summary>
    public string? LoginPath { get; set; }

    /// <summary>
    /// <para>
    /// The type of the main layout for the application. Wiki pages will be nested within this
    /// layout.
    /// </para>
    /// <para>
    /// If omitted, a default layout will be used.
    /// </para>
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? MainLayout { get; set; }

    /// <summary>
    /// <para>
    /// The API key to be used for Tenor GIF integration.
    /// </para>
    /// <para>
    /// Leave <see langword="null"/> (the default) to omit GIF functionality.
    /// </para>
    /// </summary>
    public string? TenorAPIKey { get; set; }

    /// <summary>
    /// The relative URL of the wiki's server API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If a local <see cref="IDataStore"/> has also been defined, the client will try to reach the
    /// server first for all wiki operations. If this property is left empty, or the server cannot
    /// be reached, or the requested content is unavailable at the server, the client will fall back
    /// to the local data store.
    /// </para>
    /// <para>
    /// If both the server and the local data store are unavailable, the wiki will remain
    /// operational, but will show no content and will not allow any content to be added.
    /// </para>
    /// <para>
    /// No automatic synchronization occurs from the local data store to the server (for instance
    /// when an offline client reestablishes network connectivity). If your app model requires
    /// synchronization of offline content to a server, that logic must be implemented separately.
    /// </para>
    /// <para>
    /// This is initialized to <see langword="null"/> by default, but <see
    /// cref="DefaultWikiServerApiRoute"/> may be assigned to use the default value for a hosting
    /// server app with default values.
    /// </para>
    /// </remarks>
    public string? WikiServerApiRoute { get; set; }

    /// <summary>
    /// Constructs a new instance of <see cref="WikiBlazorOptions"/>.
    /// </summary>
    public WikiBlazorOptions() => LinkTemplate = DefaultLinkTemplate;

    /// <summary>
    /// Add these options to the service collection.
    /// </summary>
    public virtual IServiceCollection Add(IServiceCollection services)
    {
        InteractiveRenderSettings.InteractiveRenderMode = InteractiveRenderMode;

        services.TryAddScoped<IOfflineManager, OfflineManager>();
        services.TryAddScoped<IArticleRenderManager, ArticleRenderManager>();

        services.TryAddScoped<WikiOptions>(_ => this);
        services.AddScoped(_ => this);

        return services
            .AddTavenemFramework()
            .AddScoped<WikiState>()
            .AddScoped<ClientWikiDataService>()
            .AddScoped<WikiDataService>();
    }
}
