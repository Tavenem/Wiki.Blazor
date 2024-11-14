using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Blazor.Client.Shared;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The article edit view.
/// </summary>
public partial class EditView : WikiEditComponent
{
    /// <summary>
    /// Whether the title of the page can be changed.
    /// </summary>
    [Parameter] public bool CanRename { get; set; }

    /// <summary>
    /// The edited article.
    /// </summary>
    [Parameter] public Page? Page { get; set; }

    /// <summary>
    /// The ID of the current user (may be null if the current user is browsing anonymously).
    /// </summary>
    [Parameter] public string? UserId { get; set; }

    /// <summary>
    /// The title of the wiki article.
    /// </summary>
    protected override string? Title { get; set; }

    private bool AllowDrafts => WikiOptions.UserDomains;

    private string? Comment { get; set; }

    [Inject, NotNull] DialogService? DialogService { get; set; }

    private List<IWikiOwner> Editors { get; set; } = [];

    private bool EditorSelf { get; set; }

    private bool HasDraft { get; set; } = true;

    private bool IsInteractive { get; set; }

    [Inject, NotNull] private NavigationManager? NavigationManager { get; set; }

    private bool NoOwner => !OwnerSelf && Owner.Count == 0;

    private List<IWikiOwner> Owner { get; set; } = [];

    private bool OwnerSelf { get; set; }

    private bool Redirect { get; set; } = true;

    private bool RedirectEnabled { get; set; }

    [Inject, NotNull] private SnackbarService? SnackbarService { get; set; }

    [MemberNotNullWhen(false, nameof(Title))]
    [MemberNotNullWhen(false, nameof(UserId))]
    private bool SubmitDisabled => string.IsNullOrEmpty(UserId)
        || string.IsNullOrWhiteSpace(Title);

    private List<IWikiOwner> Viewers { get; set; } = [];

    private bool ViewerSelf { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<Page>(nameof(Page), out var newArticle)
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
                HtmlContent = new(newArticle.Html ?? string.Empty);
                Title = newArticle.Title.ToString();
                IsScript = string.CompareOrdinal(newArticle.Title.Namespace, WikiOptions.ScriptNamespace) == 0;
            }
        }
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            IsInteractive = true;
            StateHasChanged();
        }
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

        var draftPageTitle = PageTitle.Parse(Title).WithDomain(UserId);

        var item = await WikiDataService.GetItemAsync(draftPageTitle, true);
        if (item?.Exists != true)
        {
            HasDraft = false;
            return;
        }

        IList<string>? allowedEditors = null;
        IList<string>? allowedEditorGroups = null;
        if (!EditorSelf && Editors.Count > 0)
        {
            allowedEditors = Editors
                .Where(x => x is IWikiUser)
                .Select(x => x.Id)
                .ToList();

            allowedEditorGroups = Editors
                .Where(x => x is IWikiGroup)
                .Select(x => x.Id)
                .ToList();
        }

        IList<string>? allowedViewers = null;
        IList<string>? allowedViewerGroups = null;
        if (!ViewerSelf && Viewers.Count > 0)
        {
            allowedViewers = Viewers
                .Where(x => x is IWikiUser)
                .Select(x => x.Id)
                .ToList();

            allowedViewerGroups = Viewers
                .Where(x => x is IWikiGroup)
                .Select(x => x.Id)
                .ToList();
        }

        var result = await WikiDataService.EditAsync(new EditRequest(
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
            allowedViewerGroups));
        if (result)
        {
            HasDraft = false;
            SnackbarService.Add("Draft deleted successfully", ThemeColor.Success);
        }
    }

    private async Task LoadDraftAsync()
    {
        if (!AllowDrafts || string.IsNullOrEmpty(UserId))
        {
            return;
        }

        var item = await WikiDataService.GetItemAsync(
            PageTitle.Parse(Title).WithDomain(UserId),
            true);
        if (item?.Exists != true)
        {
            HasDraft = false;
            SnackbarService.Add("No draft found", ThemeColor.Warning);
        }
        else
        {
            Content = item.MarkdownContent;
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

        IList<string>? allowedEditors = null;
        IList<string>? allowedEditorGroups = null;
        if (!EditorSelf && Editors.Count > 0)
        {
            allowedEditors = Editors
                .Where(x => x is IWikiUser)
                .Select(x => x.Id)
                .ToList();

            allowedEditorGroups = Editors
                .Where(x => x is IWikiGroup)
                .Select(x => x.Id)
                .ToList();
        }

        IList<string>? allowedViewers = null;
        IList<string>? allowedViewerGroups = null;
        if (!ViewerSelf && Viewers.Count > 0)
        {
            allowedViewers = Viewers
                .Where(x => x is IWikiUser)
                .Select(x => x.Id)
                .ToList();

            allowedViewerGroups = Viewers
                .Where(x => x is IWikiGroup)
                .Select(x => x.Id)
                .ToList();
        }

        var title = PageTitle.Parse(Title);
        var result = await WikiDataService.EditAsync(
            new EditRequest(
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
                    : null),
            "A redirect could not be created automatically, but your revision was a success.");
        if (result)
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

        IList<string>? allowedEditors = null;
        IList<string>? allowedEditorGroups = null;
        if (!EditorSelf && Editors.Count > 0)
        {
            allowedEditors = Editors
                .Where(x => x is IWikiUser)
                .Select(x => x.Id)
                .ToList();

            allowedEditorGroups = Editors
                .Where(x => x is IWikiGroup)
                .Select(x => x.Id)
                .ToList();
        }

        IList<string>? allowedViewers = null;
        IList<string>? allowedViewerGroups = null;
        if (!ViewerSelf && Viewers.Count > 0)
        {
            allowedViewers = Viewers
                .Where(x => x is IWikiUser)
                .Select(x => x.Id)
                .ToList();

            allowedViewerGroups = Viewers
                .Where(x => x is IWikiGroup)
                .Select(x => x.Id)
                .ToList();
        }

        var draftPagetitle = PageTitle.Parse(Title).WithDomain(UserId);
        var result = await WikiDataService.EditAsync(new EditRequest(
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
            allowedViewerGroups));
        if (result)
        {
            HasDraft = true;
            SnackbarService.Add("Draft saved successfully", ThemeColor.Success);
        }
    }
}