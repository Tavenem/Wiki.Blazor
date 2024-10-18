using Microsoft.AspNetCore.Components;

namespace Tavenem.Wiki.Blazor.Client;

/// <summary>
/// A component rendered dynamically in the wiki page view.
/// </summary>
public class WikiComponent : ComponentBase
{
    /// <summary>
    /// Whether the current user has permission to edit this page.
    /// </summary>
    [Parameter] public bool CanEdit { get; set; }

    /// <summary>
    /// The page to display.
    /// </summary>
    [Parameter] public Page? Page { get; set; }

    /// <summary>
    /// The current user (may be null if the current user is browsing anonymously).
    /// </summary>
    [Parameter] public IWikiUser? User { get; set; }
}