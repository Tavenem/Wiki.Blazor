using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.DataStorage;

namespace Tavenem.Wiki.Blazor.Client;

/// <summary>
/// Gets the type of a component for a given wiki page.
/// </summary>
/// <param name="page">The page for which to get a component type.</param>
/// <returns>The type of a component.</returns>
[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public delegate Type? GetArticleComponent(Page page);

/// <summary>
/// Determines whether the given content may be edited locally.
/// </summary>
/// <param name="title">The title of the content to be edited.</param>
/// <param name="wikiNamespace">The namespace of the content to be edited.</param>
/// <param name="domain">The domain of the content to be edited (if any).</param>
/// <returns>
/// <see langword="true"/> if the content can be edited locally; otherwise <see langword="false"/>.
/// </returns>
/// <remarks>
/// Locally means in the local <see cref="WikiBlazorClientOptions.DataStore"/> instance, rather than
/// via the <see cref="WikiBlazorClientOptions.WikiServerApiRoute"/>.
/// </remarks>
public delegate ValueTask<bool> CanEditOfflineFunc(string title, string wikiNamespace, string? domain);

/// <summary>
/// A function which determines whether the given domain should always be retrieved from the local
/// <see cref="WikiBlazorClientOptions.DataStore"/>, and never from the server.
/// </summary>
/// <param name="domain">A wiki domain name.</param>
/// <returns>
/// <see langword="true"/> if the content should always be retrieved from the local <see
/// cref="WikiBlazorClientOptions.DataStore"/>; <see langword="false"/> if the content should be
/// retrieved from the server when possible.
/// </returns>
public delegate ValueTask<bool> IsOfflineDomainFunc(string domain);

/// <summary>
/// Options used to configure the wiki system.
/// </summary>
public class WikiBlazorClientOptions
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
    /// The link template to be used for the Blazor wiki system.
    /// </summary>
    public const string DefaultLinkTemplate = "onmousemove=\"wikiblazor.showPreview(event, '{LINK}');\" onmouseleave=\"wikiblazor.hidePreview();\"";

    /// <summary>
    /// A function which gets the type of a component which should be displayed after the content of
    /// the given wiki article (before the category list).
    /// </summary>
    public GetArticleComponent? ArticleEndMatter { get; set; }

    /// <summary>
    /// A function which gets the type of a component which should be displayed before the content
    /// of the given wiki article (after the subtitle).
    /// </summary>
    public GetArticleComponent? ArticleFrontMatter { get; set; }

    /// <summary>
    /// Can be set to a function which determines whether content may be edited locally.
    /// </summary>
    /// <remarks>
    /// If this function is not defined, no content may be edited locally (i.e. local content may
    /// only be viewed).
    /// </remarks>
    public CanEditOfflineFunc? CanEditOffline { get; set; }

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
    public IDataStore? DataStore { get; set; }

    /// <summary>
    /// A function which determines whether the given domain should always be retrieved from the
    /// local <see cref="DataStore"/>, and never from the <see cref="WikiServerApiRoute"/>.
    /// </summary>
    /// <remarks>
    /// This function is ignored if <see cref="DataStore"/> or <see cref="WikiServerApiRoute"/> is
    /// unset.
    /// </remarks>
    public IsOfflineDomainFunc? IsOfflineDomain { get; set; }

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
    /// If the local <see cref="DataStore"/> has also been defined, the client will try to reach the
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
    /// Gets the type of a component which should be displayed after the content of the given wiki
    /// article (before the category list).
    /// </summary>
    /// <param name="article">A wiki article.</param>
    /// <returns>
    /// A component instance (or <see langword="null"/>).
    /// </returns>
    /// <remarks>
    /// The following parameters will be supplied to the component, if they exist:
    /// <list type="bullet">
    /// <listheader>
    /// <term>Name</term>
    /// <description>Value</description>
    /// </listheader>
    /// <item>
    /// <term>Article</term>
    /// <description>
    /// The currently displayed <see cref="Article"/> (may be <see langword="null"/>).
    /// </description>
    /// </item>
    /// <item>
    /// <term>CanEdit</term>
    /// <description>
    /// A boolean indicating whether the current user has permission to edit the displayed <see
    /// cref="Article"/>. Note that this may be <see langword="true"/> even if the article or the
    /// user are <see langword="null"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>User</term>
    /// <description>
    /// The current <see cref="IWikiUser"/> (may be <see langword="null"/>).
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? GetArticleEndMatter(Page article) => ArticleEndMatter?.Invoke(article);

    /// <summary>
    /// Gets the type of a component which should be displayed before the content of the given wiki
    /// article (after the subtitle).
    /// </summary>
    /// <param name="article">A wiki article.</param>
    /// <returns>
    /// A component instance (or <see langword="null"/>).
    /// </returns>
    /// <remarks>
    /// The following parameters will be supplied to the component, if they exist:
    /// <list type="bullet">
    /// <listheader>
    /// <term>Name</term>
    /// <description>Value</description>
    /// </listheader>
    /// <item>
    /// <term>Article</term>
    /// <description>
    /// The currently displayed <see cref="Article"/> (may be <see langword="null"/>).
    /// </description>
    /// </item>
    /// <item>
    /// <term>CanEdit</term>
    /// <description>
    /// A boolean indicating whether the current user has permission to edit the displayed <see
    /// cref="Article"/>. Note that this may be <see langword="true"/> even if the article or the
    /// user are <see langword="null"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>User</term>
    /// <description>
    /// The current <see cref="IWikiUser"/> (may be <see langword="null"/>).
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? GetArticleFrontMatter(Page article) => ArticleFrontMatter?.Invoke(article);
}
