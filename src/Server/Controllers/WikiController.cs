using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Client;
using Tavenem.Wiki.Blazor.Client.Services;
using Tavenem.Wiki.Blazor.Exceptions;
using Tavenem.Wiki.Blazor.Models;
using Tavenem.Wiki.Blazor.Server.Authorization;
using Tavenem.Wiki.Models;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Server.Controllers;

/// <summary>
/// The built-in wiki controller.
/// </summary>
[Area("Wiki")]
[AllowAnonymous]
public class WikiController(
    IAuthorizationService authorizationService,
    WikiBlazorOptions wikiBlazorOptions,
    WikiDataService wikiDataService,
    WikiOptions wikiOptions) : Controller
{
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Archive([FromQuery] string? domain = null)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            new PageTitle(null, null, domain),
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            try
            {
                var response = await wikiDataService.GetArchiveAsync(
                    User,
                    domain,
                    wikiBlazorOptions.DomainArchivePermission);
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
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Category(
        [FromQuery] string? title = null,
        [FromQuery] string? @namespace = null,
        [FromQuery] string? domain = null)
    {
        var pageTitle = new PageTitle(title, @namespace, domain);
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            pageTitle,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            try
            {
                var response = await wikiDataService.GetCategoryAsync(User, pageTitle);
                return Ok(response);
            }
            catch (WikiUnauthorizedException)
            {
                return Unauthorized();
            }
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
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
        var wikiUser = await wikiDataService.GetWikiUserAsync(User);
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Edit([FromBody] EditRequest request)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            request.Title,
            WikiEditRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            try
            {
                var success = await wikiDataService.EditAsync(User, request);
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
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EditInfo(
        [FromQuery] string? title = null,
        [FromQuery] string? @namespace = null,
        [FromQuery] string? domain = null)
    {
        var pageTitle = new PageTitle(title, @namespace, domain);
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            pageTitle,
            WikiEditRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            try
            {
                var result = await wikiDataService.GetEditInfoAsync(User, pageTitle);
                return Ok(result);
            }
            catch (WikiUnauthorizedException)
            {
                return Unauthorized();
            }
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Group(
        [FromQuery] string title,
        [FromQuery] long? firstTime = null,
        [FromQuery] long? secondTime = null,
        [FromQuery] bool diff = false)
    {
        var pageTitle = new PageTitle(title, wikiOptions.GroupNamespace);
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            pageTitle,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            if (firstTime.HasValue
                || secondTime.HasValue
                || diff)
            {
                try
                {
                    var result = await wikiDataService.GetItemAsync(
                        User,
                        pageTitle,
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
                var groupPage = await wikiDataService.GetGroupPageAsync(User, title);
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
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> History([FromBody] HistoryRequest request)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            request.Title,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            try
            {
                var result = await wikiDataService.GetHistoryAsync(User, request);
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
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Html([FromBody] PreviewRequest request)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            request.Title,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            var result = await wikiDataService.RenderHtmlAsync(request);
            if (string.IsNullOrEmpty(result))
            {
                return NoContent();
            }
            return Ok(result);
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Item(
        [FromQuery] string? title = null,
        [FromQuery] string? @namespace = null,
        [FromQuery] string? domain = null,
        [FromQuery] bool noRedirect = false,
        [FromQuery] long? firstTime = null,
        [FromQuery] long? secondTime = null,
        [FromQuery] bool diff = false)
    {
        var pageTitle = new PageTitle(title, @namespace, domain);
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            pageTitle,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            try
            {
                var result = await wikiDataService.GetItemAsync(
                    User,
                    pageTitle,
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
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List([FromBody] SpecialListRequest request)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            new PageTitle(),
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            return Ok(await wikiDataService.GetListAsync(request));
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Preview(
        [FromQuery] string? title = null,
        [FromQuery] string? @namespace = null,
        [FromQuery] string? domain = null)
    {
        var pageTitle = new PageTitle(title, @namespace, domain);
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            pageTitle,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            var result = await wikiDataService.GetPreviewAsync(User, pageTitle);
            if (string.IsNullOrEmpty(result))
            {
                return NoContent();
            }
            return Ok(result);
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
    }

    /// <summary>
    /// Gets the preview HTML which would be produced for content containing wiki syntax.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing HTML text when successful.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Preview([FromBody] PreviewRequest request)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            request.Title,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            var result = await wikiDataService.RenderPreviewAsync(request);
            if (string.IsNullOrEmpty(result))
            {
                return NoContent();
            }
            return Ok(result);
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
    }

    /// <summary>
    /// Restores the given wiki archive.
    /// </summary>
    /// <param name="archive">The archive to restore.</param>
    /// <returns>
    /// An <see cref="IActionResult"/>.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RestoreArchive([FromBody] Archive archive)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            new PageTitle(null, null, archive.Pages?.FirstOrDefault()?.Title.Domain),
            WikiEditRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            try
            {
                await wikiDataService.RestoreArchiveAsync(User, archive);
            }
            catch (WikiUnauthorizedException)
            {
                return Unauthorized();
            }
            return Ok();
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
    }

    /// <summary>
    /// Performs a search of the wiki.
    /// </summary>
    /// <param name="request">The <see cref="SearchRequest"/>.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="SearchResult"/> when successful.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            new PageTitle(),
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            return Ok(await wikiDataService.SearchAsync(User, request));
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
    }

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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Talk(
        [FromQuery] string? title = null,
        [FromQuery] string? @namespace = null,
        [FromQuery] string? domain = null,
        [FromQuery] bool noRedirect = false)
    {
        var pageTitle = new PageTitle(title, @namespace, domain);
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            pageTitle,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            try
            {
                var result = await wikiDataService.GetTalkAsync(User, pageTitle, noRedirect);
                return Ok(result);
            }
            catch (WikiUnauthorizedException)
            {
                return Unauthorized();
            }
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Talk(
        [FromForm] string topic,
        [FromForm] string content,
        [FromForm] string? message = null)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return BadRequest();
        }
        if (string.IsNullOrWhiteSpace(content))
        {
            return Ok();
        }
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            new PageTitle(),
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            try
            {
                await wikiDataService.PostTalkAsync(User, new ReplyRequest(topic, content, message));
                return Ok();
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
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
    }

    /// <summary>
    /// Fetches a list of wiki pages which satisfy the given request.
    /// </summary>
    /// <param name="request">A <see cref="TitleRequest"/> instance.</param>
    /// <returns>A <see cref="PagedList{T}"/> of <see cref="LinkInfo"/> instances.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PagedList<LinkInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Title([FromBody] TitleRequest request)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            request.Title,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            return Ok(await wikiDataService.GetTitleAsync(request));
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
    }

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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Upload(
        [FromServices] IFileManager fileManager,
        [FromForm] IFormFile? file = null,
        [FromForm] string? options = null)
    {
        if (file is null)
        {
            return BadRequest("File is required.");
        }

        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            new PageTitle(),
            WikiEditRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            var uploadOptions = string.IsNullOrEmpty(options)
                ? new UploadRequest(new())
                : JsonSerializer.Deserialize(options, WikiBlazorJsonSerializerContext.Default.UploadRequest)
                    ?? new(new());

            try
            {
                await using var stream = file.OpenReadStream();
                _ = await wikiDataService.UploadAsync(
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
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
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
    /// <remarks>
    /// If the current user is not authenticated, this will return 0.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<int> UploadLimit()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return 0;
        }
        return await wikiDataService.GetUploadLimitAsync(User);
    }

    /// <summary>
    /// Gets a wiki user page.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a <see cref="UserPage"/> when successful.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(UserPage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Page), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UserPage(
        [FromQuery] string title,
        [FromQuery] long? firstTime = null,
        [FromQuery] long? secondTime = null,
        [FromQuery] bool diff = false)
    {
        var pageTitle = new PageTitle(title, wikiOptions.UserNamespace);
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            pageTitle,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            if (firstTime.HasValue
                || secondTime.HasValue
                || diff)
            {
                try
                {
                    var result = await wikiDataService.GetItemAsync(
                        User,
                        pageTitle,
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
                var userPage = await wikiDataService.GetUserPageAsync(User, title);
                return Ok(userPage);
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
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
    }

    /// <summary>
    /// Gets the list of pages which link to the given page.
    /// </summary>
    /// <returns>
    /// A <see cref="PagedList{T}"/> of <see cref="LinkInfo"/> objects.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(PagedList<LinkInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> WhatLinksHere([FromBody] TitleRequest request)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            request.Title,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            return Ok(await wikiDataService.GetWhatLinksHereAsync(request));
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
    }

    /// <summary>
    /// Gets the wiki links present in content containing wiki syntax.
    /// </summary>
    /// <returns>
    /// A <see cref="List{T}"/> of <see cref="WikiLink"/> objects.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(List<WikiLink>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> WikiLinks([FromBody] PreviewRequest request)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            request.Title,
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            var result = await wikiDataService.GetWikiLinksAsync(request);
            if (result is null)
            {
                return NoContent();
            }
            return Ok(result);
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> WikiOwner([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return NoContent();
        }
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            new PageTitle(),
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            IWikiOwner? wikiOwner = await wikiDataService.GetWikiUserAsync(User, query);
            wikiOwner ??= await wikiDataService.GetWikiGroupAsync(query);
            return wikiOwner is null
                ? NoContent()
                : Ok(wikiOwner);
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> WikiUser([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return NoContent();
        }
        var authorizeResult = await authorizationService.AuthorizeAsync(
            User,
            new PageTitle(),
            WikiDefaultRequirement.Instance);
        if (authorizeResult.Succeeded)
        {
            var wikiUserInfo = await wikiDataService.GetWikiUserAsync(User, query);
            return wikiUserInfo is null
                ? NoContent()
                : Ok(wikiUserInfo);
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }
        else
        {
            return Challenge();
        }
    }
}
