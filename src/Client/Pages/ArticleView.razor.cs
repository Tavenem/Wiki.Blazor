using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The article view.
/// </summary>
public partial class ArticleView
{
    /// <summary>
    /// The article to display.
    /// </summary>
    [Parameter] public Page? Page { get; set; }

    /// <summary>
    /// Whether the current user has permission to edit this article.
    /// </summary>
    [Parameter] public bool CanEdit { get; set; }

    /// <summary>
    /// The content to display.
    /// </summary>
    [Parameter] public MarkupString Content { get; set; }

    /// <summary>
    /// Whether to display a diff.
    /// </summary>
    [Parameter] public bool IsDiff { get; set; }

    /// <summary>
    /// The current user (may be null if the current user is browsing anonymously).
    /// </summary>
    [Parameter] public IWikiUser? User { get; set; }

    private IComponentRenderMode? EndMatterRenderMode { get; set; }

    private Type? EndMatterType { get; set; }

    private IComponentRenderMode? FrontMatterRenderMode { get; set; }

    private Type? FrontMatterType { get; set; }

    [Inject, NotNull] private WikiBlazorOptions? WikiBlazorClientOptions { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        EndMatterRenderMode = null;
        EndMatterType = null;
        FrontMatterRenderMode = null;
        FrontMatterType = null;

        if (Page is null)
        {
            return;
        }

        EndMatterType = WikiBlazorClientOptions.GetArticleEndMatter(Page);
        if (EndMatterType is not null)
        {
            EndMatterRenderMode = WikiBlazorClientOptions.GetArticleEndMatterRenderMode(Page);
        }

        FrontMatterType = WikiBlazorClientOptions.GetArticleFrontMatter(Page);
        if (FrontMatterType is not null)
        {
            FrontMatterRenderMode = WikiBlazorClientOptions.GetArticleFrontMatterRenderMode(Page);
        }
    }
}