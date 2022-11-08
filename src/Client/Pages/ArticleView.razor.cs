using Microsoft.AspNetCore.Components;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The article view.
/// </summary>
public partial class ArticleView
{
    /// <summary>
    /// The article to display.
    /// </summary>
    [Parameter] public Article? Article { get; set; }

    /// <summary>
    /// Whether the current user has permission to edit this article.
    /// </summary>
    [Parameter] public bool CanEdit { get; set; }

    /// <summary>
    /// The content to display.
    /// </summary>
    [Parameter] public MarkupString? Content { get; set; }

    /// <summary>
    /// Whether to display a diff.
    /// </summary>
    [Parameter] public bool IsDiff { get; set; }

    /// <summary>
    /// The current user (may be null if the current user is browsing anonymously).
    /// </summary>
    [Parameter] public IWikiUser? User { get; set; }

    private Type? EndMatterType { get; set; }

    private Type? FrontMatterType { get; set; }

    private Dictionary<string, object> FrontEndMatterParamaters { get; set; } = new();

    [Inject] private WikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        FrontEndMatterParamaters.Clear();
        EndMatterType = null;
        FrontMatterType = null;

        if (Article is null)
        {
            return;
        }

        FrontEndMatterParamaters.Add("Article", Article);
        FrontEndMatterParamaters.Add("CanEdit", CanEdit);
        if (User is not null)
        {
            FrontEndMatterParamaters.Add("User", User);
        }

        EndMatterType = WikiBlazorClientOptions.GetArticleEndMatter(Article);
        FrontMatterType = WikiBlazorClientOptions.GetArticleFrontMatter(Article);
    }
}