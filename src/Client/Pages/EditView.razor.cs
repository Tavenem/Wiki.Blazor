using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Blazor.Client.Shared;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The article edit view.
/// </summary>
public partial class EditView : OfflineSupportComponent
{
    /// <summary>
    /// The edited article.
    /// </summary>
    [Parameter] public Article? Article { get; set; }

    /// <summary>
    /// The current user.
    /// </summary>
    [Parameter] public IWikiUser? User { get; set; }

    private bool AllowDrafts => WikiOptions.UserDomains;

    private string? Comment { get; set; }

    private string? Content { get; set; }

    [Inject] DialogService DialogService { get; set; } = default!;

    private List<WikiUserInfo> Editors { get; set; } = new();

    private bool EditorSelf { get; set; }

    private bool HasDraft { get; set; } = true;

    private bool IsScript { get; set; }

    private bool NoOwner => !OwnerSelf && Owner.Count == 0;

    private List<WikiUserInfo> Owner { get; set; } = new();

    private bool OwnerSelf { get; set; }

    private MarkupString PreviewContent { get; set; }

    private bool Redirect { get; set; } = true;

    private bool RedirectEnabled { get; set; }

    [MemberNotNullWhen(false, nameof(Title))]
    [MemberNotNullWhen(false, nameof(User))]
    private bool SubmitDisabled => User is null
        || string.IsNullOrWhiteSpace(Title);

    private string? Title { get; set; }

    private List<WikiUserInfo> Viewers { get; set; } = new();

    private bool ViewerSelf { get; set; }

