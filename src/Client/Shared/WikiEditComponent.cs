using Microsoft.AspNetCore.Components;
using Tavenem.Wiki.Models;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// A component which supports wiki editing.
/// </summary>
public class WikiEditComponent : OfflineSupportComponent
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
    /// Gets the wiki links in the content by calling the wiki server, or the offline data manager.
    /// </summary>
    protected async Task<List<WikiLink>?> GetWikiLinksAsync()
    {
        FixContent();
        if (string.IsNullOrWhiteSpace(Content))
        {
            return null;
        }

        var request = new PreviewRequest(Content, PageTitle.Parse(Title));
        return await PostAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/preview",
            request,
            WikiBlazorJsonSerializerContext.Default.PreviewRequest,
            WikiBlazorJsonSerializerContext.Default.ListWikiLink,
            user => WikiDataManager.GetWikiLinksAsync(user, request));
    }

    /// <summary>
    /// Renders the HTML content by calling the wiki server, or the offline data manager.
    /// </summary>
    protected async Task HtmlAsync()
    {
        HtmlContent = new();
        FixContent();
        if (string.IsNullOrWhiteSpace(Content))
        {
            return;
        }

        var request = new PreviewRequest(Content, PageTitle.Parse(Title));
        var preview = await PostForStringAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/html",
            request,
            WikiBlazorJsonSerializerContext.Default.PreviewRequest,
            user => WikiDataManager.RenderHtmlAsync(user, request));
        HtmlContent = new(preview ?? string.Empty);
    }

    /// <summary>
    /// Renders the preview content by calling the wiki server, or the offline data manager.
    /// </summary>
    protected async Task PreviewAsync()
    {
        PreviewContent = new();
        FixContent();
        if (string.IsNullOrWhiteSpace(Content))
        {
            return;
        }

        var request = new PreviewRequest(Content, PageTitle.Parse(Title));
        var preview = await PostForStringAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/preview",
            request,
            WikiBlazorJsonSerializerContext.Default.PreviewRequest,
            user => WikiDataManager.RenderHtmlAsync(user, request));
        PreviewContent = new(preview ?? string.Empty);
    }

    /// <summary>
    /// Repair escaped characters in the raw markdown.
    /// </summary>
    protected void FixContent()
    {
        if (!IsScript)
        {
            Content = Content?
                .Replace(@"\[\[", "[[")
                .Replace(@"\]\]", "]]");
        }
    }
}
