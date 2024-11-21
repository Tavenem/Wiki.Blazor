using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Internal.Models;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The talk page.
/// </summary>
public partial class Talk
{
    /// <summary>
    /// The topic ID.
    /// </summary>
    [Parameter] public string? TopicId { get; set; }

    private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    [MemberNotNullWhen(true, nameof(TopicId))]
    private bool CanPost { get; set; }

    [MemberNotNullWhen(true, nameof(TopicId))]
    private bool CanTalk { get; set; }

    [Inject, NotNull] private IServiceProvider? ServiceProvider { get; set; }

    private List<TalkMessageModel> TalkMessages { get; set; } = [];

    [Inject, NotNull] private WikiBlazorOptions? WikiBlazorClientOptions { get; set; }

    [Inject, NotNull] private ClientWikiDataService? WikiDataService { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        TalkMessages.Clear();

        CanTalk = !string.IsNullOrEmpty(TopicId)
            && !string.IsNullOrEmpty(WikiBlazorClientOptions.WikiServerApiRoute);
        if (!CanTalk)
        {
            return;
        }

        AuthenticationStateProvider = ServiceProvider.GetService<AuthenticationStateProvider>();
        var state = AuthenticationStateProvider is null
            ? null
            : await AuthenticationStateProvider.GetAuthenticationStateAsync();
        CanPost = state?.User.Identity?.IsAuthenticated == true;

        var messages = await WikiDataService.GetTalkAsync(WikiState.GetCurrentPageTitle());
        if (messages is null)
        {
            return;
        }
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

                    targetMessage!.Reactions ??= [];
                    if (!targetMessage.Reactions.TryGetValue(emoji, out var value))
                    {
                        value = [];
                        targetMessage.Reactions[emoji] = value;
                    }

                    value.Add(message);
                }
                else
                {
                    (targetMessage!.Replies ??= []).Add(new(message));
                }
            }
            else
            {
                TalkMessages.Add(new(message));
            }
        }

        TalkMessages.Sort((x, y) => x.Message.TimestampTicks.CompareTo(y.Message.TimestampTicks));
        foreach (var message in TalkMessages)
        {
            message.Replies?.Sort((x, y) => x.Message.TimestampTicks.CompareTo(y.Message.TimestampTicks));
        }
    }

    private static TalkMessageModel? FindMessage(List<TalkMessageModel> list, string id)
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
}