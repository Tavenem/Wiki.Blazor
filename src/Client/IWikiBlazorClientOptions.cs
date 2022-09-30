using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.SignalR;

namespace Tavenem.Wiki.Blazor.Client;

/// <summary>
/// Options used to configure the wiki system for the Blazor client.
/// </summary>
public interface IWikiBlazorClientOptions
{
    /// <summary>
    /// <para>
    /// The type of an optional component (typically containing an <see
    /// cref="Tavenem.Blazor.Framework.AppBar"/>) which will appear at the top of wiki pages.
    /// </para>
    /// <para>
    /// The type must implement <see cref="IComponent"/>.
    /// </para>
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type? AppBar { get; set; }

    /// <summary>
    /// <para>
    /// The type of layout used when requesting a compact version of a wiki page. Wiki pages will be nested
    /// within this layout.
    /// </para>
    /// <para>
    /// If omitted, a default layout will be used.
    /// </para>
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type? CompactLayout { get; set; }

    /// <summary>
    /// <para>
    /// The host part which will be recognized as indicating a request for the compact version
    /// of the wiki.
    /// </para>
    /// <para>
    /// If left empty the compact view cannot be reached at a particular host path.
    /// </para>
    /// </summary>
    string? CompactRouteHostPart { get; set; }

    /// <summary>
    /// <para>
    /// The position (zero-based) within the parts of the host string which will be examined to
    /// determine a request for the compact version of the wiki.
    /// </para>
    /// <para>
    /// If left <see langword="null"/> position zero will be assumed.
    /// </para>
    /// <para>
    /// Only used when <see cref="CompactRouteHostPart"/> is non-empty.
    /// </para>
    /// </summary>
    int? CompactRouteHostPosition { get; set; }

    /// <summary>
    /// <para>
    /// The port which will be recognized as indicating a request for the compact version of the
    /// wiki.
    /// </para>
    /// <para>
    /// If left <see langword="null"/> the compact view cannot be reached at a particular port.
    /// </para>
    /// </summary>
    int? CompactRoutePort { get; set; }

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
    /// of the parameter is from a ligetimate source to avoid exploits.
    /// </para>
    /// <para>
    /// If this option is omitted, a generic "not signed in" message will be displayed whenever
    /// a user who is not logged in attempts any action which requires an account.
    /// </para>
    /// </summary>
    string? LoginPath { get; set; }

    /// <summary>
    /// <para>
    /// The type of the main layout for the wiki. Wiki pages will be nested within this layout.
    /// </para>
    /// <para>
    /// If omitted, a default layout will be used.
    /// </para>
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type? MainLayout { get; set; }

    /// <summary>
    /// <para>
    /// The relative URL of the <see cref="IWikiTalkHub"/>.
    /// </para>
    /// <para>
    /// If omitted, the path "/wikiTalkHub" will be used.
    /// </para>
    /// </summary>
    string? TalkHubRoute { get; set; }

    /// <summary>
    /// <para>
    /// The API key to be used for Tenor GIF integration.
    /// </para>
    /// <para>
    /// Leave <see langword="null"/> (the default) to omit GIF functionality.
    /// </para>
    /// </summary>
    string? TenorAPIKey { get; set; }

    /// <summary>
    /// <para>
    /// The relative URL of the wiki's server API.
    /// </para>
    /// <para>
    /// If omitted, the path "/wikiapi" will be used.
    /// </para>
    /// </summary>
    string? WikiServerApiRoute { get; set; }

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
    Type? GetArticleEndMatter(Article article);

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
    Type? GetArticleFrontMatter(Article article);
}
