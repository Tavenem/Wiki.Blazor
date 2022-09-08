using Microsoft.AspNetCore.Components;
namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The file view.
/// </summary>
public partial class FileView
{
    /// <summary>
    /// The content to display.
    /// </summary>
    [Parameter] public MarkupString? Content { get; set; }

    /// <summary>
    /// The file to display.
    /// </summary>
    [Parameter] public WikiFile? WikiFile { get; set; }

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;
}