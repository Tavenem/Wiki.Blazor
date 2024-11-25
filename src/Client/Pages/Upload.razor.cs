using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The upload page.
/// </summary>
public partial class Upload
{
    private const string _baseDragAreaClass = "container rounded p-4 my-4";

    /// <summary>
    /// The ID of the current user (may be null if the current user is browsing anonymously).
    /// </summary>
    [Parameter] public string? UserId { get; set; }

    private string? Comment { get; set; }

    private string? Content { get; set; }

    [Inject, NotNull] private DialogService? DialogService { get; set; }

    private string DragAreaClass { get; set; } = _baseDragAreaClass;

    private List<IWikiOwner> Editors { get; set; } = [];

    private bool EditorSelf { get; set; }

    private IBrowserFile? File { get; set; }

    private string? FileName => File?.Name;

    private HttpClient? HttpClient { get; set; }

    private bool InsufficientSpace { get; set; }

    [Inject, NotNull] private IOfflineManager? OfflineManager { get; set; }

    private bool IsInteractive { get; set; }

    [Inject, NotNull] private NavigationManager? NavigationManager { get; set; }

    private bool NoOwner => !OwnerSelf && Owner.Count == 0;

    private bool NotAuthorized { get; set; }

    private List<IWikiOwner> Owner { get; set; } = [];

    private bool OwnerSelf { get; set; }

    private string? Preview { get; set; }

    [Inject, NotNull] private IServiceProvider? ServiceProvider { get; set; }

    [Inject, NotNull] private SnackbarService? SnackbarService { get; set; }

    [MemberNotNullWhen(false, nameof(File), nameof(Title))]
    private bool SubmitDisabled => File is null
        || string.IsNullOrWhiteSpace(Title)
        || Title.Contains(':');

    private string? Title { get; set; }

    private List<IWikiOwner> Viewers { get; set; } = [];

    private bool ViewerSelf { get; set; }

    [Inject, NotNull] private WikiBlazorOptions? WikiBlazorClientOptions { get; set; }

    [Inject, NotNull] private ClientWikiDataService? WikiDataService { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    protected override void OnInitialized() => HttpClient = ServiceProvider.GetService<HttpClient>();

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync() => NotAuthorized = await WikiDataService.GetUploadLimitAsync() == 0;

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            IsInteractive = true;
            StateHasChanged();
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async IAsyncEnumerable<string> TitleValidation(string? value, object? _)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }
        if (value.Contains(':'))
        {
            yield return "Files cannot be given a namespace (the ':' character is not allowed).";
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    private void OnClear()
    {
        OnDragEnd();
        File = null;
    }

    private void OnContentUpdated() => Preview = null;

    private void OnDragEnd() => DragAreaClass = _baseDragAreaClass;

    private void OnDragEnter() => DragAreaClass = $"{_baseDragAreaClass} border-primary";

    private void OnInputChanged(InputFileChangeEventArgs e)
    {
        OnDragEnd();
        File = e.File;
    }

    private async Task PreviewAsync()
    {
        Preview = null;
        if (string.IsNullOrWhiteSpace(Content))
        {
            return;
        }

        var title = PageTitle.Parse(Title);
        if (string.CompareOrdinal(title.Namespace, WikiOptions.FileNamespace) != 0)
        {
            return;
        }

        Preview = await WikiDataService.RenderHtmlAsync(new PreviewRequest(Content, title));
    }

    private async Task UploadAsync(bool confirmOverwrite = false)
    {
        if (SubmitDisabled)
        {
            return;
        }

        var title = PageTitle.Parse(Title);
        if (string.CompareOrdinal(title.Namespace, WikiOptions.FileNamespace) != 0)
        {
            return;
        }

        NotAuthorized = false;
        InsufficientSpace = false;

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

        var request = new UploadRequest(
            title,
            Content,
            confirmOverwrite,
            Comment?.Trim(),
            false,
            OwnerSelf,
            OwnerSelf || Owner.Count < 1 ? null : Owner[0].Id,
            EditorSelf,
            ViewerSelf,
            allowedEditors,
            allowedViewers,
            allowedEditorGroups,
            allowedViewerGroups);

        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(File.OpenReadStream(WikiOptions.MaxFileSize));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(File.ContentType);
            content.Add(fileContent, "\"file\"", File.Name);

            var json = JsonSerializer.Serialize(request,
                WikiBlazorJsonSerializerContext.Default.UploadRequest);
            content.Add(
                new StringContent(
                    json,
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")),
                "\"options\"");

            var isLocal = string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute);
            if (!isLocal
                && !string.IsNullOrEmpty(WikiState.WikiDomain))
            {
                isLocal = await OfflineManager.IsOfflineDomainAsync(WikiState.WikiDomain);
            }
            if (!isLocal && HttpClient is not null)
            {
                var response = await HttpClient.PostAsync(
                    $"{WikiBlazorClientOptions.WikiServerApiRoute}/upload",
                    content);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    if (string.IsNullOrEmpty(UserId))
                    {
                        if (string.IsNullOrEmpty(WikiBlazorClientOptions.LoginPath))
                        {
                            NavigationManager.NavigateTo(
                                NavigationManager.GetUriWithQueryParameter(
                                    nameof(Wiki.Unauthenticated),
                                    true));
                        }
                        else
                        {
                            var path = new StringBuilder(WikiBlazorClientOptions.LoginPath)
                                .Append(WikiBlazorClientOptions.LoginPath.Contains('?')
                                    ? '&' : '?')
                                .Append("returnUrl=")
                                .Append(UrlEncoder.Default.Encode(NavigationManager.Uri));
                            NavigationManager.NavigateTo(path.ToString());
                        }
                    }
                    NotAuthorized = true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    if (response.ReasonPhrase?.Contains("exceeds") == true)
                    {
                        InsufficientSpace = true;
                    }
                    else
                    {
                        SnackbarService.Add(response.ReasonPhrase ?? "Invalid upload", ThemeColor.Warning);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var confirm = await DialogService.ShowMessageBox(
                        "Confirm overwrite",
                        MessageBoxOptions.YesNo("A file with this title already exists. Are you sure you want to overwrite the existing file?"));
                    if (confirm == true)
                    {
                        await UploadAsync(true);
                    }
                }
                else if (response.IsSuccessStatusCode)
                {
                    NavigationManager.NavigateTo(WikiState.Link(title));
                }
            }
        }
        catch (HttpRequestException) { }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            SnackbarService.Add("An error occurred", ThemeColor.Danger);
        }
    }
}