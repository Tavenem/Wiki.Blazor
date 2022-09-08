namespace Tavenem.Wiki.Blazor.Server;

/// <summary>
/// Options used to configure the wiki system for the ASP.NET server app hosting the Blazor client.
/// </summary>
public interface IWikiBlazorServerOptions
{
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
    /// The relative URL of the <see cref="Web.SignalR.IWikiTalkHub"/>.
    /// </para>
    /// <para>
    /// If omitted, the path "/wikiTalkHub" will be used.
    /// </para>
    /// </summary>
    string? TalkHubRoute { get; set; }

    /// <summary>
    /// <para>
    /// The relative URL of the wiki's server API.
    /// </para>
    /// <para>
    /// If omitted, the path "/wikiapi" will be used.
    /// </para>
    /// </summary>
    string? WikiServerApiRoute { get; set; }
}
