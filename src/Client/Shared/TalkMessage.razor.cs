using Microsoft.AspNetCore.Components;
using Tavenem.Wiki.Blazor.Client.Internal.Models;

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

    /// <summary>
    /// The topic ID.
    /// </summary>
    [Parameter] public string? TopicId { get; set; }

    private bool IsReply { get; set; }

    private bool ShowReply { get; set; }

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnInitialized()
        => IsReply = !string.IsNullOrEmpty(Message?.Message.ReplyMessageId);

    private static string GetUserLinkClass(MessageResponse response) => response.SenderIsAdmin
        ? $"wiki-username wiki-username-link wiki-username-admin wiki-username-{response.SenderId}"
        : $"wiki-username wiki-username-link wiki-username-{response.SenderId}";

    private Task OnPost(ReplyRequest reply) => Post.InvokeAsync(reply);

    private async Task OnReactionAsync(string emoji)
    {
        if (Message is not null
            && !string.IsNullOrEmpty(TopicId))
        {
            await Post.InvokeAsync(new(
                TopicId,
                emoji,
                Message.Message.Id));
        }
    }
}