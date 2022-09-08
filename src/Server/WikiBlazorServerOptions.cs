namespace Tavenem.Wiki.Blazor.Server;

/// <summary>
/// Options used to configure the wiki system.
/// </summary>
public class WikiBlazorServerOptions : IWikiBlazorServerOptions
{
    /// <summary>
    /// The link template to be used for the Blazor wiki system.
    /// </summary>
    public const string DefaultLinkTemplate = "onmousemove=\"wikiblazor.showPreview(event, '{LINK}');\" onmouseleave=\"wikiblazor.hidePreview();\"";

    /// <summary>
    /// The relative URL of the <see cref="Web.SignalR.IWikiTalkHub"/> used if <see
    /// cref="TalkHubRoute"/> is not provided.
    /// </summary>
    public const string DefaultTalkHubRoute = "/wikiTalkHub";

    /// <summary>
    /// The relative URL of the wiki's server API used if <see
    /// cref="WikiServerApiRoute"/> is not provided.
    /// </summary>
    public const string DefaultWikiServerApiRoute = "/wikiapi";

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
    /// The relative URL of the <see cref="Web.SignalR.IWikiTalkHub"/>.
    /// </para>
    /// <para>
    /// If omitted, <see cref="DefaultTalkHubRoute"/> is used.
    /// </para>
    /// </summary>
    public string? TalkHubRoute { get; set; }

    /// <summary>
    /// <para>
    /// The relative URL of the wiki's server API.
    /// </para>
    /// <para>
    /// If omitted, the path "/wikiapi" will be used.
    /// </para>
    /// </summary>
    public string? WikiServerApiRoute { get; set; }
}
