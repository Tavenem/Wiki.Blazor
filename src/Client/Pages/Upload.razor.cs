using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The upload page.
/// </summary>
public partial class Upload : IDisposable
{
    private const string _baseDragAreaClass = "container rounded p-4 my-4";

    private bool _disposedValue;

    private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    private string? Comment { get; set; }

    private string? Content { get; set; }

    [Inject] private DialogService DialogService { get; set; } = default!;

    private string DragAreaClass { get; set; } = _baseDragAreaClass;

    private List<WikiUserInfo> Editors { get; set; } = new();

    private bool EditorSelf { get; set; }

    private IBrowserFile? File { get; set; }

    private string? FileName => File?.Name;

    [Inject] private HttpClient HttpClient { get; set; } = default!;

    private bool InsufficientSpace { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool NoOwner => !OwnerSelf && Owner.Count == 0;

    private bool NotAuthorized { get; set; }

    private List<WikiUserInfo> Owner { get; set; } = new();

    private bool OwnerSelf { get; set; }

    private string? Preview { get; set; }

    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject] private SnackbarService SnackbarService { get; set; } = default!;

    [MemberNotNullWhen(false, nameof(File), nameof(Title))]
    private bool SubmitDisabled => File is null
        || string.IsNullOrWhiteSpace(Title)
        || Title.Contains(':');

    private string? Title { get; set; }

    private List<WikiUserInfo> Viewers { get; set; } = new();

    private bool ViewerSelf { get; set; }

    [Inject] private IWikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        AuthenticationStateProvider = ServiceProvider.GetService<AuthenticationStateProvider>();
        if (AuthenticationStateProvider is not null)
        {
            AuthenticationStateProvider.AuthenticationStateChanged += OnStateChanged;
        }
        await RefreshAsync();
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
            if (disposing && AuthenticationStateProvider is not null)
            {
                AuthenticationStateProvider.AuthenticationStateChanged -= OnStateChanged;
            }

            _disposedValue = true;
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

    private async void OnStateChanged(object? sender) => await RefreshAsync();

    private async Task PreviewAsync()
    {
        Preview = null;
        if (string.IsNullOrWhiteSpace(Content))
        {
            return;
        }

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        try
        {
            var response = await HttpClient.PostAsJsonAsync(
                $"{serverApi}/preview",
                new PreviewRequest(Content, Title, WikiOptions.FileNamespace),
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

    private async Task RefreshAsync()
    {
        NotAuthorized = false;

        var state = AuthenticationStateProvider is null
            ? null
            : await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (state?.User.Identity?.IsAuthenticated != true)
        {
            if (string.IsNullOrEmpty(WikiBlazorClientOptions.LoginPath))
            {
                Navigation.NavigateTo(Navigation.GetUriWithQueryParameter(nameof(Wiki.Unauthenticated), true));
                return;
            }
            var path = new StringBuilder(WikiBlazorClientOptions.LoginPath)
                .Append(WikiBlazorClientOptions.LoginPath.Contains('?')
                    ? '&' : '?')
                .Append("returnUrl=")
                .Append(Navigation.Uri);
            Navigation.NavigateTo(path.ToString());
        }

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        try
        {
            var response = await HttpClient.GetAsync($"{serverApi}/uploadlimit");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                || !int.TryParse(await response.Content.ReadAsStringAsync(), out var limit)
                || limit == 0)
            {
                NotAuthorized = true;
            }
        }
        catch (Exception ex)
        {
            NotAuthorized = true;
            Console.WriteLine(ex);
            SnackbarService.Add("An error occurred", ThemeColor.Danger);
        }
    }

    private async Task UploadAsync(bool confirmOverwrite = false)
    {
        if (SubmitDisabled)
        {
            return;
        }

        NotAuthorized = false;
        InsufficientSpace = false;

        try
        {
            using var request = new MultipartFormDataContent();
            var fileContent = new StreamContent(File.OpenReadStream(WikiOptions.MaxFileSize));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(File.ContentType);
            request.Add(fileContent, "\"file\"", File.Name);

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
            var json = JsonSerializer.Serialize(new UploadRequest(
                Title.Trim(),
                Content,
                confirmOverwrite,
                Comment?.Trim(),
                OwnerSelf,
                OwnerSelf || Owner.Count < 1 ? null : Owner[0].Id,
                EditorSelf,
                ViewerSelf,
                allowedEditors,
                allowedViewers,
                allowedEditorGroups,
                allowedViewerGroups),
                WikiBlazorJsonSerializerContext.Default.UploadRequest);
            request.Add(
                new StringContent(
                    json,
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")),
                "\"options\"");

            var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
                ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
            var response = await HttpClient.PostAsync($"{serverApi}/upload", request);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                NotAuthorized = true;
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
            else if (response.IsSuccessStatusCode)
            {
                Navigation.NavigateTo(WikiState.Link(Title, WikiOptions.FileNamespace));
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