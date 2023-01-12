using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Blazor.Client.Internal.Models;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The talk page.
/// </summary>
public partial class Talk : IAsyncDisposable
{
    private bool _disposedValue;
    private IJSObjectReference? _module;

    /// <summary>
    /// The topic ID.
    /// </summary>
    [Parameter] public string? TopicId { get; set; }

    private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    private bool CanPost { get; set; }

    private bool CanTalk { get; set; }

    [Inject] private HttpClient HttpClient { get; set; } = default!;

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private List<TalkMessageModel> TalkMessages { get; set; } = new();

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject] private SnackbarService SnackbarService { get; set; } = default!;

    private TimeSpan TimezoneOffset { get; set; }

    [Inject] private WikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        Navigation.LocationChanged += OnLocationChanged;
        CanTalk = !string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute);

        if (CanTalk)
        {
            AuthenticationStateProvider = ServiceProvider.GetService<AuthenticationStateProvider>();
            if (AuthenticationStateProvider is not null)
            {
                AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
            }

            var state = AuthenticationStateProvider is null
                ? null
                : await AuthenticationStateProvider.GetAuthenticationStateAsync();
            CanPost = state?.User.Identity?.IsAuthenticated == true;
        }

        _module = await JSRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./_content/Tavenem.Wiki.Blazor.Client/tavenem-timezone.js");

        var offset = await _module.InvokeAsync<int>("getTimezoneOffset");
        TimezoneOffset = TimeSpan.FromMinutes(offset);

        await ReloadAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Do not change this code. Put cleanup code in 'DisposeAsync(bool disposing)' method
        await DisposeAsync(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Navigation.LocationChanged -= OnLocationChanged;
                if (AuthenticationStateProvider is not null)
                {
                    AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
                }
                if (_module is not null)
                {
                    await _module.DisposeAsync();
                }
            }

            _disposedValue = true;
        }
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        var x = await task;
        CanPost = x.User.Identity?.IsAuthenticated == true;
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        if (string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute))
        {
            return;
        }

        IList<MessageResponse>? messages = null;
        try
        {
            var url = new StringBuilder(WikiBlazorClientOptions.WikiServerApiRoute)
                .Append("/talk?title=")
                .Append(WikiState.WikiTitle);
            if (!string.IsNullOrEmpty(WikiState.WikiNamespace))
            {
                url.Append("&namespace=")
                    .Append(WikiState.WikiNamespace);
            }
            var response = await HttpClient.GetAsync(url.ToString());
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                WikiState.NotAuthorized = true;
            }
            else if (response.StatusCode is System.Net.HttpStatusCode.BadRequest
                or System.Net.HttpStatusCode.NoContent)
            {
                Navigation.NavigateTo(
                    WikiState.Link(WikiState.WikiTitle, WikiState.WikiNamespace),
                    replace: true);
            }
            else
            {
                messages = await response.Content.ReadFromJsonAsync(WikiBlazorJsonSerializerContext.Default.ListMessageResponse);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            SnackbarService.Add("An error occurred", ThemeColor.Danger);
        }

        UpdateMessages(messages);
    }

    private TalkMessageModel? FindMessage(List<TalkMessageModel> list, string id)
    {
        foreach (var message in list)
        {
            if (message.Message.Id == id)
            {
                return message;
            }

            if (message.Replies is not null)
            {
                var match = FindMessage(message.Replies, id);
                if (match is not null)
                {
                    return match;
                }
            }
        }
        return null;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "ReplyRequest will not be trimmed if this method is called")]
    private async Task OnPostAsync(ReplyRequest reply)
    {
        IList<MessageResponse>? messages = null;
        if (!string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute))
        {
            try
            {
                var response = await HttpClient.PostAsJsonAsync(
                    $"{WikiBlazorClientOptions.WikiServerApiRoute}/talk",
                    reply);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    WikiState.NotAuthorized = true;
                }
                else if (response.StatusCode is System.Net.HttpStatusCode.BadRequest
                    or System.Net.HttpStatusCode.NoContent)
                {
                    Navigation.NavigateTo(
                        WikiState.Link(WikiState.WikiTitle, WikiState.WikiNamespace),
                        replace: true);
                }
                else
                {
                    messages = await response.Content.ReadFromJsonAsync(WikiBlazorJsonSerializerContext.Default.ListMessageResponse);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SnackbarService.Add("An error occurred", ThemeColor.Danger);
            }
        }

        UpdateMessages(messages);
    }

    private void UpdateMessages(IList<MessageResponse>? messages)
    {
        TalkMessages.Clear();
        if (messages is not null)
        {
            foreach (var message in messages)
            {
                var isReply = !string.IsNullOrEmpty(message.ReplyMessageId);
                TalkMessageModel? targetMessage = null;
                if (isReply)
                {
                    targetMessage = FindMessage(TalkMessages, message.ReplyMessageId!);
                    if (targetMessage is null)
                    {
                        isReply = false;
                    }
                }

                if (isReply)
                {
                    if (message.Content.IsEmoji())
                    {
                        var emoji = message.Content.Trim();
                        if (emoji.StartsWith("<p>"))
                        {
                            emoji = emoji[3..];
                        }
                        if (emoji.EndsWith("</p>"))
                        {
                            emoji = emoji[..^4];
                        }

                        targetMessage!.Reactions ??= new();
                        if (!targetMessage.Reactions.ContainsKey(emoji))
                        {
                            targetMessage.Reactions[emoji] = new();
                        }
                        targetMessage.Reactions[emoji].Add(message);
                    }
                    else
                    {
                        (targetMessage!.Replies ??= new()).Add(new(message));
                    }
                }
                else
                {
                    TalkMessages.Add(new(message));
                }
            }
        }
        TalkMessages.Sort((x, y) => x.Message.TimestampTicks.CompareTo(y.Message.TimestampTicks));
        foreach (var message in TalkMessages)
        {
            message.Replies?.Sort((x, y) => x.Message.TimestampTicks.CompareTo(y.Message.TimestampTicks));
        }
    }
}