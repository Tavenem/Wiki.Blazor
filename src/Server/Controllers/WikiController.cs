using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Models;
using Tavenem.Wiki.Blazor.Server.Hubs;
using Tavenem.Wiki.Blazor.Services.Search;
using Tavenem.Wiki.Blazor.SignalR;
using Tavenem.Wiki.MarkdownExtensions.Transclusions;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Server.Controllers;

/// <summary>
/// The built-in wiki controller.
/// </summary>
[Area("Wiki")]
public class WikiController : Controller
{
    private readonly IDataStore _dataStore;
    private readonly ILogger _logger;
    private readonly IWikiGroupManager _groupManager;
    private readonly IWikiUserManager _userManager;
    private readonly WikiOptions _wikiOptions;

    public WikiController(
        IDataStore dataStore,
        IWikiGroupManager groupManager,
        ILoggerFactory loggerFactory,
        IWikiUserManager userManager,
        WikiOptions wikiOptions)
    {
        _dataStore = dataStore;
        _groupManager = groupManager;
        _logger = loggerFactory.CreateLogger("Wiki");
        _userManager = userManager;
        _wikiOptions = wikiOptions;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CategoryInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Category([FromQuery] string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return BadRequest();
        }

        var wikiUser = User is null
            ? null
            : await _userManager.GetUserAsync(User);

        var response = await _dataStore.GetCategoryAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            title,
            wikiUser);
        if (!response.Permission.HasFlag(WikiPermission.Read))
        {
            return Unauthorized();
        }
        return Ok(response);
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(IWikiUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CurrentUser()
    {
        if (User is null)
        {
            return Unauthorized();
        }
        var wikiUser = await _userManager.GetUserAsync(User);
        if (wikiUser is null
            || wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            return NotFound();
        }
        return Ok(wikiUser);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Edit([FromBody] EditRequest request)
    {
        if (string.Equals(
            request.WikiNamespace,
            _wikiOptions.FileNamespace,
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }
        if (User is null)
        {
            return Unauthorized();
        }
        var wikiUser = await _userManager.GetUserAsync(User);
        if (wikiUser is null
            || wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            return Unauthorized();
        }

        var result = await _dataStore.GetWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            request.Title,
            request.WikiNamespace,
            wikiUser,
            true);
        if (!result.Permission.HasFlag(WikiPermission.Write))
        {
            return Unauthorized();
        }
        if (result.Item is null
            && !result.Permission.HasFlag(WikiPermission.Create))
        {
            return Unauthorized();
        }

        var ownerId = request.OwnerSelf
            ? wikiUser.Id
            : request.Owner;

        if (!result.Permission.HasFlag(WikiPermission.SetOwner)
            && ownerId != result.Item?.Owner)
        {
            return Unauthorized();
        }

        var intendedOwner = await _userManager.FindByIdAsync(ownerId);

        List<string>? allAllowedEditors = null;
        if (request.EditorSelf)
        {
            allAllowedEditors = new List<string>();
        }
        else if (request.AllowedEditors is not null)
        {
            foreach (var id in request.AllowedEditors)
            {
                var editor = await _userManager.FindByIdAsync(id);
                if (editor?.IsDisabled == false
                    && !editor.IsDisabled)
                {
                    (allAllowedEditors ??= new()).Add(editor.Id);
                }
            }
        }

        List<string>? allAllowedEditorGroups = null;
        if (!request.EditorSelf
            && request.AllowedEditorGroups is not null)
        {
            foreach (var id in request.AllowedEditorGroups)
            {
                var editor = await _groupManager.FindByIdAsync(id);
                if (editor is not null)
                {
                    (allAllowedEditorGroups ??= new()).Add(editor.Id);
                }
            }
        }

        List<string>? allAllowedViewers = null;
        if (request.EditorSelf)
        {
            allAllowedViewers = new List<string>();
        }
        else if (request.AllowedViewers is not null)
        {
            foreach (var id in request.AllowedViewers)
            {
                var editor = await _userManager.FindByIdAsync(id);
                if (editor?.IsDisabled == false
                    && !editor.IsDisabled)
                {
                    (allAllowedViewers ??= new()).Add(editor.Id);
                }
            }
        }

        List<string>? allAllowedViewerGroups = null;
        if (!request.EditorSelf
            && request.AllowedViewerGroups is not null)
        {
            foreach (var id in request.AllowedViewerGroups)
            {
                var editor = await _groupManager.FindByIdAsync(id);
                if (editor is not null)
                {
                    (allAllowedViewerGroups ??= new()).Add(editor.Id);
                }
            }
        }

        var success = await _dataStore.AddOrReviseWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            wikiUser,
            request.Title,
            request.Markdown,
            request.RevisionComment,
            request.WikiNamespace,
            request.IsDeleted,
            intendedOwner?.Id,
            allAllowedEditors,
            allAllowedViewers,
            allAllowedEditorGroups,
            allAllowedViewerGroups);
        if (!success)
        {
            return BadRequest("Article could not be created. You may not have the appropriate permissions.");
        }

        if (request.LeaveRedirect
            && result.Item is not null
            && (!string.Equals(result.Item.Title, request.Title, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(result.Item.WikiNamespace, request.WikiNamespace, StringComparison.OrdinalIgnoreCase)))
        {
            var redirectSuccess = await _dataStore.AddOrReviseWikiItemAsync(
                _wikiOptions,
                _userManager,
                _groupManager,
                wikiUser,
                result.Item.Title,
                $$$"""{{redirect|{{{Article.GetFullTitle(_wikiOptions, request.Title ?? result.Item.Title, request.WikiNamespace ?? result.Item.WikiNamespace)}}}}}""",
                request.RevisionComment,
                result.Item.WikiNamespace,
                false,
                intendedOwner?.Id,
                allAllowedEditors,
                allAllowedViewers,
                allAllowedEditorGroups,
                allAllowedViewerGroups);
            if (!redirectSuccess)
            {
                return Ok("A redirect could not be created automatically, but your revision was a success.");
            }
        }

        return Ok();
    }

    [HttpGet]
    [ProducesResponseType(typeof(WikiEditInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditInfo(
        [FromQuery] string? title = null,
        [FromQuery] string? wikiNamespace = null,
        [FromQuery] bool noRedirect = false)
    {
        if (string.IsNullOrEmpty(title)
            && !string.IsNullOrEmpty(wikiNamespace)
            && !string.Equals(wikiNamespace, _wikiOptions.DefaultNamespace, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        var wikiUser = User is null
            ? null
            : await _userManager.GetUserAsync(User);

        var result = await _dataStore.GetWikiItemForEditingAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            title,
            wikiNamespace,
            wikiUser,
            noRedirect);
        if ((result.Permission & WikiPermission.ReadWrite) != WikiPermission.ReadWrite)
        {
            return Unauthorized();
        }
        if (result.Item is null
            && !result.Permission.HasFlag(WikiPermission.Create))
        {
            return Unauthorized();
        }

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(GroupPageInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(WikiItemInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Group(
        [FromQuery] string title,
        [FromQuery] bool requestedDiffCurrent = false,
        [FromQuery] bool requestedDiffPrevious = false,
        [FromQuery] long? requestedDiffTimestamp = null,
        [FromQuery] long? requestedTimestamp = null)
    {
        if (string.IsNullOrEmpty(title))
        {
            return BadRequest();
        }

        if (requestedDiffCurrent
            || requestedDiffPrevious
            || requestedDiffTimestamp.HasValue
            || requestedTimestamp.HasValue)
        {
            return await Item(
                title,
                _wikiOptions.CategoryNamespace,
                true,
                requestedDiffCurrent,
                requestedDiffPrevious,
                requestedDiffTimestamp,
                requestedTimestamp);
        }

        var wikiUser = User is null
            ? null
            : await _userManager.GetUserAsync(User);

        var result = await _dataStore.GetGroupPageAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            title,
            wikiUser);
        if (result is null)
        {
            return NotFound();
        }
        if (!result.Permission.HasFlag(WikiPermission.Read))
        {
            return Unauthorized();
        }

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PagedRevisionInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> History([FromBody] HistoryRequest request)
    {
        if (string.IsNullOrEmpty(request.Title)
            && !string.IsNullOrEmpty(request.WikiNamespace)
            && !string.Equals(request.WikiNamespace, _wikiOptions.DefaultNamespace, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        var wikiUser = User is null
            ? null
            : await _userManager.GetUserAsync(User);

        var result = await _dataStore.GetHistoryAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            request,
            wikiUser);
        if (result is null)
        {
            return NotFound();
        }
        if (!result.Permission.HasFlag(WikiPermission.Read))
        {
            return Unauthorized();
        }

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(WikiItemInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Item(
        [FromQuery] string? title = null,
        [FromQuery] string? wikiNamespace = null,
        [FromQuery] bool noRedirect = false,
        [FromQuery] bool requestedDiffCurrent = false,
        [FromQuery] bool requestedDiffPrevious = false,
        [FromQuery] long? requestedDiffTimestamp = null,
        [FromQuery] long? requestedTimestamp = null)
    {
        if (string.IsNullOrEmpty(title)
            && !string.IsNullOrEmpty(wikiNamespace)
            && !string.Equals(wikiNamespace, _wikiOptions.DefaultNamespace, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        var wikiUser = User is null
            ? null
            : await _userManager.GetUserAsync(User);

        WikiItemInfo? result;
        if (requestedDiffCurrent
            && requestedTimestamp.HasValue)
        {
            result = await _dataStore.GetWikiItemDiffWithCurrentAsync(
                _wikiOptions,
                _userManager,
                _groupManager,
                requestedTimestamp.Value,
                title,
                wikiNamespace,
                wikiUser);
        }
        else if (requestedDiffPrevious)
        {
            result = await _dataStore.GetWikiItemDiffWithPreviousAsync(
                _wikiOptions,
                _userManager,
                _groupManager,
                requestedTimestamp,
                title,
                wikiNamespace,
                wikiUser);
        }
        else if (requestedDiffTimestamp.HasValue)
        {
            result = requestedTimestamp.HasValue
                ? await _dataStore.GetWikiItemDiffAsync(
                    _wikiOptions,
                    _userManager,
                    _groupManager,
                    requestedTimestamp.Value,
                    requestedDiffTimestamp.Value,
                    title,
                    wikiNamespace,
                    wikiUser)
                : await _dataStore.GetWikiItemDiffWithCurrentAsync(
                    _wikiOptions,
                    _userManager,
                    _groupManager,
                    requestedDiffTimestamp.Value,
                    title,
                    wikiNamespace,
                    wikiUser);
        }
        else if (requestedTimestamp.HasValue)
        {
            result = await _dataStore.GetWikiItemAtTimeAsync(
                _wikiOptions,
                _userManager,
                _groupManager,
                requestedTimestamp.Value,
                title,
                wikiNamespace,
                wikiUser);
        }
        else
        {
            result = await _dataStore.GetWikiItemAsync(
                _wikiOptions,
                _userManager,
                _groupManager,
                title,
                wikiNamespace,
                wikiUser,
                noRedirect);
        }
        if (!result.Permission.HasFlag(WikiPermission.Read))
        {
            return Unauthorized();
        }

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromBody] SpecialListRequest request)
        => Ok(new ListResponse(await _dataStore.GetSpecialListAsync(_wikiOptions, request)));

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Preview([FromBody] PreviewRequest request)
    {
        if (User is null)
        {
            return Unauthorized();
        }
        var wikiUser = await _userManager.GetUserAsync(User);
        if (wikiUser is null
            || wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            return Unauthorized();
        }

        var fullTitle = Article.GetFullTitle(
            _wikiOptions,
            request.Title ?? "Example",
            request.WikiNamespace ?? _wikiOptions.DefaultNamespace);

        return Ok(MarkdownItem.RenderHtml(
            _wikiOptions,
            _dataStore,
            await TransclusionParser.TranscludeAsync(
                _wikiOptions,
                _dataStore,
                request.Title,
                fullTitle,
                request.Content)));
    }

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PreviewLink([FromQuery] string link)
    {
        var (wikiNamespace, title, isTalk, _) = Article.GetTitleParts(_wikiOptions, link);
        if (isTalk)
        {
            return NoContent();
        }

        var wikiUser = User is null
            ? null
            : await _userManager.GetUserAsync(User);

        var result = await _dataStore.GetWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            title,
            wikiNamespace,
            wikiUser);
        if (result.Item?.IsDeleted != false
            || !result.Permission.HasFlag(WikiPermission.Read))
        {
            return NoContent();
        }
        return Ok(result.Item.Preview);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromServices] ISearchClient searchClient,
        [FromBody] SearchRequest request)
    {
        if (string.IsNullOrEmpty(request.Query))
        {
            return NoContent();
        }

        string? ownerQuery = null;
        if (!string.IsNullOrEmpty(request.Owner))
        {
            var owners = request
                .Owner
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ownerIds = new List<string>();
            foreach (var name in owners)
            {
                var excluded = name.Length > 0 && name[0] == '!';
                var ownerId = excluded ? name[1..] : name;

                IWikiOwner? foundOwner = await _userManager.FindByIdAsync(ownerId);
                foundOwner ??= await _groupManager.FindByIdAsync(ownerId);
                if (foundOwner is not null)
                {
                    ownerIds.Add(excluded ? $"!{foundOwner.Id}" : foundOwner.Id);
                }
            }
            if (ownerIds.Count == 0)
            {
                return NoContent();
            }
            ownerQuery = string.Join(';', ownerIds);
        }

        string? singleSearchNamespace = null;
        string? namespaceQuery = null;
        if (!string.IsNullOrEmpty(request.WikiNamespace))
        {
            var namespaces = request
                .WikiNamespace
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var includedCount = 0;
            string? singleNamespace = null;
            var searchNamespaces = new List<string>();
            foreach (var name in namespaces)
            {
                var excluded = name.Length > 0 && name[0] == '!';
                var namespaceName = excluded
                    ? name[1..].ToWikiTitleCase()
                    : name.ToWikiTitleCase();
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    searchNamespaces.Add(excluded ? $"!{namespaceName}" : namespaceName);
                    if (!excluded)
                    {
                        singleNamespace = namespaceName;
                        includedCount++;
                    }
                }
            }
            singleSearchNamespace = includedCount == 1
                ? singleNamespace
                : null;
            if (searchNamespaces.Count > 0)
            {
                namespaceQuery = string.Join(';', searchNamespaces);
            }
        }

        var query = request.Query.Trim();
        var original = query;
        query = query.Trim('"');
        var (queryNamespace, title, _, queryIsDefault) = Article.GetTitleParts(_wikiOptions, query);
        if (queryIsDefault && singleSearchNamespace is not null)
        {
            queryNamespace = singleSearchNamespace;
        }

        var exactMatch = await _dataStore.GetWikiItemAsync(_wikiOptions, title, queryNamespace);
        if (exactMatch?.IsDeleted == true)
        {
            exactMatch = null;
        }

        var wikiUser = await _userManager.GetUserAsync(User);
        var result = await searchClient.SearchAsync(new SearchRequest
        {
            Descending = request.Descending,
            Owner = ownerQuery,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Query = query,
            Sort = request.Sort,
            WikiNamespace = namespaceQuery,
        }, wikiUser);

        return Ok(new SearchResponse(
            request.Descending,
            original,
            new PagedListDTO<SearchHit>(result
                .SearchHits
                .Select(x => new SearchHit(x.Title, x.WikiNamespace, x.FullTitle, x.Excerpt)),
                    result.SearchHits.PageNumber,
                    result.SearchHits.PageSize,
                    result.SearchHits.TotalCount),
            request.Sort,
            ownerQuery,
            namespaceQuery,
            exactMatch));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<List<string>> SearchSuggest([FromQuery] string? input = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new();
        }
        var (wikiNamespace, title, isTalk, defaultNamespace) = Article.GetTitleParts(_wikiOptions, input);
        if (string.IsNullOrEmpty(title))
        {
            return new();
        }

        IReadOnlyList<Article> items;
        if (defaultNamespace)
        {
            items = await _dataStore
                .Query<Article>()
                .Where(x => !x.IsDeleted
                    && x.Title.StartsWith(title, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToListAsync();
            if (items.Count == 0)
            {
                items = await _dataStore
                    .Query<Category>()
                    .Where(x => !x.IsDeleted
                        && x.Title.StartsWith(title, StringComparison.OrdinalIgnoreCase))
                    .Take(10)
                    .ToListAsync();
            }
            if (items.Count == 0)
            {
                items = await _dataStore
                    .Query<WikiFile>()
                    .Where(x => !x.IsDeleted
                        && x.Title.StartsWith(title, StringComparison.OrdinalIgnoreCase))
                    .Take(10)
                    .ToListAsync();
            }
        }
        else if (wikiNamespace == _wikiOptions.CategoryNamespace)
        {
            items = await _dataStore
                .Query<Category>()
                .Where(x => !x.IsDeleted
                    && x.Title.StartsWith(title, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToListAsync();
        }
        else if (wikiNamespace == _wikiOptions.FileNamespace)
        {
            items = await _dataStore
                .Query<WikiFile>()
                .Where(x => !x.IsDeleted
                    && x.Title.StartsWith(title, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToListAsync();
        }
        else
        {
            items = await _dataStore
                .Query<Article>()
                .Where(x => !x.IsDeleted
                    && x.Title.StartsWith(title, StringComparison.OrdinalIgnoreCase)
                    && x.WikiNamespace == wikiNamespace)
                .Take(10)
                .ToListAsync();
        }

        return items
            .Select(x => Article.GetFullTitle(_wikiOptions, x.Title, x.WikiNamespace))
            .ToList();
    }

    [HttpGet]
    [ProducesResponseType(typeof(TalkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Talk(
        [FromQuery] string? title = null,
        [FromQuery] string? wikiNamespace = null,
        [FromQuery] bool noRedirect = false)
    {
        if (string.IsNullOrEmpty(title)
            && !string.IsNullOrEmpty(wikiNamespace)
            && !string.Equals(wikiNamespace, _wikiOptions.DefaultNamespace, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        var wikiUser = User is null
            ? null
            : await _userManager.GetUserAsync(User);

        var result = await _dataStore.GetWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            title,
            wikiNamespace,
            wikiUser,
            noRedirect);
        if (result.Item?.IsDeleted != false)
        {
            return NotFound();
        }
        if (!result.Permission.HasFlag(WikiPermission.Read))
        {
            return Unauthorized();
        }

        var messages = await _dataStore
            .Query<Message>()
            .Where(x => x.TopicId == result.Item.Id)
            .ToListAsync();
        var responses = new List<MessageResponse>();
        var senders = new Dictionary<string, bool>();
        var senderPages = new Dictionary<string, bool>();
        foreach (var message in messages)
        {
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
                        link.WikiNamespace);
                    if (article?.IsDeleted == false)
                    {
                        preview = true;
                        var namespaceStr = article.WikiNamespace == _wikiOptions.DefaultNamespace
                            ? string.Empty
                            : string.Format(WikiTalkHub.PreviewNamespaceTemplate, article.WikiNamespace);
                        html = HtmlEncoder.Default.Encode(string.Format(
                            WikiTalkHub.PreviewTemplate,
                            namespaceStr,
                            article.Title,
                            article.Preview));
                    }
                }
            }
            if (!preview)
            {
                html = HtmlEncoder.Default.Encode(message.Html);
            }
            IWikiUser? replyUser = null;
            if (!senders.TryGetValue(message.SenderId, out var senderExists))
            {
                replyUser = await _userManager.FindByIdAsync(message.SenderId);
                senderExists = replyUser?.IsDeleted == false;
                senders.Add(message.SenderId, senderExists);
            }
            if (!senderPages.TryGetValue(message.SenderId, out var senderPageExists))
            {
                if (!senderExists)
                {
                    senderPageExists = false;
                }
                else
                {
                    replyUser ??= await _userManager.FindByIdAsync(message.SenderId);
                    senderPageExists = replyUser?.IsDeleted == false
                        && await Article.GetArticleAsync(
                            _wikiOptions,
                            _dataStore,
                            replyUser.Id,
                            _wikiOptions.UserNamespace) is not null;
                }
                senderPages.Add(message.SenderId, senderPageExists);
            }
            responses.Add(new(
                message,
                html,
                senderExists,
                senderPageExists));
        }

        return Ok(new TalkResponse(
            responses,
            result.Item.Id));
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(
        [FromServices] IFileManager fileManager,
        [FromForm] IFormFile file,
        [FromForm] string options)
    {
        if (file is null)
        {
            return BadRequest("File is required.");
        }

        var uploadOptions = JsonSerializer.Deserialize(options, WikiBlazorJsonSerializerContext.Default.UploadRequest);
        if (uploadOptions is null
            || string.IsNullOrEmpty(uploadOptions.Title))
        {
            return BadRequest("A title is required.");
        }
        if (uploadOptions.Title.Contains(':'))
        {
            return BadRequest("Files may not have namespaces.");
        }
        if (file.Length > _wikiOptions.MaxFileSize)
        {
            return BadRequest($"File size exceeds {_wikiOptions.MaxFileSizeString}.");
        }
        if (User is null)
        {
            return Unauthorized();
        }
        var wikiUser = await _userManager.GetUserAsync(User);
        if (wikiUser is null
            || wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            return Unauthorized();
        }
        var limit = await _groupManager.UserMaxUploadLimit(wikiUser);
        if (limit == 0)
        {
            return Unauthorized();
        }
        if (file.Length > limit)
        {
            return BadRequest("The size of this file exceeds your allotted upload limit.");
        }
        var freeSpace = await fileManager.GetFreeSpaceAsync(wikiUser);
        if (freeSpace >= 0 && file.Length > freeSpace)
        {
            return BadRequest("The size of this file exceeds your remaining upload space.");
        }

        var result = await _dataStore.GetWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            uploadOptions.Title,
            _wikiOptions.FileNamespace,
            wikiUser,
            true);
        if (!result.Permission.HasFlag(WikiPermission.Write))
        {
            return Unauthorized();
        }
        if (result.Item is null
            && !result.Permission.HasFlag(WikiPermission.Create))
        {
            return Unauthorized();
        }

        var ownerId = uploadOptions.OwnerSelf
            || string.IsNullOrEmpty(uploadOptions.Owner)
            ? wikiUser.Id
            : uploadOptions.Owner;

        if (!result.Permission.HasFlag(WikiPermission.SetOwner)
            && ownerId != result.Item?.Owner)
        {
            return Unauthorized();
        }

        var intendedOwner = await _userManager.FindByIdAsync(ownerId);
        if (intendedOwner is null)
        {
            return BadRequest(error: "No such owner found.");
        }

        if (result.Item is not null && !uploadOptions.OverwriteConfirmed)
        {
            return Conflict();
        }

        var fileName = file.FileName;
        string? storagePath;
        try
        {
            storagePath = await fileManager.SaveFileAsync(file.OpenReadStream(), fileName, intendedOwner.Id);
        }
        catch (Exception ex)
        {
            _logger.Log(
                LogLevel.Error,
                ex,
                "Exception during file upload for file with name {FileName}.",
                fileName);
            return StatusCode(500);
        }
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return StatusCode(500);
        }

        WikiFile? wikiFile = null;
        if (result.Item is WikiFile wF)
        {
            wikiFile = wF;
            try
            {
                await fileManager.DeleteFileAsync(wikiFile.FilePath);
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Exception during file delete for file with path {Path} during overwrite.",
                    wikiFile.FilePath);
            }
        }

        List<string>? allAllowedEditors = null;
        if (uploadOptions.EditorSelf)
        {
            allAllowedEditors = new List<string>();
        }
        else if (uploadOptions.AllowedEditors is not null)
        {
            foreach (var id in uploadOptions.AllowedEditors)
            {
                var editor = await _userManager.FindByIdAsync(id);
                if (editor?.IsDisabled == false
                    && !editor.IsDisabled)
                {
                    (allAllowedEditors ??= new()).Add(editor.Id);
                }
            }
        }

        List<string>? allAllowedEditorGroups = null;
        if (!uploadOptions.EditorSelf
            && uploadOptions.AllowedEditorGroups is not null)
        {
            foreach (var id in uploadOptions.AllowedEditorGroups)
            {
                var editor = await _groupManager.FindByIdAsync(id);
                if (editor is not null)
                {
                    (allAllowedEditorGroups ??= new()).Add(editor.Id);
                }
            }
        }

        List<string>? allAllowedViewers = null;
        if (uploadOptions.EditorSelf)
        {
            allAllowedViewers = new List<string>();
        }
        else if (uploadOptions.AllowedViewers is not null)
        {
            foreach (var id in uploadOptions.AllowedViewers)
            {
                var editor = await _userManager.FindByIdAsync(id);
                if (editor?.IsDisabled == false
                    && !editor.IsDisabled)
                {
                    (allAllowedViewers ??= new()).Add(editor.Id);
                }
            }
        }

        List<string>? allAllowedViewerGroups = null;
        if (!uploadOptions.EditorSelf
            && uploadOptions.AllowedViewerGroups is not null)
        {
            foreach (var id in uploadOptions.AllowedViewerGroups)
            {
                var editor = await _groupManager.FindByIdAsync(id);
                if (editor is not null)
                {
                    (allAllowedViewerGroups ??= new()).Add(editor.Id);
                }
            }
        }

        if (wikiFile is null)
        {
            try
            {
                var newArticle = await WikiFile.NewAsync(
                    _wikiOptions,
                    _dataStore,
                    uploadOptions.Title,
                    wikiUser.Id,
                    storagePath,
                    (int)file.Length,
                    file.ContentType,
                    uploadOptions.Markdown,
                    uploadOptions.RevisionComment,
                    intendedOwner.Id,
                    allAllowedEditors,
                    allAllowedViewers,
                    allAllowedEditorGroups,
                    allAllowedViewerGroups);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "User with ID {UserId} failed to upload a new file with title {Title} of size {Length}.",
                    wikiUser.Id,
                    uploadOptions.Title,
                    file.Length);
                return StatusCode(500);
            }
        }

        var titleCase = uploadOptions.Title.ToWikiTitleCase();
        var newTitle = string.Equals(titleCase, wikiFile.Title, StringComparison.CurrentCulture)
            ? null
            : titleCase;
        try
        {
            await wikiFile.ReviseAsync(
                _wikiOptions,
                _dataStore,
                wikiUser.Id,
                newTitle,
                storagePath,
                (int)file.Length,
                file.ContentType,
                uploadOptions.Markdown,
                uploadOptions.RevisionComment,
                false,
                intendedOwner.Id,
                allAllowedEditors,
                allAllowedViewers,
                allAllowedEditorGroups,
                allAllowedViewerGroups);
        }
        catch (Exception ex)
        {
            _logger.Log(
                LogLevel.Error,
                ex,
                "User with ID {UserId} failed to upload a new file for wiki item with ID {Id}, title {Title}, and new size {Length}.",
                wikiUser.Id,
                wikiFile.Id,
                uploadOptions.Title,
                file.Length);
            return StatusCode(500);
        }
        return Ok();
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadLimit()
    {
        if (User is null)
        {
            return Unauthorized();
        }
        var wikiUser = await _userManager.GetUserAsync(User);
        if (wikiUser is null
            || wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            return NotFound();
        }
        return Ok(await _groupManager.UserMaxUploadLimit(wikiUser));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WhatLinksHere([FromBody] WhatLinksHereRequest request)
    {
        var result = await _dataStore.GetWhatLinksHereAsync(
            _wikiOptions,
            request);
        if (result is null)
        {
            return NotFound();
        }
        return Ok(new ListResponse(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(WikiUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WikiUser([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return NotFound();
        }
        var wikiUser = await _userManager.FindByIdAsync(query);
        wikiUser ??= await _userManager.FindByNameAsync(query);
        wikiUser ??= await _userManager.FindByNameAsync(query);
        if (wikiUser is null)
        {
            return NotFound();
        }

        var requestingUser = await _userManager.GetUserAsync(User);
        if (requestingUser?.IsWikiAdmin == true)
        {
            return Ok(wikiUser);
        }

        if (wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            return NotFound();
        }

        return Ok(new WikiUser
        {
            DisplayName = wikiUser.DisplayName,
            Id = wikiUser.Id,
            IsWikiAdmin = wikiUser.IsWikiAdmin,
        });
    }
}
