using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;

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

    private Type? EndMatterType { get; set; }

    private Type? FrontMatterType { get; set; }

    private Dictionary<string, object> FrontEndMatterParameters { get; set; } = [];

    [CascadingParameter] private FrameworkLayout? FrameworkLayout { get; set; }

    [Inject, NotNull] private WikiBlazorClientOptions? WikiBlazorClientOptions { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        FrontEndMatterParameters.Clear();
        EndMatterType = null;
        FrontMatterType = null;

        if (Page is null)
        {
            return;
        }

        FrontEndMatterParameters.Add("Article", Page);
        FrontEndMatterParameters.Add("CanEdit", CanEdit);
        if (User is not null)
        {
            FrontEndMatterParameters.Add("User", User);
        }

        EndMatterType = WikiBlazorClientOptions.GetArticleEndMatter(Page);
        FrontMatterType = WikiBlazorClientOptions.GetArticleFrontMatter(Page);
    }
}