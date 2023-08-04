using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Exceptions;
using Tavenem.Wiki.Blazor.Models;
using Tavenem.Wiki.Blazor.Services.Search;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Server.Controllers;

/// <summary>
/// The built-in wiki controller.
/// </summary>
[Area("Wiki")]
public class WikiController(
    IDataStore dataStore,
    IWikiGroupManager groupManager,
    ILoggerFactory loggerFactory,
    IWikiUserManager userManager,
    WikiBlazorServerOptions wikiBlazorServerOptions,
    WikiOptions wikiOptions) : Controller
{
    private readonly WikiDataManager _dataManager = new(
        dataStore,
        groupManager,
        loggerFactory,
        userManager,
        wikiOptions);

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
                wikiBlazorServerOptions.DomainArchivePermission);
            return new JsonResult(response, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                TypeInfoResolver = WikiArchiveJsonSerializerContext.Default,
            });
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(CategoryInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Category(
        [FromQuery] string? title = null,
        [FromQuery] string? @namespace = null,
        [FromQuery] string? domain = null)
    {
        try
        {
            var response = await _dataManager.GetCategoryAsync(User, new PageTitle(title, @namespace, domain));
            return Ok(response);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(WikiUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return NoContent();
        }
        var wikiUser = await _dataManager.GetWikiUserAsync(User);
        if (wikiUser is null)
        {
            return NoContent();
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EditInfo(
        [FromQuery] string? title = null,
        [FromQuery] string? @namespace = null,
        [FromQuery] string? domain = null)
    {
        try
        {
            var result = await _dataManager.GetEditInfoAsync(User, new PageTitle(title, @namespace, domain));
            return Ok(result);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(GroupPageInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(WikiPageInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Group(
        [FromQuery] string title,
        [FromQuery] long? firstTime = null,
        [FromQuery] long? secondTime = null,
        [FromQuery] bool diff = false)
    {
        if (firstTime.HasValue
            || secondTime.HasValue
            || diff)
        {
            try
            {
                var result = await _dataManager.GetItemAsync(
                    User,
                    new PageTitle(title, wikiOptions.GroupNamespace),
                    true,
                    firstTime is null ? null : new DateTimeOffset(firstTime.Value, TimeSpan.Zero),
                    secondTime is null ? null : new DateTimeOffset(secondTime.Value, TimeSpan.Zero),
                    diff);
                return Ok(result);
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> History([FromBody] HistoryRequest request)
    {
        try
        {
            var result = await _dataManager.GetHistoryAsync(User, request);
            if (result is null)
            {
                return NoContent();
            }
            return Ok(result);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Html([FromBody] PreviewRequest request)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return NoContent();
        }
        var result = await _dataManager.RenderHtmlAsync(User, request);
        if (string.IsNullOrEmpty(result))
        {
            return NoContent();
        }
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(WikiPageInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Item(
        [FromQuery] string? title = null,
        [FromQuery] string? @namespace = null,
        [FromQuery] string? domain = null,
        [FromQuery] bool noRedirect = false,
        [FromQuery] long? firstTime = null,
        [FromQuery] long? secondTime = null,
        [FromQuery] bool diff = false)
    {
        try
        {
            var result = await _dataManager.GetItemAsync(
                User,
                new PageTitle(title, @namespace, domain),
                noRedirect,
                firstTime is null ? null : new DateTimeOffset(firstTime.Value, TimeSpan.Zero),
                secondTime is null ? null : new DateTimeOffset(secondTime.Value, TimeSpan.Zero),
                diff);
            return Ok(result);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(PagedList<LinkInfo>), StatusCodes.Status200OK)]
    public Task<PagedList<LinkInfo>> List([FromBody] SpecialListRequest request)
        => _dataManager.GetListAsync(request);

    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Preview([FromBody] PreviewRequest request)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return NoContent();
        }
        var result = await _dataManager.RenderPreviewAsync(User, request);
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

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RestoreArchive([FromBody] Archive archive)
    {
        try
        {
            await _dataManager.RestoreArchiveAsync(User, archive);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
        return Ok();
    }

    [HttpPost]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    public Task<SearchResult> Search(
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
    [ProducesResponseType(typeof(List<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Talk(
        [FromQuery] string? title = null,
        [FromQuery] string? @namespace = null,
        [FromQuery] string? domain = null,
        [FromQuery] bool noRedirect = false)
    {
        try
        {
            var result = await _dataManager.GetTalkAsync(User, new PageTitle(title, @namespace, domain), noRedirect);
            return Ok(result);
        }
        catch (WikiUnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(List<MessageResponse>), StatusCodes.Status200OK)]
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

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        [FromServices] IFileManager fileManager,
        [FromForm] IFormFile? file = null,
        [FromForm] string? options = null)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        if (file is null)
        {
            return BadRequest("File is required.");
        }

        var uploadOptions = string.IsNullOrEmpty(options)
            ? new UploadRequest(new())
            : JsonSerializer.Deserialize(options, WikiBlazorJsonSerializerContext.Default.UploadRequest)
                ?? new(new());

        try
        {
            using var stream = file.OpenReadStream();
            _ = await _dataManager.UploadAsync(
                User,
                fileManager,
                uploadOptions,
                stream,
                file.FileName,
                file.ContentType);
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

    [HttpGet]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<int> UploadLimit()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return 0;
        }

        return await _dataManager.GetUploadLimitAsync(User);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PagedList<LinkInfo>), StatusCodes.Status200OK)]
    public Task<PagedList<LinkInfo>> WhatLinksHere([FromBody] WhatLinksHereRequest request)
        => _dataManager.GetWhatLinksHereAsync(request);

    [HttpGet]
    [ProducesResponseType(typeof(WikiUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> WikiUser([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return NoContent();
        }
        var wikiUserInfo = await _dataManager.GetWikiUserAsync(User, query);
        if (wikiUserInfo is null)
        {
            return NoContent();
        }
        return Ok(wikiUserInfo);
    }
}
