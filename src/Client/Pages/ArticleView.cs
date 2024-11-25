using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The article view.
/// </summary>
public class ArticleView : ComponentBase
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

    [Inject, NotNull] private IArticleRenderManager? ArticleRenderManager { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2111:RequiresUnreferencedCode",
        Justification = "OpenComponent already has the right set of attributes")]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "wiki-site-subtitle");
        builder.AddContent(2, "From ");
        builder.AddContent(3, WikiOptions.SiteName);
        builder.CloseElement();

        if (!IsDiff && Page is not null)
        {
            var frontMatterType = ArticleRenderManager.GetArticleFrontMatter(Page);
            if (frontMatterType is not null)
            {
                var frontMatterRenderMode = ArticleRenderManager.GetArticleFrontMatterRenderMode(Page);
                builder.OpenComponent(4, frontMatterType);
                builder.AddAttribute(5, nameof(WikiComponent.Page), Page);
                builder.AddAttribute(6, nameof(WikiComponent.CanEdit), CanEdit);
                builder.AddAttribute(7, nameof(WikiComponent.User), User);
                if (frontMatterRenderMode is not null)
                {
                    builder.AddComponentRenderMode(frontMatterRenderMode);
                }
                builder.CloseComponent();
            }
        }

        builder.OpenElement(8, "tf-syntax-highlight");
        builder.AddAttribute(9, "class", "wiki-parser-output");
        builder.AddContent(10, Content);
        builder.CloseElement();

        if (!IsDiff && Page is not null)
        {
            var endMatterType = ArticleRenderManager.GetArticleEndMatter(Page);
            if (endMatterType is not null)
            {
                var endMatterRenderMode = ArticleRenderManager.GetArticleEndMatterRenderMode(Page);
                builder.OpenComponent(11, endMatterType);
                builder.AddAttribute(12, nameof(WikiComponent.Page), Page);
                builder.AddAttribute(13, nameof(WikiComponent.CanEdit), CanEdit);
                builder.AddAttribute(14, nameof(WikiComponent.User), User);
                if (endMatterRenderMode is not null)
                {
                    builder.AddComponentRenderMode(endMatterRenderMode);
                }
                builder.CloseComponent();
            }
        }
    }
}