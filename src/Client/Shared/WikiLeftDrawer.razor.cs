using Microsoft.AspNetCore.Components;

namespace Tavenem.Wiki.Blazor.Client;

/// <summary>
/// The left drawer for the default main layout of the <see cref="Wiki"/> component.
/// </summary>
public partial class WikiLeftDrawer
{
    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;
}