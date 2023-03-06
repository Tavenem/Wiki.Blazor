using Microsoft.AspNetCore.Components;
using Tavenem.Blazor.Framework;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The article view.
/// </summary>
public partial class ArticleView : IDisposable
{
    private readonly List<HeadingInfo> _headings = new();

    private bool _disposedValue;

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

    private Dictionary<string, object> FrontEndMatterParameters { get; set; } = new();

    [CascadingParameter] private FrameworkLayout? FrameworkLayout { get; set; }

    [Inject] private WikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

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

        ClearHeadings();
        if (FrameworkLayout is not null)
        {
            var topHeading = new HeadingInfo()
            {
                Id = "wiki-main-heading",
                Level = HeadingLevel.H1,
                Title = "[Top]",
            };
            _headings.Add(topHeading);
            FrameworkLayout.AddHeading(topHeading);
            if (Page.Headings is not null)
            {
                foreach (var heading in Page.Headings.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
                {
                    var headingInfo = new HeadingInfo()
                    {
                        Id = heading.Id,
                        Level = (HeadingLevel)Math.Clamp(heading.OffsetLevel, 1, 6),
                        Title = heading.Text,
                    };
                    _headings.Add(headingInfo);
                    FrameworkLayout.AddHeading(headingInfo);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                ClearHeadings();
            }

            _disposedValue = true;
        }
    }

    private void ClearHeadings()
    {
        if (FrameworkLayout is not null
            && _headings.Count > 0)
        {
            foreach (var heading in _headings)
            {
                FrameworkLayout.RemoveHeading(heading);
            }
        }
    }
}