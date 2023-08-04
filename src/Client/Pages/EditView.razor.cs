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
public partial class EditView : WikiEditComponent
{
    /// <summary>
    /// The edited article.
    /// </summary>
    [Parameter] public Page? Page { get; set; }

    /// <summary>
    /// The current user.
    /// </summary>
    [Parameter] public IWikiUser? User { get; set; }

    /// <summary>
    /// The title of the wiki article.
    /// </summary>
    protected override string? Title { get; set; }

    private bool AllowDrafts => WikiOptions.UserDomains;

    private string? Comment { get; set; }

    [Inject] DialogService DialogService { get; set; } = default!;

    private List<WikiUserInfo> Editors { get; set; } = new();

    private bool EditorSelf { get; set; }

    private bool HasDraft { get; set; } = true;

    private bool NoOwner => !OwnerSelf && Owner.Count == 0;

    private List<WikiUserInfo> Owner { get; set; } = new();

    private bool OwnerSelf { get; set; }

    private bool Redirect { get; set; } = true;

    private bool RedirectEnabled { get; set; }

    [MemberNotNullWhen(false, nameof(Title))]
    [MemberNotNullWhen(false, nameof(User))]
    private bool SubmitDisabled => User is null
        || string.IsNullOrWhiteSpace(Title);

    private List<WikiUserInfo> Viewers { get; set; } = new();

    private bool ViewerSelf { get; set; }

    /// <inheritdoc/>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<Article>(nameof(Page), out var newArticle)
            && !string.Equals(newArticle?.Id, Page?.Id))
        {
            if (newArticle is null)
            {
                Content = null;
                HtmlContent = new();
                Title = null;
            }
            else
            {
                Content = newArticle.MarkdownContent;
                HtmlContent = new(newArticle.Html);
                Title = newArticle.Title.ToString();
                IsScript = string.CompareOrdinal(newArticle.Title.Namespace, WikiOptions.ScriptNamespace) == 0;
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

        var draftPageTitle = PageTitle.Parse(Title).WithDomain(User.Id);
        var (title, @namespace, _) = draftPageTitle;
        var url = new StringBuilder(WikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/item?title=")
            .Append(title);
        if (!string.IsNullOrEmpty(@namespace))
        {
            url.Append("&namespace=")
                .Append(@namespace);
        }
        url.Append("&domain=")
            .Append(User.Id)
            .Append("&noRedirect=true");

        var item = await FetchDataAsync(
            url.ToString(),
            WikiJsonSerializerContext.Default.WikiPageInfo,
            async user => await WikiDataManager.GetItemAsync(
                user,
                draftPageTitle,
                true));
        if (item?.Page?.Exists != true)
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
            draftPageTitle,
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

    private async Task LoadDraftAsync()
    {
        if (!AllowDrafts || User is null)
        {
            return;
        }

        var draftPageTitle = PageTitle.Parse(Title).WithDomain(User.Id);
        var (title, @namespace, _) = draftPageTitle;
        var url = new StringBuilder(WikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/item?title=")
            .Append(title);
        if (!string.IsNullOrEmpty(@namespace))
        {
            url.Append("&namespace=")
                .Append(@namespace);
        }
        url.Append("&domain=")
            .Append(User.Id)
            .Append("&noRedirect=true");

        var item = await FetchDataAsync(
            url.ToString(),
            WikiJsonSerializerContext.Default.WikiPageInfo,
            async user => await WikiDataManager.GetItemAsync(
                user,
                draftPageTitle,
                true));
        if (item?.Page?.Exists != true)
        {
            HasDraft = false;
            SnackbarService.Add("No draft found", ThemeColor.Warning);
        }
        else
        {
            Content = item.Page.MarkdownContent;
            HtmlContent = string.IsNullOrEmpty(item.Html)
                ? new()
                : new(item.Html);
        }
    }

    private async Task OnTabChangedAsync(int? index)
    {
        if (index > 0)
        {
            await HtmlAsync();
        }
    }

    private void OnTitleChanged()
    {
        var pageTitle = PageTitle.Parse(Title);
        if (string.CompareOrdinal(pageTitle.Namespace, WikiOptions.FileNamespace) == 0)
        {
            SnackbarService.Add("Cannot add articles to the file namespace.", ThemeColor.Warning);
            Title = pageTitle.WithNamespace(null).ToString();
            IsScript = false;
        }
        else
        {
            IsScript = string.CompareOrdinal(pageTitle.Namespace, WikiOptions.ScriptNamespace) == 0;
        }
        var wasEnabled = RedirectEnabled;
        RedirectEnabled = Page is not null
            && !pageTitle.Equals(Page.Title);
        if (RedirectEnabled && !wasEnabled)
        {
            Redirect = true;
        }
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

        var title = PageTitle.Parse(Title);
        var request = new EditRequest(
            title,
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
            allowedViewerGroups,
            RedirectEnabled
                ? new PageTitle(WikiState.WikiTitle, WikiState.WikiNamespace, WikiState.WikiDomain)
                : null);
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
            NavigationManager.NavigateTo(WikiState.Link(title));
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

        var draftPagetitle = PageTitle.Parse(Title).WithDomain(User.Id);
        var request = new EditRequest(
            draftPagetitle,
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