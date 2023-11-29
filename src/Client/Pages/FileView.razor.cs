using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
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

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }
}