    /// <inheritdoc/>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<Article>(nameof(Article), out var newArticle)
            && !string.Equals(newArticle?.Id, Article?.Id))
        {
            if (newArticle is null)
            {
                Content = null;
                PreviewContent = new();
                Title = null;
            }
            else
            {
                Content = newArticle.MarkdownContent;
                PreviewContent = new(newArticle.Html);
                Title = Article.GetFullTitle(
                    WikiOptions,
                    newArticle.Title,
                    newArticle.WikiNamespace,
                    newArticle.Domain);
                IsScript = string.Equals(newArticle.WikiNamespace, WikiOptions.ScriptNamespace, StringComparison.Ordinal);
            }
        }
        return base.SetParametersAsync(parameters);
    }

    private async Task DeleteAsync()
    {
        var result = await DialogService.ShowMessageBox(
            "Confirm Delete",
            MessageBoxOptions.YesNo("Are you sure you want to delete this article?"));
        if (result == true)
        {
            await ReviseInnerAsync(true);
        }
    }

    private async Task DeleteDraftAsync()
    {
        if (!AllowDrafts || !HasDraft || SubmitDisabled)
        {
            return;
        }

        var (_, wikiNamespace, title, _, defaultNamespace) = Article.GetTitleParts(WikiOptions, Title);
        var url = new StringBuilder(WikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/item?title=")
            .Append(title);
        if (!defaultNamespace && !string.IsNullOrEmpty(wikiNamespace))
        {
            url.Append("&wikiNamespace=")
                .Append(wikiNamespace);
        }
        url.Append("&domain=")
            .Append(User.Id)
            .Append("&noRedirect=true");

        var item = await FetchDataAsync(
            url.ToString(),
            WikiBlazorJsonSerializerContext.Default.WikiItemInfo,
            async user => await WikiDataManager.GetItemAsync(
                user,
                title,
                wikiNamespace,
                User.Id,
                true));
        if (item?.Item is null)
        {
            HasDraft = false;
            return;
        }

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

        var request = new EditRequest(
            title,
            wikiNamespace,
            User.Id,
            null,
            "deleted",
            true,
            false,
            true,
            null,
            EditorSelf,
            ViewerSelf,
            allowedEditors,
            allowedViewers,
            allowedEditorGroups,
            allowedViewerGroups);
        var result = await PostAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/edit",
            request,
            WikiBlazorJsonSerializerContext.Default.EditRequest,
            user => WikiDataManager.EditAsync(user, request));
        if (result.Success)
        {
            HasDraft = false;
            SnackbarService.Add("Draft deleted successfully", ThemeColor.Success);
        }
    }

    private void FixContent()
    {
        if (!IsScript)
        {
            Content = Content?
                .Replace(@"\[\[", "[[")
                .Replace(@"\]\]", "]]");
        }
    }

    private async Task LoadDraftAsync()
    {
        if (!AllowDrafts || User is null)
        {
            return;
        }

        var (_, wikiNamespace, title, _, defaultNamespace) = Article.GetTitleParts(WikiOptions, Title);
        var url = new StringBuilder(WikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/item?title=")
            .Append(title);
        if (!defaultNamespace && !string.IsNullOrEmpty(wikiNamespace))
        {
            url.Append("&wikiNamespace=")
                .Append(wikiNamespace);
        }
        url.Append("&domain=")
            .Append(User.Id)
            .Append("&noRedirect=true");

        var item = await FetchDataAsync(
            url.ToString(),
            WikiBlazorJsonSerializerContext.Default.WikiItemInfo,
            async user => await WikiDataManager.GetItemAsync(
                user,
                title,
                wikiNamespace,
                User.Id,
                true));
        if (item?.Item is null)
        {
            HasDraft = false;
            SnackbarService.Add("No draft found", ThemeColor.Warning);
        }
        else
        {
            Content = item.Item.MarkdownContent;
            PreviewContent = string.IsNullOrEmpty(item.Html)
                ? new()
                : new(item.Html);
        }
    }

    private async Task OnTabChangedAsync(int? index)
    {
        if (index > 0)
        {
            await PreviewAsync();
        }
    }

    private void OnTitleChanged()
    {
        var (domain, wikiNamespace, title, _, _) = Article.GetTitleParts(WikiOptions, Title);
        if (string.Equals(wikiNamespace, WikiOptions.FileNamespace))
        {
            SnackbarService.Add("Cannot add articles to the file namespace.", ThemeColor.Warning);
            Title = title;
            IsScript = false;
        }
        else
        {
            IsScript = string.Equals(wikiNamespace, WikiOptions.ScriptNamespace, StringComparison.Ordinal);
        }
        RedirectEnabled = Article is not null
            && (!string.Equals(title, Article.Title)
            || !string.Equals(wikiNamespace, Article.WikiNamespace)
            || !string.Equals(domain, Article.Domain));
    }

    private async Task PreviewAsync()
    {
        PreviewContent = new();
        FixContent();
        if (string.IsNullOrWhiteSpace(Content))
        {
            return;
        }

        var (domain, wikiNamespace, title, _, _) = Article.GetTitleParts(WikiOptions, Title);
        var request = new PreviewRequest(Content, title, wikiNamespace, domain);
        var preview = await PostForStringAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/preview",
            request,
            WikiBlazorJsonSerializerContext.Default.PreviewRequest,
            user => WikiDataManager.PreviewAsync(user, request));
        PreviewContent = new(preview ?? string.Empty);
    }

    private Task ReviseAsync() => ReviseInnerAsync();

    private async Task ReviseInnerAsync(bool delete = false)
    {
        if (SubmitDisabled)
        {
            return;
        }

        FixContent();

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

        var (domain, wikiNamespace, title, _, _) = Article.GetTitleParts(WikiOptions, Title);
        var request = new EditRequest(
            title,
            wikiNamespace,
            domain,
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
        var result = await PostAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/edit",
            request,
            WikiBlazorJsonSerializerContext.Default.EditRequest,
            user => WikiDataManager.EditAsync(user, request),
            "A redirect could not be created automatically, but your revision was a success.");
        if (!string.IsNullOrEmpty(result.Message))
        {
            SnackbarService.Add(result.Message, ThemeColor.Warning);
        }
        if (result.Success)
        {
            NavigationManager.NavigateTo(WikiState.Link(title, wikiNamespace, domain));
        }
    }

    private async Task SaveDraftAsync()
    {
        if (!AllowDrafts || SubmitDisabled)
        {
            return;
        }

        FixContent();

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

        var (_, wikiNamespace, title, _, _) = Article.GetTitleParts(WikiOptions, Title);
        var request = new EditRequest(
            title,
            wikiNamespace,
            User.Id,
            Content,
            Comment?.Trim(),
            false,
            false,
            true,
            null,
            EditorSelf,
            ViewerSelf,
            allowedEditors,
            allowedViewers,
            allowedEditorGroups,
            allowedViewerGroups);
        var result = await PostAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/edit",
            request,
            WikiBlazorJsonSerializerContext.Default.EditRequest,
            user => WikiDataManager.EditAsync(user, request));
        if (result.Success)
        {
            HasDraft = true;
            SnackbarService.Add("Draft saved successfully", ThemeColor.Success);
        }
    }
}