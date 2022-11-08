using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Exceptions;
using Tavenem.Wiki.Blazor.Models;
using Tavenem.Wiki.Blazor.Services.Search;
using Tavenem.Wiki.Blazor.SignalR;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Server.Controllers;

/// <summary>
/// The built-in wiki controller.
/// </summary>
[Area("Wiki")]
public class WikiController : Controller
{
    private readonly WikiDataManager _dataManager;
    private readonly WikiBlazorServerOptions _wikiBlazorServerOptions;
    private readonly WikiOptions _wikiOptions;

    public WikiController(
        IDataStore dataStore,
        IWikiGroupManager groupManager,
        ILoggerFactory loggerFactory,
        IWikiUserManager userManager,
        WikiBlazorServerOptions wikiBlazorServerOptions,
        WikiOptions wikiOptions)
    {
        _dataManager = new WikiDataManager(
            dataStore,
            groupManager,
            loggerFactory,
            userManager,
            wikiOptions);
        _wikiBlazorServerOptions = wikiBlazorServerOptions;
        _wikiOptions = wikiOptions;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Archive), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Archive([FromQuery] string? domain = null)
    {
        try
        {
            var response = await _dataManager.GetArchiveAsync(
                User,
                domain,
                _wikiBlazorServerOptions.DomainArchivePermission);
            return Ok(response);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(CategoryInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Category([FromQuery] string title, [FromQuery] string? domain = null)
    {
        if (string.IsNullOrEmpty(title))
        {
            return BadRequest();
        }

        try
        {
            var response = await _dataManager.GetCategoryAsync(User, title, domain);
            if (response is null)
            {
                return NotFound();
            }
            return Ok(response);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(WikiUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CurrentUser()
    {
        var wikiUser = await _dataManager.GetWikiUserAsync(User);
        if (wikiUser is null)
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
        try
        {
            var success = await _dataManager.EditAsync(User, request);
            if (success)
            {
                return Ok();
            }
            return Ok("A redirect could not be created automatically, but your revision was a success.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(WikiEditInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EditInfo(
        [FromQuery] string? title = null,
        [FromQuery] string? wikiNamespace = null,
        [FromQuery] string? domain = null,
        [FromQuery] bool noRedirect = false)
    {
        try
        {
            var result = await _dataManager.GetEditInfoAsync(User, title, wikiNamespace, domain, noRedirect);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
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
            try
            {
                var result = await _dataManager.GetItemAsync(
                    User,
                    title,
                    _wikiOptions.GroupNamespace,
                    null,
                    true,
                    requestedDiffCurrent,
                    requestedDiffPrevious,
                    requestedDiffTimestamp,
                    requestedTimestamp);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (WikiUnauthorizedException)
            {
                return Unauthorized();
            }
        }

        try
        {
            var groupPage = await _dataManager.GetGroupPageAsync(User, title);
            return Ok(groupPage);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(PagedRevisionInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> History([FromBody] HistoryRequest request)
    {
        try
        {
            var result = await _dataManager.GetHistoryAsync(User, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(WikiItemInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Item(
        [FromQuery] string? title = null,
        [FromQuery] string? wikiNamespace = null,
        [FromQuery] string? domain = null,
        [FromQuery] bool noRedirect = false,
        [FromQuery] bool requestedDiffCurrent = false,
        [FromQuery] bool requestedDiffPrevious = false,
        [FromQuery] long? requestedDiffTimestamp = null,
        [FromQuery] long? requestedTimestamp = null)
    {
        try
        {
            var result = await _dataManager.GetItemAsync(
                User,
                title,
                wikiNamespace,
                domain,
                noRedirect,
                requestedDiffCurrent,
                requestedDiffPrevious,
                requestedDiffTimestamp,
                requestedTimestamp);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ListResponse), StatusCodes.Status200OK)]
    public Task<ListResponse> List([FromBody] SpecialListRequest request)
        => _dataManager.GetListAsync(request);

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Preview([FromBody] PreviewRequest request)
    {
        var result = await _dataManager.PreviewAsync(User, request);
        if (string.IsNullOrEmpty(result))
        {
            return NoContent();
        }
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PreviewLink([FromQuery] string link)
    {
        var result = await _dataManager.GetPreviewAsync(User, link);
        if (string.IsNullOrEmpty(result))
        {
            return NoContent();
        }
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    public Task<SearchResponse> Search(
        [FromServices] ISearchClient searchClient,
        [FromBody] SearchRequest request)
        => _dataManager.SearchAsync(searchClient, User, request);

    [HttpGet]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public Task<List<string>> SearchSuggest(
        [FromServices] ISearchClient searchClient,
        [FromQuery] string? input = null)
        => _dataManager.GetSearchSuggestionsAsync(searchClient, User, input);

    [HttpGet]
    [ProducesResponseType(typeof(TalkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Talk(
        [FromQuery] string? title = null,
        [FromQuery] string? wikiNamespace = null,
        [FromQuery] string? domain = null,
        [FromQuery] bool noRedirect = false)
    {
        try
        {
            var result = await _dataManager.GetTalkAsync(User, title, wikiNamespace, domain, noRedirect);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(TalkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Talk([FromBody] ReplyRequest reply)
    {
        try
        {
            var result = await _dataManager.PostTalkAsync(User, reply);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        try
        {
            _ = await _dataManager.UploadAsync(User, fileManager, file, uploadOptions);
            return Ok();
        }
        catch (WikiConflictException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public Task<int> UploadLimit()
        => _dataManager.GetUploadLimitAsync(User);

    [HttpPost]
    [ProducesResponseType(typeof(ListResponse), StatusCodes.Status200OK)]
    public Task<ListResponse> WhatLinksHere([FromBody] WhatLinksHereRequest request)
        => _dataManager.GetWhatLinksHereAsync(request);

    [HttpGet]
    [ProducesResponseType(typeof(WikiUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WikiUser([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return NotFound();
        }
        var wikiUserInfo = await _dataManager.GetWikiUserAsync(User, query);
        if (wikiUserInfo is null)
        {
            return NotFound();
        }
        return Ok(wikiUserInfo);
    }
}
