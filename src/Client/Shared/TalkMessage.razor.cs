using Microsoft.AspNetCore.Components;
using Tavenem.Wiki.Blazor.Client.Internal.Models;
using Tavenem.Wiki.Blazor.SignalR;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// A message on a talk page.
/// </summary>
public partial class TalkMessage
{
    /// <summary>
    /// Whether the chat is currently connected and the user may post.
    /// </summary>
    [Parameter] public bool CanPost { get; set; }

    /// <summary>
    /// The displayed talk message.
    /// </summary>
    [Parameter] public TalkMessageModel? Message { get; set; }

    /// <summary>
    /// Raised when a reply is posted.
    /// </summary>
    [Parameter] public EventCallback<ReplyRequest> Post { get; set; }

    /// <summary>
    /// The user's timezone offset from UTC.
    /// </summary>
    [Parameter] public TimeSpan TimezoneOffset { get; set; }

    private bool IsReply { get; set; }

    private bool ShowReply { get; set; }

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnInitialized()
        => IsReply = !string.IsNullOrEmpty(Message?.Message.ReplyMessageId);

    private Task OnPost(ReplyRequest reply) => Post.InvokeAsync(reply);

    private async Task OnReactionAsync(string emoji)
    {
        if (Message is not null)
        {
            await Post.InvokeAsync(new(
                Message.Message.TopicId,
                emoji,
                Message.Message.Id));
        }
    }
}