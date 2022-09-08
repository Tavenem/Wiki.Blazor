using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.SignalR;

namespace Tavenem.Wiki.Blazor.Server.Hubs;

/// <summary>
/// A SignalR hub for sending wiki discussion messages.
/// </summary>
public class WikiTalkHub : Hub<IWikiTalkClient>, IWikiTalkHub
{
    internal const string PreviewNamespaceTemplate = "<span class=\"wiki-main-heading-namespace\">{0}</span><span class=\"wiki-main-heading-namespace-separator\">:</span>";
    internal const string PreviewTemplate = "<div class=\"wiki compact preview\"><div><main class=\"wiki-content\" role=\"main\"><div class=\"wiki-heading\" role=\"heading\"><h1 class=\"wiki-main-heading\">{0}<span class=\"wiki-main-heading-title\">{1}</span></h1></div><div class=\"wiki-body\"><div class=\"wiki-parser-output\">{2}</div></div></main></div></div>";

    private readonly IDataStore _dataStore;
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
        IWikiUserManager userManager,
        WikiOptions wikiOptions)
    {
        _dataStore = dataStore;
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
                    var namespaceStr = article.WikiNamespace == _wikiOptions.DefaultNamespace
                        ? string.Empty
                        : string.Format(PreviewNamespaceTemplate, article.WikiNamespace);
                    html = string.Format(PreviewTemplate, namespaceStr, article.Title, previewHtml);
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
        if (string.IsNullOrWhiteSpace(topicId)
            || string.IsNullOrEmpty(topicId))
        {
            return false;
        }

        var article = await _dataStore.GetItemAsync<Article>(topicId).ConfigureAwait(false);
        if (article is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(article.Owner)
            || article.Owner == user?.Id)
        {
            return true;
        }

        if (article.AllowedViewers is null)
        {
            return true;
        }
        else if (user is null)
        {
            return false;
        }
        else if (article.AllowedViewers.Contains(user.Id))
        {
            return true;
        }
        else if (user.Groups is null)
        {
            return false;
        }
        else
        {
            return user.Groups.Contains(article.Owner)
                || article.AllowedViewers?.Intersect(user.Groups).Any() != false;
        }
    }
}
