using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Exceptions;
using Tavenem.Wiki.Blazor.Models;
using Tavenem.Wiki.Blazor.Services.Search;
using Tavenem.Wiki.Models;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Server.Controllers;

/// <summary>
/// The built-in wiki controller.
/// </summary>
[Area("Wiki")]
[Authorize(Policy = "WikiPolicy")]
[AllowAnonymous]
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

    /// <summary>
    /// Retrieve a wiki archive for the given <paramref name="domain"/>, or the entire wiki.
    /// </summary>
    /// <param name="domain">
    /// The domain to be archived; or <see langword="null"/> if the entire wiki is to be archived.
    /// </param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing an <see cref="Wiki.Archive"/> when successful.
    /// </returns>
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

    /// <summary>
    /// Retrieve a given wiki category.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="Wiki.Category"/> when successful.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Retrieve the current wiki user.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="Wiki.WikiUser"/> when successful.
    /// </returns>
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

    /// <summary>
    /// Perform an edit of a wiki page.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/>.
    /// </returns>
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

    /// <summary>
    /// Gets edit information for a wiki page.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="Page"/> when successful.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(Page), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Gets a wiki group page.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="GroupPage"/> when successful.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(GroupPage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Page), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Gets revision information for a wiki page.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="PagedRevisionInfo"/> when successful.
    /// </returns>
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

    /// <summary>
    /// Gets the HTML which would be produced for content containing wiki syntax.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing HTML text when successful.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Html([FromBody] PreviewRequest request)
    {
        var result = await _dataManager.RenderHtmlAsync(User, request);
        if (string.IsNullOrEmpty(result))
        {
            return NoContent();
        }
        return Ok(result);
    }

    /// <summary>
    /// Gets a wiki page.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="Page"/> when successful.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(Page), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Gets a special list of wiki links.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="PagedList{T}"/> of <see
    /// cref="LinkInfo"/> when successful.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(PagedList<LinkInfo>), StatusCodes.Status200OK)]
    public Task<PagedList<LinkInfo>> List([FromBody] SpecialListRequest request)
        => _dataManager.GetListAsync(request);

    /// <summary>
    /// Gets the preview HTML which would be produced for content containing wiki syntax.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing HTML text when successful.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Preview([FromBody] PreviewRequest request)
    {
        var result = await _dataManager.RenderPreviewAsync(User, request);
        if (string.IsNullOrEmpty(result))
        {
            return NoContent();
        }
        return Ok(result);
    }

    /// <summary>
    /// Gets the preview HTML for a given wiki link.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing HTML text when successful.
    /// </returns>
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

    /// <summary>
    /// Restores the given wiki archive.
    /// </summary>
    /// <param name="archive">The archive to restore.</param>
    /// <returns>
    /// An <see cref="IActionResult"/>.
    /// </returns>
    [HttpPost]
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

    /// <summary>
    /// Performs a search of the wiki.
    /// </summary>
    /// <param name="searchClient">An <see cref="ISearchClient"/> (from dependency injection).</param>
    /// <param name="request">The <see cref="SearchRequest"/>.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="SearchResult"/> when successful.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    public Task<SearchResult> Search(
        [FromServices] ISearchClient searchClient,
        [FromBody] SearchRequest request)
        => _dataManager.SearchAsync(searchClient, User, request);

    /// <summary>
    /// Gets search suggestions for a given input string.
    /// </summary>
    /// <param name="searchClient">An <see cref="ISearchClient"/> (from dependency injection).</param>
    /// <param name="input">The search input.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="List{T}"/> of strings when successful.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public Task<List<string>> SearchSuggest(
        [FromServices] ISearchClient searchClient,
        [FromQuery] string? input = null)
        => _dataManager.GetSearchSuggestionsAsync(searchClient, User, input);

    /// <summary>
    /// Gets a wiki talk page.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="List{T}"/> of <see
    /// cref="MessageResponse"/> objects when successful.
    /// </returns>
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

    /// <summary>
    /// Posts a message to a wiki talk page.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the new <see cref="List{T}"/> of <see
    /// cref="MessageResponse"/> objects when successful.
    /// </returns>
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

    /// <summary>
    /// Fetches a list of wiki pages which satisfy the given request.
    /// </summary>
    /// <param name="request">A <see cref="TitleRequest"/> instance.</param>
    /// <returns>A <see cref="PagedList{T}"/> of <see cref="LinkInfo"/> instances.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PagedList<LinkInfo>), StatusCodes.Status200OK)]
    public Task<PagedList<LinkInfo>> Title([FromBody] TitleRequest request)
        => _dataManager.GetTitleAsync(request);

    /// <summary>
    /// Uploads a file to the wiki.
    /// </summary>
    /// <param name="fileManager">An <see cref="IFileManager"/> (from dependency injection).</param>
    /// <param name="file">An <see cref="IFormFile"/> to upload.</param>
    /// <param name="options">An <see cref="UploadRequest"/> serialized as JSON (optional).</param>
    /// <returns>
    /// An <see cref="IActionResult"/>.
    /// </returns>
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
            await using var stream = file.OpenReadStream();
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

    /// <summary>
    /// Gets the current user's upload limit.
    /// </summary>
    /// <returns>
    /// <para>
    /// The current user's upload limit, in bytes.
    /// </para>
    /// <para>
    /// A value of -1 indicates no limit.
    /// </para>
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public Task<int> UploadLimit() => _dataManager.GetUploadLimitAsync(User);

    /// <summary>
    /// Gets the list of pages which link to the given page.
    /// </summary>
    /// <returns>
    /// A <see cref="PagedList{T}"/> of <see cref="LinkInfo"/> objects.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(PagedList<LinkInfo>), StatusCodes.Status200OK)]
    public Task<PagedList<LinkInfo>> WhatLinksHere([FromBody] TitleRequest request)
        => _dataManager.GetWhatLinksHereAsync(request);

    /// <summary>
    /// Gets the wiki links present in content containing wiki syntax.
    /// </summary>
    /// <returns>
    /// A <see cref="List{T}"/> of <see cref="WikiLink"/> objects.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(List<WikiLink>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> WikiLinks([FromBody] PreviewRequest request)
    {
        var result = await _dataManager.GetWikiLinksAsync(User, request);
        if (result is null)
        {
            return NoContent();
        }
        return Ok(result);
    }

    /// <summary>
    /// Gets the wiki user or group whose ID or username corresponds to the given search query.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the <see cref="IWikiOwner"/>, or a 204 (no
    /// content) result.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(IWikiOwner), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> WikiOwner([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return NoContent();
        }
        IWikiOwner? wikiOwner = await _dataManager.GetWikiUserAsync(User, query);
        wikiOwner ??= await _dataManager.GetWikiGroupAsync(User, query);
        return wikiOwner is null
            ? NoContent()
            : Ok(wikiOwner);
    }

    /// <summary>
    /// Gets the <see cref="Wiki.WikiUser"/> whose ID or username corresponds to the given search
    /// query.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the <see cref="Wiki.WikiUser"/>, or a 204 (no
    /// content) result.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(WikiUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> WikiUser([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return NoContent();
        }
        var wikiUserInfo = await _dataManager.GetWikiUserAsync(User, query);
        return wikiUserInfo is null
            ? NoContent()
            : Ok(wikiUserInfo);
    }
}
