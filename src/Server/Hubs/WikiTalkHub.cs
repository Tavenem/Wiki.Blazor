using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Server.Controllers;
using Tavenem.Wiki.Blazor.SignalR;

namespace Tavenem.Wiki.Blazor.Server.Hubs;

/// <summary>
/// A SignalR hub for sending wiki discussion messages.
/// </summary>
public class WikiTalkHub : Hub<IWikiTalkClient>, IWikiTalkHub
{
    private readonly IDataStore _dataStore;
    private readonly IWikiGroupManager _groupManager;
    private readonly IWikiUserManager _userManager;
    private readonly WikiOptions _wikiOptions;

    /// <summary>
    /// <para>
    /// Initializes a new instance of <see cref="WikiTalkHub"/>.
    /// </para>
    /// <para>
    /// Note: this class is expected to be used in a <c>MapHub{T}</c> call, not instantiated
    /// directly.
    /// </para>
    /// </summary>
    public WikiTalkHub(
        IDataStore dataStore,
        IWikiGroupManager groupManager,
        IWikiUserManager userManager,
        WikiOptions wikiOptions)
    {
        _dataStore = dataStore;
        _groupManager = groupManager;
        _userManager = userManager;
        _wikiOptions = wikiOptions;
    }

    /// <summary>
    /// Begin listening for messages sent to the given topic.
    /// </summary>
    /// <param name="topicId">A topic ID.</param>
    public async Task JoinTopic(string topicId)
    {
        var user = await _userManager.GetUserAsync(Context.User).ConfigureAwait(false);
        var permission = await GetTopicPermissionAsync(topicId, user).ConfigureAwait(false);
        if (permission)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, topicId).ConfigureAwait(false);
        }
        else
        {
            throw new HubException("You do not have permission to view this topic.");
        }
    }

    /// <summary>
    /// Stop listening for messages sent to the given topic.
    /// </summary>
    /// <param name="topicId">A topic ID.</param>
    public Task LeaveTopic(string topicId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, topicId);

    /// <summary>
    /// Notify clients who are viewing the relevant topic about a new message, and save the
    /// message to the persistent data source.
    /// </summary>
    /// <param name="reply">
    /// <para>
    /// The message that has been sent.
    /// </para>
    /// <para>
    /// Note: messages with empty content are neither saved to the data source, nor forwarded to
    /// clients. Messages with missing topic IDs are also ignored.
    /// </para>
    /// </param>
    [Authorize]
    public async Task Send(ReplyRequest reply)
    {
        if (string.IsNullOrWhiteSpace(reply.TopicId)
            || string.IsNullOrWhiteSpace(reply.Markdown))
        {
            return;
        }

        var user = await _userManager.GetUserAsync(Context.User).ConfigureAwait(false);
        if (user?.IsDeleted != false
            || user.IsDisabled)
        {
            throw new HubException("You do not have permission to reply to this topic.");
        }

        var permission = await GetTopicPermissionAsync(reply.TopicId, user).ConfigureAwait(false);
        if (!permission)
        {
            throw new HubException("You do not have permission to reply to this topic.");
        }

        var message = await Message
            .ReplyAsync(
                _wikiOptions,
                _dataStore,
                reply.TopicId,
                user.Id,
                user.IsWikiAdmin,
                user.DisplayName ?? user.Id,
                reply.Markdown,
                reply.MessageId)
            .ConfigureAwait(false);
        var html = string.Empty;
        var preview = false;
        if (message.WikiLinks.Count == 1)
        {
            var link = message.WikiLinks.First();
            if (!link.IsCategory
                && !link.IsTalk
                && !link.Missing
                && !string.IsNullOrEmpty(link.WikiNamespace))
            {
                var article = await Article.GetArticleAsync(
                    _wikiOptions,
                    _dataStore,
                    link.Title,
                    link.WikiNamespace)
                    .ConfigureAwait(false);
                if (article?.IsDeleted == false)
                {
                    preview = true;
                    var previewHtml = article.Preview;
                    var domainStr = string.IsNullOrEmpty(article.Domain)
                        ? string.Empty
                        : string.Format(WikiDataManager.PreviewDomainTemplate, article.Domain);
                    var namespaceStr = article.WikiNamespace == _wikiOptions.DefaultNamespace
                        ? string.Empty
                        : string.Format(WikiDataManager.PreviewNamespaceTemplate, article.WikiNamespace);
                    html = string.Format(
                        WikiDataManager.PreviewTemplate,
                        domainStr,
                        namespaceStr,
                        article.Title,
                        previewHtml);
                }
            }
        }
        if (!preview)
        {
            html = message.Html;
        }

        if (!string.IsNullOrWhiteSpace(html))
        {
            var senderPage = await Article.GetArticleAsync(
                _wikiOptions,
                _dataStore,
                user.Id,
                _wikiOptions.UserNamespace)
                .ConfigureAwait(false);

            await Clients
                .Group(reply.TopicId)
                .Receive(new MessageResponse(message, html, true, senderPage is not null))
                .ConfigureAwait(false);
        }
    }

    private async ValueTask<bool> GetTopicPermissionAsync(string topicId, IWikiUser? user)
    {
        if (string.IsNullOrWhiteSpace(topicId))
        {
            return false;
        }

        var result = await _dataStore.GetWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            topicId,
            user);
        return result.Permission.HasFlag(WikiPermission.Read);
    }
}
