using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Services;
using Tavenem.Wiki.Models;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// A component which supports wiki editing.
/// </summary>
public class WikiEditComponent : ComponentBase
{
    /// <summary>
    /// The content of the wiki article.
    /// </summary>
    protected string? Content { get; set; }

    /// <summary>
    /// <para>
    /// The HTML content of the full article.
    /// </para>
    /// <para>
    /// This may be a draft copy, rather than the stored version.
    /// </para>
    /// </summary>
    protected MarkupString HtmlContent { get; set; }

    /// <summary>
    /// Whether this is a script article.
    /// </summary>
    protected virtual bool IsScript { get; set; }

    /// <summary>
    /// <para>
    /// A preview of the HTML content of the full article.
    /// </para>
    /// <para>
    /// This may be a draft copy, rather than the stored version.
    /// </para>
    /// </summary>
    protected MarkupString PreviewContent { get; set; }

    /// <summary>
    /// The title of the wiki article.
    /// </summary>
    protected virtual string? Title { get; set; }

    /// <summary>
    /// An injected <see cref="Services.WikiDataService"/> instance.
    /// </summary>
    [Inject, NotNull] protected WikiDataService? WikiDataService { get; set; }

    /// <summary>
    /// Gets the wiki links in the content by calling the wiki server, or the offline data manager.
    /// </summary>
    protected async Task<List<WikiLink>?> GetWikiLinksAsync() => string.IsNullOrWhiteSpace(Content)
        ? null
        : await WikiDataService.GetWikiLinksAsync(
            new PreviewRequest(Content, PageTitle.Parse(Title)));

    /// <summary>
    /// Renders the HTML content by calling the wiki server, or the offline data manager.
    /// </summary>
    protected async Task HtmlAsync()
    {
        HtmlContent = new();
        if (string.IsNullOrWhiteSpace(Content))
        {
            return;
        }

        var preview = await WikiDataService.RenderHtmlAsync(
            new PreviewRequest(Content, PageTitle.Parse(Title)));
        HtmlContent = new(preview ?? string.Empty);
    }

    /// <summary>
    /// Renders the preview content by calling the wiki server, or the offline data manager.
    /// </summary>
    protected async Task PreviewAsync()
    {
        PreviewContent = new();
        if (string.IsNullOrWhiteSpace(Content))
        {
            return;
        }

        var preview = await WikiDataService.RenderPreviewAsync(
            new PreviewRequest(Content, PageTitle.Parse(Title)));
        PreviewContent = new(preview ?? string.Empty);
    }
}
