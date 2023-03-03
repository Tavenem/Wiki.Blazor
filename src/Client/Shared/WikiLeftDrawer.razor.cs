using Microsoft.AspNetCore.Components;

namespace Tavenem.Wiki.Blazor.Client;

/// <summary>
/// The left drawer for the default main layout of the <see cref="Wiki"/> component.
/// </summary>
public partial class WikiLeftDrawer
{
    /// <summary>
    /// Whether to show the links relevant to a currently displayed wiki page. Defaults to <see
    /// langword="true"/>.
    /// </summary>
    /// <remarks>
    /// This can be set to <see langword="false"/> on pages which are not themselves wiki articles,
    /// but still wish to display the links to main wiki pages.
    /// </remarks>
    [Parameter] public bool ShowPageTools { get; set; } = true;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;
}