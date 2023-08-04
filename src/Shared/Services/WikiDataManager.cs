using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Exceptions;
using Tavenem.Wiki.Blazor.Models;
using Tavenem.Wiki.Blazor.Services.Search;
using Tavenem.Wiki.MarkdownExtensions.Transclusions;
using Tavenem.Wiki.Models;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor;

/// <summary>
/// Facilitates data operations for the wiki.
/// </summary>
/// <remarks>
/// Constructs a new instance of <see cref="WikiDataManager"/>.
/// </remarks>
public class WikiDataManager(
    IDataStore dataStore,
    IWikiGroupManager groupManager,
    ILoggerFactory loggerFactory,
    IWikiUserManager userManager,
    WikiOptions wikiOptions)
{
    /// <summary>
    /// The format string used for a domain in a preview.
    /// </summary>
    public const string PreviewDomainTemplate = "<span class=\"wiki-main-heading-domain\">{0}</span><span class=\"wiki-main-heading-domain-separator\"></span>";
    /// <summary>
    /// The format string used for a namespace in a preview.
    /// </summary>
    public const string PreviewNamespaceTemplate = "<span class=\"wiki-main-heading-namespace\">{0}</span><span class=\"wiki-main-heading-namespace-separator\"></span>";
    /// <summary>
    /// The format string used for a preview.
    /// </summary>
    public const string PreviewTemplate = "<div class=\"wiki compact preview\"><div><main class=\"wiki-content\" role=\"main\"><div class=\"wiki-heading\" role=\"heading\"><h1 class=\"wiki-main-heading\">{0}{1}<span class=\"wiki-main-heading-title\">{2}</span></h1></div><div class=\"wiki-body\"><div class=\"wiki-parser-output\">{3}</div></div></main></div></div>";
    private readonly ILogger _logger = loggerFactory.CreateLogger("Wiki");

    /// <summary>
    /// Performs the requested edit operation.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="request">
    /// An <see cref="EditRequest"/> instance describing the edit.
    /// </param>
    /// <returns>
    /// <para>
    /// <see langword="false"/> if a redirect failed to be automatically created as a result of a
    /// move/rename operation; otherwise <see langword="true"/>.
    /// </para>
    /// <para>
    /// Note that the edit is successful for both a <see langword="true"/> and <see
    /// langword="false"/> result. Only an exception indicates failure.
    /// </para>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The edit was attempted in the file namespace. The <see cref="UploadAsync(ClaimsPrincipal?,
    /// IFileManager, IFormFile, UploadRequest)"/> endpoint should be used instead.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The content could not be updated (usually for permission reasons).
    /// </exception>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have permission to make the requested edit.
    /// </exception>
    public async Task<bool> EditAsync(ClaimsPrincipal? user, EditRequest request)
    {
        if (string.Equals(
            request.Title.Namespace,
            wikiOptions.FileNamespace,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Cannot edit content in the File namespace. Use the Upload endpoint instead.",
                nameof(request));
        }

        if (user is null)
        {
            throw new WikiUnauthorizedException();
        }
        var wikiUser = await userManager.GetUserAsync(user);
        if (wikiUser?.IsDeleted != false
            || wikiUser.IsDisabled)
        {
            throw new WikiUnauthorizedException();
        }

        var result = await dataStore.GetWikiPageAsync(
            wikiOptions,
            userManager,
            groupManager,
            request.OriginalTitle ?? request.Title,
            wikiUser,
            true);
        if (!result.Permission.HasFlag(WikiPermission.Write))
        {
            throw new WikiUnauthorizedException();
        }
        if (result.Page?.Exists != true
            && !result.Permission.HasFlag(WikiPermission.Create))
        {
            throw new WikiUnauthorizedException();
        }

        var ownerId = request.OwnerSelf
            ? wikiUser.Id
            : request.Owner;

        if (!result.Permission.HasFlag(WikiPermission.SetOwner)
            && ownerId != result.Page?.Owner)
        {
            throw new WikiUnauthorizedException();
        }

        var intendedOwner = await userManager.FindByIdAsync(ownerId);

        List<string>? allAllowedEditors = null;
        if (request.EditorSelf)
        {
            allAllowedEditors = new List<string>();
        }
        else if (request.AllowedEditors is not null)
        {
            foreach (var id in request.AllowedEditors)
            {
                var editor = await userManager.FindByIdAsync(id);
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
                var editor = await groupManager.FindByIdAsync(id);
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
                var editor = await userManager.FindByIdAsync(id);
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
                var editor = await groupManager.FindByIdAsync(id);
                if (editor is not null)
                {
                    (allAllowedViewerGroups ??= new()).Add(editor.Id);
                }
            }
        }

        var success = await dataStore.AddOrReviseWikiPageAsync(
            wikiOptions,
            userManager,
            groupManager,
            wikiUser,
            request.Title,
            request.Markdown,
            request.RevisionComment,
            request.IsDeleted,
            intendedOwner?.Id,
            allAllowedEditors,
            allAllowedViewers,
            allAllowedEditorGroups,
            allAllowedViewerGroups,
            originalTitle: request.OriginalTitle);
        if (!success)
        {
            throw new InvalidOperationException("Article could not be updated. You may not have the appropriate permissions.");
        }

        if (request.LeaveRedirect
            && result.Page?.Title.Equals(request.Title) == false)
        {
            var redirectSuccess = await dataStore.AddOrReviseWikiPageAsync(
                wikiOptions,
                userManager,
                groupManager,
                wikiUser,
                result.Page.Title,
                null,
                request.RevisionComment,
                false,
                intendedOwner?.Id,
                allAllowedEditors,
                allAllowedViewers,
                allAllowedEditorGroups,
                allAllowedViewerGroups,
                request.Title);
            if (!redirectSuccess)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Retreive an archive of a domain, or the entire wiki.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="domain">
    /// The domain to be archived; or an empty string to archive content with no domain; or <see
    /// langword="null"/> to archive the entire wiki.
    /// </param>
    /// <param name="requiredPermission">
    /// <para>
    /// The minimum permission the user must have for the target <paramref name="domain"/> in order
    /// to create the archive.
    /// </para>
    /// <para>
    /// When creating an archive for content without a domain, or for the entire wiki, this
    /// parameter is ignored.
    /// </para>
    /// <para>
    /// Since it would be prohibitive to check individual pages' permission, this method only
    /// requires that a user has this level of permission (defaulting to <see
    /// cref="WikiPermission.Read"/>) for the target <paramref name="domain"/>. This could represent
    /// a potential security breach, if individual pages within the domain are further restricted.
    /// It is strongly recommended that the ability to create archives is restricted in your client
    /// code in a manner specific to your implementation's use of domains, which guarantees that
    /// only those with the correct permissions can create archives.
    /// </para>
    /// </param>
    /// <returns>An <see cref="Archive"/> object.</returns>
    /// <remarks>
    /// <para>
    /// Since it would be prohibitive to check individual pages' permission, and there is no way to
    /// establish any particular level of permission for all non-domain content, only an admin user
    /// may request an archive of content without a domain, or the entire wiki.
    /// </para>
    /// </remarks>
    /// <exception cref="WikiUnauthorizedException">
    /// <para>
    /// The user does not have <see cref="WikiPermission.Read"/> permission for the given domain.
    /// </para>
    /// <para>
    /// Or, an archive of non-domain content or the entire wiki was requested by the user is not an
    /// admin.
    /// </para>
    /// </exception>
    public async Task<Archive> GetArchiveAsync(
        ClaimsPrincipal? user,
        string? domain = null,
        WikiPermission requiredPermission = WikiPermission.Read)
    {
        var wikiUser = await userManager.GetUserAsync(user)
            ?? throw new WikiUnauthorizedException();
        var hasDomain = !string.IsNullOrEmpty(domain);
        if (hasDomain)
        {
            var domainPermission = WikiPermission.None;
            if (wikiOptions.GetDomainPermission is not null)
            {
                domainPermission = await wikiOptions
                    .GetDomainPermission
                    .Invoke(wikiUser.Id, domain!);
            }
            if (wikiUser.AllowedViewDomains?.Contains(domain!) == true)
            {
                domainPermission |= WikiPermission.Read;
            }

            if ((domainPermission & requiredPermission) != requiredPermission)
            {
                throw new WikiUnauthorizedException();
            }
        }
        else if (!wikiUser.IsWikiAdmin)
        {
            throw new WikiUnauthorizedException();
        }

        return await dataStore.GetWikiArchiveAsync(wikiOptions, domain);
    }

    /// <summary>
    /// Gets information about the category with the given <paramref name="title"/>.
    /// </summary>
    /// <param name="user">The user making the request (if any).</param>
    /// <param name="title">The requested category title.</param>
    /// <param name="domain">The requested category domain (if any).</param>
    /// <returns>
    /// A <see cref="CategoryInfo"/> object.
    /// </returns>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have <see cref="WikiPermission.Read"/> permission for the given category.
    /// </exception>
    public async Task<CategoryInfo> GetCategoryAsync(
        ClaimsPrincipal? user,
        PageTitle title)
    {
        var wikiUser = user is null
            ? null
            : await userManager.GetUserAsync(user);

        var response = await dataStore.GetCategoryAsync(
            wikiOptions,
            userManager,
            groupManager,
            title,
            wikiUser);
        if (!response.Permission.HasFlag(WikiPermission.Read))
        {
            throw new WikiUnauthorizedException();
        }
        return response;
    }

    /// <summary>
    /// Fetches edit info for the given content.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="title">The title of the requested content.</param>
    /// <returns>A <see cref="WikiEditInfo"/> instance.</returns>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have permission to make the requested edit.
    /// </exception>
    public async Task<WikiEditInfo> GetEditInfoAsync(
        ClaimsPrincipal? user,
        PageTitle title)
    {
        var wikiUser = user is null
            ? null
            : await userManager.GetUserAsync(user);

        var result = await dataStore.GetWikiPageForEditingAsync(
            wikiOptions,
            userManager,
            groupManager,
            title,
            wikiUser);
        if ((result.Permission & WikiPermission.ReadWrite) != WikiPermission.ReadWrite)
        {
            throw new WikiUnauthorizedException();
        }
        if (result.Item is null
            && !result.Permission.HasFlag(WikiPermission.Create))
        {
            throw new WikiUnauthorizedException();
        }

        return result;
    }

    /// <summary>
    /// Fetches information about the group page with the given <paramref name="title"/>.
    /// </summary>
    /// <param name="title">
    /// The title of a group page (i.e. the group's <see cref="IIdItem.Id"/>).
    /// </param>
    /// <returns>
    /// A <see cref="GroupPageInfo"/> instance; or <see langword="null"/> if there is no such page.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="title"/> was empty.
    /// </exception>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have permission to view the requested page.
    /// </exception>
    public async Task<GroupPageInfo?> GetGroupPageAsync(ClaimsPrincipal? user, string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException($"{nameof(title)} cannot be empty", nameof(title));
        }

        var wikiUser = user is null
            ? null
            : await userManager.GetUserAsync(user);

        var result = await dataStore.GetGroupPageAsync(
            wikiOptions,
            userManager,
            groupManager,
            title,
            wikiUser);
        if (result is null)
        {
            return null;
        }
        if (!result.Permission.HasFlag(WikiPermission.Read))
        {
            throw new WikiUnauthorizedException();
        }

        return result;
    }

    /// <summary>
    /// Gets revision information for the requested content.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="request">A <see cref="HistoryRequest"/> instance.</param>
    /// <returns>
    /// A <see cref="PagedRevisionInfo"/> instance; or <see langword="null"/> if no such
    /// page exists.
    /// </returns>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have <see cref="WikiPermission.Read"/> permission for the given content.
    /// </exception>
    public async Task<PagedRevisionInfo?> GetHistoryAsync(ClaimsPrincipal? user, HistoryRequest request)
    {
        var wikiUser = user is null
            ? null
            : await userManager.GetUserAsync(user);

        var result = await dataStore.GetHistoryAsync(
            wikiOptions,
            userManager,
            groupManager,
            request,
            wikiUser);
        if (result is null)
        {
            return null;
        }
        if (!result.Permission.HasFlag(WikiPermission.Read))
        {
            throw new WikiUnauthorizedException();
        }

        return result;
    }

    /// <summary>
    /// Fetches information about the given wiki page.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="title">The title of the requested page.</param>
    /// <param name="noRedirect">
    /// Whether to prevent redirects when fetching content.
    /// </param>
    /// <param name="firstTime">
    /// <para>
    /// The first revision time.
    /// </para>
    /// <para>
    /// If <see langword="null"/> the revision at <paramref name="secondTime"/> will be compared
    /// with the previous version.
    /// </para>
    /// <para>
    /// If both are <see langword="null"/> and <paramref name="diff"/> is <see langword="true"/> the
    /// current version of the page will be compared with the previous version.
    /// </para>
    /// <para>
    /// If both are <see langword="null"/> and <paramref name="diff"/> is <see langword="false"/>
    /// the current version of the page is retrieved.
    /// </para>
    /// </param>
    /// <param name="secondTime">
    /// <para>
    /// The second revision time to compare.
    /// </para>
    /// <para>
    /// If <see langword="null"/>, <paramref name="firstTime"/> is not <see langword="null"/>, and
    /// <paramref name="diff"/> is <see langword="true"/> the revision at <paramref
    /// name="firstTime"/> will be compared with the current version.
    /// </para>
    /// <para>
    /// If <see langword="null"/>, <paramref name="firstTime"/> is not <see langword="null"/>, and
    /// <paramref name="diff"/> is <see langword="false"/> the revision at <paramref
    /// name="firstTime"/> will be retrieved.
    /// </para>
    /// <para>
    /// If both are <see langword="null"/> and <paramref name="diff"/> is <see langword="true"/> the
    /// current version of the page will be compared with the previous version.
    /// </para>
    /// <para>
    /// If both are <see langword="null"/> and <paramref name="diff"/> is <see langword="false"/>
    /// the current version of the page is retrieved.
    /// </para>
    /// </param>
    /// <returns>A <see cref="WikiPageInfo"/> instance.</returns>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have <see cref="WikiPermission.Read"/> permission for the given content.
    /// </exception>
    public async Task<WikiPageInfo> GetItemAsync(
        ClaimsPrincipal? user,
        PageTitle title,
        bool noRedirect = false,
        DateTimeOffset? firstTime = null,
        DateTimeOffset? secondTime = null,
        bool diff = false)
    {
        var wikiUser = user is null
            ? null
            : await userManager.GetUserAsync(user);

        WikiPageInfo result;
        if (diff
            || secondTime.HasValue)
        {
            result = await dataStore.GetWikiPageDiffAsync(
                wikiOptions,
                userManager,
                groupManager,
                title,
                firstTime,
                secondTime,
                wikiUser);
        }
        else if (firstTime.HasValue)
        {
            result = await dataStore.GetWikiPageAsync(
                wikiOptions,
                userManager,
                groupManager,
                title,
                wikiUser,
                noRedirect,
                firstTime);
        }
        else
        {
            result = await dataStore.GetWikiPageAsync(
                wikiOptions,
                userManager,
                groupManager,
                title,
                wikiUser,
                noRedirect);
        }
        if (!result.Permission.HasFlag(WikiPermission.Read))
        {
            throw new WikiUnauthorizedException();
        }

        return result;
    }

    /// <summary>
    /// Fetches a special list for the given request.
    /// </summary>
    /// <param name="request">A <see cref="SpecialListRequest"/> instance.</param>
    /// <returns>A <see cref="PagedList{T}"/> of <see cref="LinkInfo"/> instances.</returns>
    public async Task<PagedList<LinkInfo>> GetListAsync(SpecialListRequest request)
        => await dataStore.GetSpecialListAsync(request);

    /// <summary>
    /// Gets the preview content of an article.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="link">The wiki link to preview.</param>
    /// <returns>
    /// The preview content; or <see langword="null"/> if there is no such article, or the given
    /// user does not have permission to view it.
    /// </returns>
    public async Task<string?> GetPreviewAsync(ClaimsPrincipal? user, string link)
    {
        var title = PageTitle.Parse(link);

        var wikiUser = user is null
            ? null
            : await userManager.GetUserAsync(user);

        var result = await dataStore.GetWikiPageAsync(
            wikiOptions,
            userManager,
            groupManager,
            title,
            wikiUser);
        if (result.Page?.Exists != true
            || !result.Permission.HasFlag(WikiPermission.Read))
        {
            return null;
        }
        return result.Page.Preview;
    }

    /// <summary>
    /// Gets a list of search suggestions based on the given input.
    /// </summary>
    /// <param name="searchClient">An <see cref="ISearchClient"/> instance.</param>
    /// <param name="user">The user making the request.</param>
    /// <param name="input">An input string.</param>
    /// <returns>
    /// A <see cref="List{T}"/> of the full titles of wiki articles which match the given input.
    /// </returns>
    public async Task<List<string>> GetSearchSuggestionsAsync(
        ISearchClient searchClient,
        ClaimsPrincipal? user,
        string? input = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new();
        }
        var title = PageTitle.Parse(input);
        if (string.IsNullOrEmpty(title.Title))
        {
            return new();
        }

        var request = new SearchRequest
        {
            Domain = title.Domain,
            PageSize = 10,
            Query = title.Title,
            TitleMatchOnly = true,
            Namespace = title.Namespace,
        };
        var wikiUser = user is null
            ? null
            : await userManager.GetUserAsync(user);
        var result = await searchClient.SearchAsync(request, wikiUser);

        return result
            .SearchHits
            .Select(x => x.Title.ToString())
            .ToList();
    }

    /// <summary>
    /// Get the talk messages for a given page.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="title">The title of the requested content.</param>
    /// <param name="noRedirect">
    /// Whether to prevent redirects when fetching content.
    /// </param>
    /// <returns>
    /// A <see cref="TalkResponse"/> instance.
    /// </returns>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have <see cref="WikiPermission.Read"/> permission for the given content.
    /// </exception>
    public async Task<List<MessageResponse>> GetTalkAsync(
        ClaimsPrincipal? user,
        PageTitle title,
        bool noRedirect = false)
    {
        var wikiUser = user is null
            ? null
            : await userManager.GetUserAsync(user);

        var result = await dataStore.GetWikiPageAsync(
            wikiOptions,
            userManager,
            groupManager,
            title,
            wikiUser,
            noRedirect);
        if (!result.Permission.HasFlag(WikiPermission.Read))
        {
            throw new WikiUnauthorizedException();
        }
        if (result.Page?.Exists != true)
        {
            return new();
        }

        var topic = await Topic.GetTopicAsync(dataStore, result.Page.Title);
        return await GetTopicMessagesAsync(topic);
    }

    /// <summary>
    /// Gets the given <paramref name="user"/>'s upload limit.
    /// </summary>
    /// <returns>
    /// <para>
    /// The given <paramref name="user"/>'s upload limit, in bytes.
    /// </para>
    /// <para>
    /// A value of -1 indicates no limit.
    /// </para>
    /// </returns>
    public async Task<int> GetUploadLimitAsync(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return 0;
        }
        var wikiUser = await userManager.GetUserAsync(user);
        if (wikiUser?.IsDeleted != false
            || wikiUser.IsDisabled)
        {
            return 0;
        }
        return await groupManager.UserMaxUploadLimit(wikiUser);
    }

    /// <summary>
    /// Fetches a list of the pages which link to a given resource.
    /// </summary>
    /// <param name="request">a <see cref="WhatLinksHereRequest"/> instance.</param>
    /// <returns>A <see cref="PagedList{T}"/> of <see cref="LinkInfo"/> instances.</returns>
    public async Task<PagedList<LinkInfo>> GetWhatLinksHereAsync(WhatLinksHereRequest request)
    {
        var result = await dataStore.GetWhatLinksHereAsync(
            wikiOptions,
            request);
        return result
            ?? new(null, 1, request.PageSize, 0);
    }

    /// <summary>
    /// Gets the wiki user associated with the given <paramref name="user"/>.
    /// </summary>
    /// <param name="user">A <see cref="ClaimsPrincipal"/>.</param>
    /// <returns>
    /// An <see cref="IWikiUser"/> instance; or <see langword="null"/> if there is no such user, or
    /// if the given user is deleted or disabled.
    /// </returns>
    public async Task<WikiUser?> GetWikiUserAsync(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return null;
        }
        var wikiUser = await userManager.GetUserAsync(user);
        if (wikiUser?.IsDeleted != false
            || wikiUser.IsDisabled)
        {
            return null;
        }
        return new WikiUser
        {
            AllowedEditPages = wikiUser.AllowedEditPages,
            AllowedViewPages = wikiUser.AllowedViewPages,
            AllowedViewDomains = wikiUser.AllowedViewDomains,
            DisplayName = wikiUser.DisplayName,
            Groups = wikiUser.Groups,
            Id = wikiUser.Id,
            IsWikiAdmin = wikiUser.IsWikiAdmin,
            UploadLimit = wikiUser.UploadLimit,
        };
    }

    /// <summary>
    /// Fetches information about a given wiki user.
    /// </summary>
    /// <param name="query">
    /// A wiki user ID or username.
    /// </param>
    /// <returns>
    /// An <see cref="IWikiUser"/> instance (possibly with a limited set of information, if the
    /// requesting <paramref name="user"/> is not an administrator); or <see langword="null"/> if
    /// there is no such user.
    /// </returns>
    /// <remarks>
    /// When an administrator requests information about a deleted or disabled user, the <see
    /// cref="IWikiUser"/> instance is returned with the relevant properties set. When a
    /// non-administrator requests information about a deleted or disabled user, <see
    /// langword="null"/> is returned.
    /// </remarks>
    public async Task<WikiUserInfo?> GetWikiUserAsync(ClaimsPrincipal? user, string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }
        var wikiUser = await userManager.FindByIdAsync(query);
        wikiUser ??= await userManager.FindByNameAsync(query);
        if (wikiUser is null)
        {
            return null;
        }

        var requestingUser = await userManager.GetUserAsync(user);
        if (requestingUser?.IsDeleted != false
            || requestingUser.IsDisabled)
        {
            return null;
        }

        if (requestingUser.IsWikiAdmin)
        {
            return new WikiUserInfo(
                wikiUser.Id,
                new WikiUser
                {
                    AllowedEditPages = wikiUser.AllowedEditPages,
                    AllowedViewPages = wikiUser.AllowedViewPages,
                    AllowedViewDomains = wikiUser.AllowedViewDomains,
                    DisplayName = wikiUser.DisplayName,
                    Groups = wikiUser.Groups,
                    Id = wikiUser.Id,
                    IsDeleted = wikiUser.IsDeleted,
                    IsDisabled = wikiUser.IsDisabled,
                    IsWikiAdmin = wikiUser.IsWikiAdmin,
                    UploadLimit = wikiUser.UploadLimit,
                });
        }

        if (wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            return null;
        }

        return new WikiUserInfo(
            wikiUser.Id,
            new WikiUser
            {
                DisplayName = wikiUser.DisplayName,
                Id = wikiUser.Id,
                IsWikiAdmin = wikiUser.IsWikiAdmin,
            });
    }

    /// <summary>
    /// Posts a talk message to a given topic.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="reply">A <see cref="ReplyRequest"/> instance.</param>
    /// <returns>
    /// A <see cref="TalkResponse"/> instance containing all the messages for the related page.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The <see cref="ReplyRequest.TopicId"/> was missing.
    /// </exception>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have <see cref="WikiPermission.Read"/> permission for the given topic.
    /// </exception>
    public async Task<List<MessageResponse>> PostTalkAsync(
        ClaimsPrincipal? user,
        ReplyRequest reply)
    {
        if (string.IsNullOrEmpty(reply.TopicId))
        {
            throw new ArgumentException($"The {nameof(ReplyRequest.TopicId)} cannot be empty.",
                nameof(reply));
        }

        var wikiUser = user is null
            ? null
            : await userManager.GetUserAsync(user);
        if (wikiUser?.IsDeleted != false
            || wikiUser.IsDisabled)
        {
            throw new WikiUnauthorizedException();
        }

        var title = new Topic(reply.TopicId, null).GetTitle();
        if (!title.Equals(new PageTitle()))
        {
            var result = await dataStore.GetWikiPageAsync(
                wikiOptions,
                userManager,
                groupManager,
                title,
                wikiUser);
            if (!result.Permission.HasFlag(WikiPermission.Read))
            {
                throw new WikiUnauthorizedException();
            }
            if (result.Page?.Exists != true)
            {
                return new();
            }
        }

        _ = await Message.ReplyAsync(
            wikiOptions,
            dataStore,
            reply.TopicId,
            wikiUser.Id,
            wikiUser.IsWikiAdmin,
            wikiUser.DisplayName ?? wikiUser.Id,
            reply.Markdown,
            reply.MessageId);

        var topic = await dataStore.GetItemAsync<Topic>(reply.TopicId);
        return await GetTopicMessagesAsync(topic);
    }

    /// <summary>
    /// Gets the given content's rendered HTML.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="request">A <see cref="PreviewRequest"/> instance.</param>
    /// <returns>
    /// A <see cref="string"/> containing the HTML; or <see langword="null"/> if there is no such
    /// content, or the user's account is not found, deleted, or disabled.
    /// </returns>
    public async Task<string?> RenderHtmlAsync(ClaimsPrincipal? user, PreviewRequest request)
    {
        if (user is null)
        {
            return null;
        }
        var wikiUser = await userManager.GetUserAsync(user);
        if (wikiUser?.IsDeleted != false
            || wikiUser.IsDisabled)
        {
            return null;
        }

        return MarkdownItem.RenderHtml(
            wikiOptions,
            dataStore,
            await TransclusionParser.TranscludeAsync(
                wikiOptions,
                dataStore,
                request.Title,
                request.Content));
    }

    /// <summary>
    /// Gets a preview of the given content's rendered HTML.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="request">A <see cref="PreviewRequest"/> instance.</param>
    /// <returns>
    /// A <see cref="string"/> containing the preview; or <see langword="null"/> if there is no such
    /// content, or the user's account is not found, deleted, or disabled.
    /// </returns>
    public async Task<string?> RenderPreviewAsync(ClaimsPrincipal? user, PreviewRequest request)
    {
        if (user is null)
        {
            return null;
        }
        var wikiUser = await userManager.GetUserAsync(user);
        if (wikiUser?.IsDeleted != false
            || wikiUser.IsDisabled)
        {
            return null;
        }

        return MarkdownItem.RenderPreview(
            wikiOptions,
            dataStore,
            await TransclusionParser.TranscludeAsync(
                wikiOptions,
                dataStore,
                request.Title,
                request.Content));
    }

    /// <summary>
    /// Restores an <see cref="Archive"/> to the wiki.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="archive">An <see cref="Archive"/> instance.</param>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have appropriate permission to restore all the pages in the archive.
    /// </exception>
    public async Task RestoreArchiveAsync(ClaimsPrincipal? user, Archive archive)
    {
        if (archive.Pages is null
            || archive.Pages.Count == 0)
        {
            return;
        }

        if (user is null)
        {
            throw new WikiUnauthorizedException();
        }
        var wikiUser = await userManager.GetUserAsync(user);
        if (wikiUser?.IsDeleted != false
            || wikiUser.IsDisabled)
        {
            throw new WikiUnauthorizedException();
        }

        var skipPermission = false;
        if (wikiUser.IsWikiAdmin)
        {
            skipPermission = true;
        }
        else if (wikiOptions.UserDomains)
        {
            var domains = archive.Pages.ConvertAll(x => x.Title.Domain);
            if (domains.Count == 1
                && string.CompareOrdinal(domains[0], wikiUser.Id) == 0)
            {
                skipPermission = true;
            }
        }

        if (!skipPermission)
        {
            const WikiPermission RequiredPermissions = WikiPermission.Write
                | WikiPermission.Create
                | WikiPermission.SetPermissions
                | WikiPermission.SetOwner;

            foreach (var page in archive.Pages)
            {
                var permission = await userManager.GetPermissionAsync(
                    wikiOptions,
                    dataStore,
                    groupManager,
                    page,
                    wikiUser);
                if ((permission & RequiredPermissions) != RequiredPermissions)
                {
                    throw new WikiUnauthorizedException();
                }
            }
        }

        await archive.RestoreAsync(dataStore, wikiOptions, wikiUser.Id);
    }

    /// <summary>
    /// Performs a search.
    /// </summary>
    /// <param name="searchClient">An <see cref="ISearchClient"/> instance.</param>
    /// <param name="user">The user making the request.</param>
    /// <param name="request">The search request.</param>
    /// <returns>
    /// A <see cref="SearchResult"/> object.
    /// </returns>
    public async Task<SearchResult> SearchAsync(
        ISearchClient searchClient,
        ClaimsPrincipal? user,
        SearchRequest request)
    {
        if (string.IsNullOrEmpty(request.Query))
        {
            return new SearchResult(
                request.Descending,
                request.Query,
                new PagedList<SearchHit>(null, request.PageNumber, request.PageSize, 0),
                request.Sort,
                request.Owner,
                request.Namespace,
                request.Domain);
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

                IWikiOwner? foundOwner = await userManager.FindByIdAsync(ownerId);
                foundOwner ??= await groupManager.FindByIdAsync(ownerId);
                if (foundOwner is not null)
                {
                    ownerIds.Add(excluded ? $"!{foundOwner.Id}" : foundOwner.Id);
                }
            }
            if (ownerIds.Count == 0)
            {
                return new SearchResult(
                    request.Descending,
                    request.Query,
                    new PagedList<SearchHit>(null, request.PageNumber, request.PageSize, 0),
                    request.Sort,
                    request.Owner,
                    request.Namespace,
                    request.Domain);
            }
            ownerQuery = string.Join(';', ownerIds);
        }

        string? singleSearchNamespace = null;
        string? namespaceQuery = null;
        if (!string.IsNullOrEmpty(request.Namespace))
        {
            var namespaces = request
                .Namespace
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
        var title = PageTitle.Parse(query);
        if (string.IsNullOrEmpty(title.Domain)
            && !string.IsNullOrEmpty(request.Domain))
        {
            title = title.WithDomain(request.Domain);
        }
        if (string.IsNullOrEmpty(title.Namespace)
            && singleSearchNamespace is not null)
        {
            title = title.WithNamespace(singleSearchNamespace);
        }

        var exactMatch = await dataStore.GetWikiPageAsync(wikiOptions, title);
        if (exactMatch?.Exists != true)
        {
            exactMatch = null;
        }

        var wikiUser = user is null
            ? null
            : await userManager.GetUserAsync(user);
        var result = await searchClient.SearchAsync(new SearchRequest
        {
            Descending = request.Descending,
            Owner = ownerQuery,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Query = query,
            Sort = request.Sort,
            Namespace = namespaceQuery,
            Domain = request.Domain,
        }, wikiUser);

        return new SearchResult(
            request.Descending,
            original,
            new PagedList<SearchHit>(
                result
                    .SearchHits
                    .Select(x => new SearchHit(
                        x.Title,
                        x.Excerpt)),
                result.SearchHits.PageNumber,
                result.SearchHits.PageSize,
                result.SearchHits.TotalCount),
            request.Sort,
            ownerQuery,
            namespaceQuery,
            request.Domain,
            exactMatch);
    }

    /// <summary>
    /// Upload a file.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="fileManager">An <see cref="IFileManager"/> instance.</param>
    /// <param name="options">
    /// An <see cref="UploadRequest"/> instance.
    /// </param>
    /// <param name="file">
    /// <para>
    /// A <see cref="Stream"/> containing the file to be uploaded.
    /// </para>
    /// <para>
    /// Or <see langword="null"/> if the file is being deleted.
    /// </para>
    /// </param>
    /// <param name="fileName">
    /// <para>
    /// The name of the file to be uploaded.
    /// </para>
    /// <para>
    /// Or <see langword="null"/> if a file is being deleted.
    /// </para>
    /// </param>
    /// <param name="contentType">
    /// <para>
    /// The MIME type of the file to be uploaded.
    /// </para>
    /// <para>
    /// Or <see langword="null"/> if a file is being deleted.
    /// </para>
    /// </param>
    /// <returns>
    /// <para>
    /// <see langword="false"/> if a previous version of the file failed to be automatically deleted
    /// as a result of an update; otherwise <see langword="true"/>.
    /// </para>
    /// <para>
    /// Note that the upload is successful for both a <see langword="true"/> and <see
    /// langword="false"/> result. Only an exception indicates failure.
    /// </para>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <para>
    /// <paramref name="options"/> specifies a <see cref="UploadRequest.Title"/> with a namespace
    /// other than <see cref="WikiOptions.FileNamespace"/>.
    /// </para>
    /// <para>
    /// Or, the size of <paramref name="file"/> exceeds <see cref="WikiOptions.MaxFileSize"/>, or
    /// the user's own upload limit.
    /// </para>
    /// <para>
    /// Or, the specified <see cref="UploadRequest.Owner"/> does not exist.
    /// </para>
    /// <para>
    /// Or, the specified title is an existing page which is not a file.
    /// </para>
    /// </exception>
    /// <exception cref="WikiUnauthorizedException">
    /// <para>
    /// The user does not have permission to upload files.
    /// </para>
    /// <para>
    /// Or, the user does not have permission to create/edit this particular file.
    /// </para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The file upload operation failed.
    /// </exception>
    /// <exception cref="WikiConflictException">
    /// The specified file already exists, and <see cref="UploadRequest.OverwriteConfirmed"/> was
    /// not set to <see langword="true"/>.
    /// </exception>
    public async Task<bool> UploadAsync(
        ClaimsPrincipal? user,
        IFileManager fileManager,
        UploadRequest options,
        Stream? file,
        string? fileName,
        string? contentType)
    {
        if (!string.IsNullOrEmpty(options.Title.Namespace)
            && string.CompareOrdinal(options.Title.Namespace, wikiOptions.FileNamespace) != 0)
        {
            throw new ArgumentException("Files can only be uploaded to the File namespace (the namespace may be omitted to use the File namespace by default).", nameof(options));
        }
        if (file is not null && string.IsNullOrEmpty(contentType))
        {
            throw new ArgumentException($"{nameof(contentType)} cannot be missing when a {nameof(file)} is provided.", nameof(contentType));
        }
        if (file?.Length > wikiOptions.MaxFileSize)
        {
            throw new ArgumentException($"File size exceeds {wikiOptions.MaxFileSizeString}.", nameof(file));
        }
        if (user is null)
        {
            throw new WikiUnauthorizedException();
        }
        var wikiUser = await userManager.GetUserAsync(user);
        if (wikiUser?.IsDeleted != false
            || wikiUser.IsDisabled)
        {
            throw new WikiUnauthorizedException();
        }
        var limit = await groupManager.UserMaxUploadLimit(wikiUser);
        if (limit == 0)
        {
            throw new WikiUnauthorizedException();
        }
        if (file?.Length > limit)
        {
            throw new ArgumentException("The size of this file exceeds your allotted upload limit.", nameof(file));
        }
        var freeSpace = await fileManager.GetFreeSpaceAsync(wikiUser);
        if (freeSpace >= 0 && file?.Length > freeSpace)
        {
            throw new ArgumentException("The size of this file exceeds your remaining upload space.", nameof(file));
        }

        var ownerId = options.OwnerSelf
            || string.IsNullOrEmpty(options.Owner)
            ? wikiUser.Id
            : options.Owner;

        var intendedOwner = await userManager.FindByIdAsync(ownerId)
            ?? throw new ArgumentException("No such owner found.", nameof(options));

        var title = options.Title.WithNamespace(wikiOptions.FileNamespace);

        var result = await dataStore.GetWikiPageAsync(
            wikiOptions,
            userManager,
            groupManager,
            title,
            wikiUser,
            true);

        if (result.Page?.Exists != true && file is null)
        {
            throw new WikiConflictException("Cannot upload an empty file to a new page.");
        }

        if (result.Page is not null and not WikiFile)
        {
            throw new ArgumentException("The specified title is an existing page which is not a file.", nameof(options));
        }

        if (result.Page?.Exists == true && !options.OverwriteConfirmed)
        {
            throw new WikiConflictException("A file with this title already exists.");
        }

        List<string>? allAllowedEditors = null;
        if (options.EditorSelf)
        {
            allAllowedEditors = new List<string>();
        }
        else if (options.AllowedEditors is not null)
        {
            foreach (var id in options.AllowedEditors)
            {
                var editor = await userManager.FindByIdAsync(id);
                if (editor?.IsDisabled == false
                    && !editor.IsDisabled)
                {
                    (allAllowedEditors ??= new()).Add(editor.Id);
                }
            }
        }

        List<string>? allAllowedEditorGroups = null;
        if (!options.EditorSelf
            && options.AllowedEditorGroups is not null)
        {
            foreach (var id in options.AllowedEditorGroups)
            {
                var editor = await groupManager.FindByIdAsync(id);
                if (editor is not null)
                {
                    (allAllowedEditorGroups ??= new()).Add(editor.Id);
                }
            }
        }

        List<string>? allAllowedViewers = null;
        if (options.EditorSelf)
        {
            allAllowedViewers = new List<string>();
        }
        else if (options.AllowedViewers is not null)
        {
            foreach (var id in options.AllowedViewers)
            {
                var editor = await userManager.FindByIdAsync(id);
                if (editor?.IsDisabled == false
                    && !editor.IsDisabled)
                {
                    (allAllowedViewers ??= new()).Add(editor.Id);
                }
            }
        }

        List<string>? allAllowedViewerGroups = null;
        if (!options.EditorSelf
            && options.AllowedViewerGroups is not null)
        {
            foreach (var id in options.AllowedViewerGroups)
            {
                var editor = await groupManager.FindByIdAsync(id);
                if (editor is not null)
                {
                    (allAllowedViewerGroups ??= new()).Add(editor.Id);
                }
            }
        }

        var hasPermission = CheckEditPermissions(
            wikiOptions,
            result,
            false,
            allAllowedEditors,
            allAllowedViewers,
            allAllowedEditorGroups,
            allAllowedViewerGroups);
        if (!hasPermission)
        {
            throw new WikiUnauthorizedException();
        }

        WikiPageInfo? originalPage = null;
        if (options.OriginalTitle.HasValue
            && !options.OriginalTitle.Equals(title))
        {
            originalPage = await dataStore.GetWikiPageAsync(
                wikiOptions,
                userManager,
                groupManager,
                options.OriginalTitle.Value,
                wikiUser,
                true);

            var hasOriginalPermission = CheckEditPermissions(
                wikiOptions,
                originalPage,
                true,
                allAllowedEditors,
                allAllowedViewers,
                allAllowedEditorGroups,
                allAllowedViewerGroups);
            if (!hasOriginalPermission || originalPage.Page is null)
            {
                return false;
            }
        }

        string? storagePath = null;
        if (file is not null)
        {
            try
            {
                storagePath = await fileManager.SaveFileAsync(file, fileName, intendedOwner.Id);
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Exception during file upload for file with name {FileName}.",
                    fileName);
                throw;
            }
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                throw new InvalidOperationException("File save operation failed.");
            }
        }

        var success = true;
        if (originalPage?.Page is WikiFile originalWikiFile)
        {
            try
            {
                await fileManager.DeleteFileAsync(originalWikiFile.FilePath);
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Exception during file delete for file with path {Path} during rename.",
                    originalWikiFile.FilePath);
                success = false;
            }

            try
            {
                await originalWikiFile.UpdateAsync(
                    wikiOptions,
                    dataStore,
                    wikiUser.Id,
                    originalWikiFile.FilePath,
                    0,
                    originalWikiFile.FileType,
                    null,
                    options.RevisionComment,
                    intendedOwner.Id,
                    allAllowedEditors,
                    allAllowedViewers,
                    allAllowedEditorGroups,
                    allAllowedViewerGroups,
                    options.LeaveRedirect ? title : null);
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "User with ID {UserId} failed to delete a file for wiki item with ID {Id}, title {Title}, and new size {Length}.",
                    wikiUser.Id,
                    originalWikiFile.Id,
                    title,
                    file?.Length ?? 0);
                throw;
            }
        }

        if (result.Page is WikiFile wikiFile)
        {
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
                success = false;
            }
        }
        else
        {
            wikiFile = WikiFile.Empty(title);
        }

        if (file is null || string.IsNullOrEmpty(storagePath))
        {
            try
            {
                await wikiFile.UpdateAsync(
                    wikiOptions,
                    dataStore,
                    wikiUser.Id,
                    wikiFile.FilePath,
                    0,
                    wikiFile.FileType,
                    null,
                    options.RevisionComment,
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
                    "User with ID {UserId} failed to delete wiki file with ID {Id} and title {Title}.",
                    wikiUser.Id,
                    wikiFile.Id,
                    title);
                throw;
            }
        }
        else
        {
            try
            {
                await wikiFile.UpdateAsync(
                    wikiOptions,
                    dataStore,
                    wikiUser.Id,
                    storagePath,
                    (int)file.Length,
                    contentType ?? string.Empty,
                    options.Markdown,
                    options.RevisionComment,
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
                    title,
                    file.Length);
                throw;
            }
        }

        return success;
    }

    private static bool CheckEditPermissions(
        WikiOptions options,
        WikiPageInfo page,
        bool isDeletedOrRenamed = false,
        IEnumerable<string>? allowedEditors = null,
        IEnumerable<string>? allowedViewers = null,
        IEnumerable<string>? allowedEditorGroups = null,
        IEnumerable<string>? allowedViewerGroups = null)
    {
        if (page.Page is null
            || !page.Permission.HasFlag(WikiPermission.Write))
        {
            return false;
        }
        if (!page.Page.Exists)
        {
            if (!page.Permission.HasFlag(WikiPermission.Create))
            {
                return false;
            }
            if (options.ReservedNamespaces.Any(x => string.CompareOrdinal(page.Page.Title.Namespace, x) == 0))
            {
                return false;
            }
        }
        if (!string.IsNullOrEmpty(page.Page.Owner)
            && !page.Permission.HasFlag(WikiPermission.SetOwner))
        {
            return false;
        }

        if (isDeletedOrRenamed
            && !page.Permission.HasFlag(WikiPermission.Delete))
        {
            return false;
        }

        if (!page.Permission.HasFlag(WikiPermission.SetPermissions))
        {
            if (!page.Page.Exists)
            {
                if (allowedEditors is not null
                    || allowedEditorGroups is not null
                    || allowedViewers is not null
                    || allowedViewerGroups is not null)
                {
                    return false;
                }
            }
            else
            {
                if (page.Page.AllowedEditors is null)
                {
                    if (allowedEditors is not null)
                    {
                        return false;
                    }
                }
                else if (allowedEditors is null
                    || !page.Page.AllowedEditors.Order().SequenceEqual(allowedEditors.Order()))
                {
                    return false;
                }

                if (page.Page.AllowedEditorGroups is null)
                {
                    if (allowedEditorGroups is not null)
                    {
                        return false;
                    }
                }
                else if (allowedEditorGroups is null
                    || !page.Page.AllowedEditorGroups.Order().SequenceEqual(allowedEditorGroups.Order()))
                {
                    return false;
                }

                if (page.Page.AllowedViewers is null)
                {
                    if (allowedViewers is not null)
                    {
                        return false;
                    }
                }
                else if (allowedViewers is null
                    || !page.Page.AllowedViewers.Order().SequenceEqual(allowedViewers.Order()))
                {
                    return false;
                }

                if (page.Page.AllowedViewerGroups is null)
                {
                    if (allowedViewerGroups is not null)
                    {
                        return false;
                    }
                }
                else if (allowedViewerGroups is null
                    || !page.Page.AllowedViewerGroups.Order().SequenceEqual(allowedViewerGroups.Order()))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<List<MessageResponse>> GetTopicMessagesAsync(Topic? topic)
    {
        var responses = new List<MessageResponse>();
        if (topic?.Messages is not null)
        {
            foreach (var message in topic.Messages)
            {
                var html = string.Empty;
                var preview = false;
                var link = message.WikiLinks.SingleOrDefault();
                if (link?.IsCategory == false
                    && !link.IsMissing
                    && string.IsNullOrEmpty(link.Action))
                {
                    var article = await dataStore.GetWikiPageAsync(wikiOptions, link.Title);
                    if (article?.Exists == true)
                    {
                        preview = true;
                        html = string.Format(
                            PreviewTemplate,
                            string.IsNullOrEmpty(article.Title.Domain)
                                ? string.Empty
                                : string.Format(PreviewDomainTemplate, article.Title.Domain),
                            string.IsNullOrEmpty(article.Title.Namespace)
                                ? string.Empty
                                : string.Format(PreviewNamespaceTemplate, article.Title.Namespace),
                            article.Title,
                            article.Preview);
                    }
                }
                if (!preview)
                {
                    html = message.Html;
                }
                responses.Add(new(
                    message,
                    html));
            }
        }

        return responses;
    }
}
