using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The article edit view.
/// </summary>
public partial class EditView
{
    /// <summary>
    /// The edited article.
    /// </summary>
    [Parameter] public Article? Article { get; set; }

    private string? Comment { get; set; }

    private string? Content { get; set; }

    [Inject] DialogService DialogService { get; set; } = default!;

    private List<WikiUserInfo> Editors { get; set; } = new();

    private bool EditorSelf { get; set; }

    [Inject] private HttpClient HttpClient { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool NoOwner => !OwnerSelf && Owner.Count == 0;

    private List<WikiUserInfo> Owner { get; set; } = new();

    private bool OwnerSelf { get; set; }

    private string? Preview { get; set; }

    private bool Redirect { get; set; } = true;

    private bool RedirectEnabled { get; set; }

    [Inject] private SnackbarService SnackbarService { get; set; } = default!;

    [MemberNotNullWhen(false, nameof(Title))]
    private bool SubmitDisabled => string.IsNullOrWhiteSpace(Title)
        || Title.Contains(':');

    private string? Title { get; set; }

    private List<WikiUserInfo> Viewers { get; set; } = new();

    private bool ViewerSelf { get; set; }

    [Inject] private IWikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<Article>(nameof(Article), out var newArticle)
            && !string.Equals(newArticle?.Id, Article?.Id))
        {
            if (newArticle is null)
            {
                Content = null;
                Preview = null;
                Title = null;
            }
            else
            {
                Content = newArticle.MarkdownContent;
                Preview = newArticle.Html;
                Title = Article.GetFullTitle(WikiOptions, newArticle.Title, newArticle.WikiNamespace);
            }
        }
        return base.SetParametersAsync(parameters);
    }

    private async Task DeleteAsync()
    {
        var result = await DialogService.ShowMessageBox("Confirm Delete", MessageBoxOptions.YesNo("Are you sure you want to delete this article?"));
        if (result == true)
        {
            await ReviseInnerAsync(true);
        }
    }

    private void OnContentUpdated() => Preview = null;

    private void OnTitleChanged()
    {
        var (wikiNamespace, title, _, _) = Article.GetTitleParts(WikiOptions, Title);
        if (string.Equals(wikiNamespace, WikiOptions.FileNamespace))
        {
            SnackbarService.Add("Cannot add articles to the file namespace.", ThemeColor.Warning);
            Title = title;
        }
        RedirectEnabled = Article is not null
            && (!string.Equals(title, Article.Title)
            || !string.Equals(wikiNamespace, Article.WikiNamespace));
    }

    private async Task PreviewAsync()
    {
        Preview = null;
        if (string.IsNullOrWhiteSpace(Content))
        {
            return;
        }

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        var (wikiNamespace, title, _, _) = Article.GetTitleParts(WikiOptions, Title);
        try
        {
            var response = await HttpClient.PostAsJsonAsync(
                $"{serverApi}/preview",
                new PreviewRequest(Content, title, wikiNamespace),
                WikiBlazorJsonSerializerContext.Default.PreviewRequest);
            if (response.IsSuccessStatusCode)
            {
                Preview = await response.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            SnackbarService.Add("An error occurred", ThemeColor.Danger);
        }
    }

    private Task ReviseAsync() => ReviseInnerAsync();

    private async Task ReviseInnerAsync(bool delete = false)
    {
        if (SubmitDisabled)
        {
            return;
        }

        try
        {
            IList<string>? allowedEditors = null;
            IList<string>? allowedEditorGroups = null;
            if (!EditorSelf && Editors.Count > 0)
            {
                allowedEditors = Editors
                    .Where(x => x.Entity is IWikiUser)
                    .Select(x => x.Id)
                    .ToList();

                allowedEditorGroups = Editors
                    .Where(x => x.Entity is IWikiGroup)
                    .Select(x => x.Id)
                    .ToList();
            }

            IList<string>? allowedViewers = null;
            IList<string>? allowedViewerGroups = null;
            if (!ViewerSelf && Viewers.Count > 0)
            {
                allowedViewers = Viewers
                    .Where(x => x.Entity is IWikiUser)
                    .Select(x => x.Id)
                    .ToList();

                allowedViewerGroups = Viewers
                    .Where(x => x.Entity is IWikiGroup)
                    .Select(x => x.Id)
                    .ToList();
            }

            var (wikiNamespace, title, _, _) = Article.GetTitleParts(WikiOptions, Title);

            var request = new EditRequest(
                title,
                wikiNamespace,
                Content,
                Comment?.Trim(),
                delete,
                Redirect,
                OwnerSelf,
                OwnerSelf || Owner.Count < 1 ? null : Owner[0].Id,
                EditorSelf,
                ViewerSelf,
                allowedEditors,
                allowedViewers,
                allowedEditorGroups,
                allowedViewerGroups);

            var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
                ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
            var response = await HttpClient.PostAsJsonAsync(
                $"{serverApi}/edit",
                request,
                WikiBlazorJsonSerializerContext.Default.EditRequest);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                WikiState.NotAuthorized = true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                SnackbarService.Add(response.ReasonPhrase ?? "Invalid edit", ThemeColor.Warning);
            }
            else if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    SnackbarService.Add(response.ReasonPhrase, ThemeColor.Warning);
                }
                Navigation.NavigateTo(WikiState.Link(title, wikiNamespace));
            }
            else
            {
                Console.WriteLine(response.ReasonPhrase);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            SnackbarService.Add("An error occurred", ThemeColor.Danger);
        }
    }
}