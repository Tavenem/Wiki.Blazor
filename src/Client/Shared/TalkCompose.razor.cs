using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Blazor.Client.Internal.Models;
using Tavenem.Wiki.Blazor.SignalR;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// Composition controls for a talk page.
/// </summary>
public partial class TalkCompose : IAsyncDisposable
{
    private bool _disposedValue;
    private DotNetObjectReference<TalkCompose>? _dotNetObjectReference;
    private IJSObjectReference? _emojiModule;
    private IJSObjectReference? _module;

    /// <summary>
    /// The message ID, if this is for a reply.
    /// </summary>
    [Parameter] public string? MessageId { get; set; }

    /// <summary>
    /// Raised when a message is entered.
    /// </summary>
    [Parameter] public EventCallback<ReplyRequest> Post { get; set; }

    /// <summary>
    /// The topic ID.
    /// </summary>
    [Parameter] public string? TopicId { get; set; }

    [MemberNotNullWhen(true, nameof(TopicId), nameof(NewMessage))]
    private bool CanPost => !string.IsNullOrEmpty(TopicId)
        && !string.IsNullOrWhiteSpace(NewMessage);

    private string EmojiButtonId { get; set; } = Guid.NewGuid().ToHtmlId();

    private List<GifCategory> GifCategories { get; set; } = new();

    private List<string>? GifSuggestions { get; set; }

    private List<GifInfo> Gifs { get; set; } = new();

    [Inject] private JSRuntime JSRuntime { get; set; } = default!;

    private string? NewMessage { get; set; }

    private string? NextGifs { get; set; }

    private bool ShowFullEditor { get; set; }

    private string? SearchGifText { get; set; }

    private bool ShowGifSearch { get; set; }

    [Inject] private WikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetObjectReference = DotNetObjectReference.Create(this);
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/Tavenem.Wiki.Blazor.Client/Shared/TalkCompose.razor.js");
            _emojiModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/Tavenem.Wiki.Blazor.Client/tavenem-emoji.js");

            await _module.InvokeVoidAsync(
                "showGifSearch",
                _dotNetObjectReference,
                WikiBlazorClientOptions.TenorAPIKey);
            await _emojiModule.InvokeVoidAsync(
                "initialize",
                _dotNetObjectReference,
                EmojiButtonId);
        }
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
                _dotNetObjectReference?.Dispose();
                if (_emojiModule is not null)
                {
                    await _emojiModule.DisposeAsync();
                }
                if (_module is not null)
                {
                    await _module.DisposeAsync();
                }
            }

            _disposedValue = true;
        }
    }

    /// <summary>
    /// Invoked by javascript interop.
    /// </summary>
    [JSInvokable]
    public void AppendGifSearch(GifSearchResults results)
    {
        NextGifs = results.Next;
        if (results.Gifs is not null)
        {
            Gifs.AddRange(results.Gifs);
        }
    }

    /// <summary>
    /// Invoked by javascript interop.
    /// </summary>
    [JSInvokable]
    public void GetGifSuggestions(List<string> suggestions)
        => GifSuggestions = suggestions.Count > 0 ? suggestions : null;

    /// <summary>
    /// Invoked by javascript interop.
    /// </summary>
    [JSInvokable]
    public void GetGifCategories(List<GifCategory> categories) => GifCategories = categories
        .Where(x => !string.IsNullOrEmpty(x.SearchTerm)
            && !string.IsNullOrEmpty(x.Image))
        .ToList();

    /// <summary>
    /// Invoked by javascript interop.
    /// </summary>
    [JSInvokable]
    public void PopulateGifSearch(GifSearchResults results)
    {
        NextGifs = results.Next;
        Gifs = results.Gifs?.ToList() ?? new();
    }

    /// <summary>
    /// Invoked by javascript interop.
    /// </summary>
    [JSInvokable]
    public void PostEmoji(string? emoji)
    {
        if (string.IsNullOrEmpty(emoji))
        {
            return;
        }

        NewMessage ??= string.Empty;
        NewMessage += emoji;
    }

    /// <summary>
    /// Invoked by javascript interop.
    /// </summary>
    [JSInvokable]
    public void PostGif(GifInfo gif)
    {
        if (string.IsNullOrEmpty(gif?.Url))
        {
            return;
        }

        NewMessage ??= string.Empty;
        NewMessage += $"[![{gif.Title}]({gif.Url})]({gif.Url})";
    }

    private async Task<IEnumerable<KeyValuePair<string, object>>> GetGifSearchSuggestionsAsync(string input)
    {
        if (_module is null)
        {
            return Enumerable.Empty<KeyValuePair<string, object>>();
        }

        var results = await _module.InvokeAsync<IEnumerable<string>>(
            "getGifAutocomplete",
            WikiBlazorClientOptions.TenorAPIKey,
            input);
        return results.Select(x => new KeyValuePair<string, object>(x, x));
    }

    private async Task OnGetMoreGifsAsync()
    {
        if (_module is null
            || string.IsNullOrWhiteSpace(SearchGifText))
        {
            return;
        }

        var query = SearchGifText.Trim();
        await _module.InvokeVoidAsync(
            "searchGif",
            _dotNetObjectReference,
            WikiBlazorClientOptions.TenorAPIKey,
            query,
            NextGifs);
    }

    private async Task OnGifSearchAsync(string query)
    {
        if (_module is null
            || string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        await _module.InvokeVoidAsync(
            "searchGif",
            _dotNetObjectReference,
            WikiBlazorClientOptions.TenorAPIKey,
            query.Trim());
    }

    private async Task OnPostAsync()
    {
        if (CanPost)
        {
            await Post.InvokeAsync(new ReplyRequest(TopicId, NewMessage, MessageId));
            NewMessage = null;
        }
    }

    private async Task OnReactAsync(string emoji)
    {
        if (CanPost)
        {
            await Post.InvokeAsync(new ReplyRequest(TopicId, emoji, MessageId));
        }
    }

    private async Task OnSetSearchGifTextAsync()
    {
        if (_module is null
            || string.IsNullOrWhiteSpace(SearchGifText))
        {
            return;
        }

        var query = SearchGifText.Trim();
        await _module.InvokeVoidAsync(
            "searchGif",
            _dotNetObjectReference,
            WikiBlazorClientOptions.TenorAPIKey,
            query);
    }

    private async Task OnPostGifAsync(GifInfo gif)
    {
        ShowGifSearch = false;

        if (_module is null)
        {
            return;
        }

        await _module.InvokeVoidAsync(
            "shareGif",
            WikiBlazorClientOptions.TenorAPIKey,
            gif.Id);
    }
}