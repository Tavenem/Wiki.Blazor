using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Internal.Models;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// A message on a talk page.
/// </summary>
public partial class TalkMessage
{
    /// <summary>
    /// The displayed talk message.
    /// </summary>
    [Parameter] public TalkMessageModel? Message { get; set; }

    /// <summary>
    /// The topic ID.
    /// </summary>
    [Parameter] public string? TopicId { get; set; }

    private bool IsReply { get; set; }

    [MemberNotNullWhen(false, nameof(TopicId))]
    private bool PostDisabled => string.IsNullOrEmpty(TopicId)
        || string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute);

    [Inject, NotNull] private WikiBlazorOptions? WikiBlazorClientOptions { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    protected override void OnInitialized()
        => IsReply = !string.IsNullOrEmpty(Message?.Message.ReplyMessageId);

    private static string GetUserLinkClass(MessageResponse response) => response.SenderIsAdmin
        ? $"wiki-username wiki-username-link wiki-username-admin wiki-username-{response.SenderId}"
        : $"wiki-username wiki-username-link wiki-username-{response.SenderId}";
}