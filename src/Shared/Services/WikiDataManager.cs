using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Exceptions;
using Tavenem.Wiki.Blazor.Models;
using Tavenem.Wiki.Blazor.Services.Search;
using Tavenem.Wiki.Blazor.SignalR;
using Tavenem.Wiki.MarkdownExtensions.Transclusions;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor;

/// <summary>
/// Facilitates data operations for the wiki.
/// </summary>
public class WikiDataManager
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

    private readonly IDataStore _dataStore;
    private readonly ILogger _logger;
    private readonly IWikiGroupManager _groupManager;
    private readonly IWikiUserManager _userManager;
    private readonly WikiOptions _wikiOptions;

    /// <summary>
    /// Constructs a new instance of <see cref="WikiDataManager"/>.
    /// </summary>
    public WikiDataManager(
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
    /// <para>
    /// The edit was attempted in the file namespace. The <see cref="Upload(IFileManager, IFormFile,
    /// string)"/> endpoint should be used instead.
    /// </para>
    /// <para>
    /// Or, the title was empty when the namespace was non-empty and not equal to the default
    /// namespace.
    /// </para>
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
            request.WikiNamespace,
            _wikiOptions.FileNamespace,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Cannot edit content in the File namespace. Use the Upload endpoint instead.",
                nameof(request));
        }

        if (string.IsNullOrEmpty(request.Title)
            && !string.IsNullOrEmpty(request.WikiNamespace)
            && !string.Equals(request.WikiNamespace, _wikiOptions.DefaultNamespace, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The title cannot be empty if the namespace is non-empty and not equal to the default namespace.",
                nameof(request));
        }
        var title = request.Title ?? _wikiOptions.MainPageTitle;

        if (user is null)
        {
            throw new WikiUnauthorizedException();
        }
        var wikiUser = await _userManager.GetUserAsync(user);
        if (wikiUser is null
            || wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            throw new WikiUnauthorizedException();
        }

        var result = await _dataStore.GetWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            request.OriginalTitle ?? title,
            request.OriginalWikiNamespace ?? request.WikiNamespace,
            request.OriginalDomain ?? request.Domain,
            wikiUser,
            true);
        if (!result.Permission.HasFlag(WikiPermission.Write))
        {
            throw new WikiUnauthorizedException();
        }
        if (result.Item is null
            && !result.Permission.HasFlag(WikiPermission.Create))
        {
            throw new WikiUnauthorizedException();
        }

        var ownerId = request.OwnerSelf
            ? wikiUser.Id
            : request.Owner;

        if (!result.Permission.HasFlag(WikiPermission.SetOwner)
            && ownerId != result.Item?.Owner)
        {
            throw new WikiUnauthorizedException();
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
            title,
            request.WikiNamespace,
            request.Domain,
            request.Markdown,
            request.RevisionComment,
            request.IsDeleted,
            intendedOwner?.Id,
            allAllowedEditors,
            allAllowedViewers,
            allAllowedEditorGroups,
            allAllowedViewerGroups,
            request.OriginalTitle,
            request.OriginalWikiNamespace,
            request.OriginalDomain);
        if (!success)
        {
            throw new InvalidOperationException("Article could not be updated. You may not have the appropriate permissions.");
        }

        if (request.LeaveRedirect
            && result.Item is not null
            && (!string.Equals(result.Item.Title, title, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(result.Item.WikiNamespace, request.WikiNamespace, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(result.Item.Domain, request.Domain, StringComparison.OrdinalIgnoreCase)))
        {
            var redirectSuccess = await _dataStore.AddOrReviseWikiItemAsync(
                _wikiOptions,
                _userManager,
                _groupManager,
                wikiUser,
                result.Item.Title,
                result.Item.WikiNamespace,
                result.Item.Domain,
                $$$"""{{redirect|{{{Article.GetFullTitle(_wikiOptions, title ?? result.Item.Title, request.WikiNamespace ?? result.Item.WikiNamespace, request.Domain ?? result.Item.Domain)}}}}}""",
                request.RevisionComment,
                false,
                intendedOwner?.Id,
                allAllowedEditors,
                allAllowedViewers,
                allAllowedEditorGroups,
                allAllowedViewerGroups);
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
        var wikiUser = await _userManager.GetUserAsync(user);
        if (wikiUser is null)
        {
            throw new WikiUnauthorizedException();
        }

        var hasDomain = !string.IsNullOrEmpty(domain);
        if (hasDomain)
        {
            var domainPermission = WikiPermission.None;
            if (_wikiOptions.GetDomainPermission is not null)
            {
                domainPermission = await _wikiOptions
                    .GetDomainPermission
                    .Invoke(wikiUser.Id, domain!);
            }
            if (wikiUser.AllowedViewDomains?.Contains(domain) == true)
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

        return await _dataStore.GetWikiArchiveAsync(_wikiOptions, domain);
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
    public async Task<CategoryInfo?> GetCategoryAsync(
        ClaimsPrincipal? user,
        string title,
        string? domain = null)
    {
        var wikiUser = user is null
            ? null
            : await _userManager.GetUserAsync(user);

        var response = await _dataStore.GetCategoryAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            title,
            domain,
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
    /// <param name="wikiNamespace">The namespace of the requested content.</param>
    /// <param name="domain">The domain of the requested content (if any).</param>
    /// <param name="noRedirect">
    /// Whether to prevent redirects when fetching content.
    /// </param>
    /// <returns>A <see cref="WikiEditInfo"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// The <paramref name="title"/> was empty when <paramref name="wikiNamespace"/> was non-empty
    /// and not equal to the default namespace.
    /// </exception>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have permission to make the requested edit.
    /// </exception>
    public async Task<WikiEditInfo> GetEditInfoAsync(
        ClaimsPrincipal? user,
        string? title = null,
        string? wikiNamespace = null,
        string? domain = null,
        bool noRedirect = false)
    {
        if (string.IsNullOrEmpty(title)
            && !string.IsNullOrEmpty(wikiNamespace)
            && !string.Equals(wikiNamespace, _wikiOptions.DefaultNamespace, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The {nameof(title)} parameter cannot be empty if the {nameof(wikiNamespace)} parameter is non-empty and not equal to the default namespace.",
                nameof(title));
        }

        var wikiUser = user is null
            ? null
            : await _userManager.GetUserAsync(user);

        var result = await _dataStore.GetWikiItemForEditingAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            title,
            wikiNamespace,
            domain,
            wikiUser,
            noRedirect);
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
            : await _userManager.GetUserAsync(user);

        var result = await _dataStore.GetGroupPageAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
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
    /// <exception cref="ArgumentException">
    /// The title was empty when the namespace was non-empty and not equal to the default namespace.
    /// </exception>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have <see cref="WikiPermission.Read"/> permission for the given content.
    /// </exception>
    public async Task<PagedRevisionInfo?> GetHistoryAsync(ClaimsPrincipal? user, HistoryRequest request)
    {
        if (string.IsNullOrEmpty(request.Title)
            && !string.IsNullOrEmpty(request.WikiNamespace)
            && !string.Equals(request.WikiNamespace, _wikiOptions.DefaultNamespace, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The title cannot be empty if the namespace is non-empty and not equal to the default namespace.",
                nameof(request));
        }

        var wikiUser = user is null
            ? null
            : await _userManager.GetUserAsync(user);

        var result = await _dataStore.GetHistoryAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
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
    /// Fetches information about the given wiki content.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="title">The title of the requested content.</param>
    /// <param name="wikiNamespace">The namespace of the requested content.</param>
    /// <param name="domain">The domain of the requested content (if any).</param>
    /// <param name="noRedirect">
    /// Whether to prevent redirects when fetching content.
    /// </param>
    /// <param name="requestedDiffCurrent">
    /// Whether a diff between the requested version and the current version is requested.
    /// </param>
    /// <param name="requestedDiffPrevious">
    /// Whether a diff between the requested version and the previous version is requested.
    /// </param>
    /// <param name="requestedDiffTimestamp">
    /// The timestamp of the version with which the requested version should be compared.
    /// </param>
    /// <param name="requestedTimestamp">
    /// The timestamp of the requested version.
    /// </param>
    /// <returns>A <see cref="WikiItemInfo"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// The <paramref name="title"/> was empty when <paramref name="wikiNamespace"/> was non-empty
    /// and not equal to the default namespace.
    /// </exception>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have <see cref="WikiPermission.Read"/> permission for the given content.
    /// </exception>
    public async Task<WikiItemInfo> GetItemAsync(
        ClaimsPrincipal? user,
        string? title = null,
        string? wikiNamespace = null,
        string? domain = null,
        bool noRedirect = false,
        bool requestedDiffCurrent = false,
        bool requestedDiffPrevious = false,
        long? requestedDiffTimestamp = null,
        long? requestedTimestamp = null)
    {
        if (string.IsNullOrEmpty(title)
            && !string.IsNullOrEmpty(wikiNamespace)
            && !string.Equals(wikiNamespace, _wikiOptions.DefaultNamespace, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The {nameof(title)} parameter cannot be empty if the {nameof(wikiNamespace)} parameter is non-empty and not equal to the default namespace.",
                nameof(title));
        }

        var wikiUser = user is null
            ? null
            : await _userManager.GetUserAsync(user);

        WikiItemInfo result;
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
                domain,
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
                domain,
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
                    domain,
                    wikiUser)
                : await _dataStore.GetWikiItemDiffWithCurrentAsync(
                    _wikiOptions,
                    _userManager,
                    _groupManager,
                    requestedDiffTimestamp.Value,
                    title,
                    wikiNamespace,
                    domain,
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
                domain,
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
                domain,
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
    /// <returns>A <see cref="ListResponse"/> instance.</returns>
    public async Task<ListResponse> GetListAsync(SpecialListRequest request)
        => new(await _dataStore.GetSpecialListAsync(_wikiOptions, request));

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
        var (domain, wikiNamespace, title, isTalk, _) = Article.GetTitleParts(_wikiOptions, link);
        if (isTalk)
        {
            return null;
        }

        var wikiUser = user is null
            ? null
            : await _userManager.GetUserAsync(user);

        var result = await _dataStore.GetWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            title,
            wikiNamespace,
            domain,
            wikiUser);
        if (result.Item?.IsDeleted != false
            || !result.Permission.HasFlag(WikiPermission.Read))
        {
            return null;
        }
        return result.Item.Preview;
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
        var (domain, wikiNamespace, title, isTalk, defaultNamespace) = Article.GetTitleParts(_wikiOptions, input);
        if (string.IsNullOrEmpty(title))
        {
            return new();
        }

        var request = new SearchRequest
        {
            Domain = domain,
            PageSize = 10,
            Query = title,
            TitleMatchOnly = true,
            WikiNamespace = defaultNamespace ? null : wikiNamespace,
        };
        var wikiUser = user is null
            ? null
            : await _userManager.GetUserAsync(user);
        var result = await searchClient.SearchAsync(request, wikiUser);

        return result
            .SearchHits
            .Select(x => x.FullTitle)
            .ToList();
    }

    /// <summary>
    /// Get the talk messages for a given page.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="title">The title of the requested content.</param>
    /// <param name="wikiNamespace">The namespace of the requested content.</param>
    /// <param name="domain">The domain of the requested content (if any).</param>
    /// <param name="noRedirect">
    /// Whether to prevent redirects when fetching content.
    /// </param>
    /// <returns>
    /// A <see cref="TalkResponse"/> instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The <paramref name="title"/> was empty when <paramref name="wikiNamespace"/> was non-empty
    /// and not equal to the default namespace.
    /// </exception>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have <see cref="WikiPermission.Read"/> permission for the given content.
    /// </exception>
    public async Task<TalkResponse> GetTalkAsync(
        ClaimsPrincipal? user,
        string? title = null,
        string? wikiNamespace = null,
        string? domain = null,
        bool noRedirect = false)
    {
        if (string.IsNullOrEmpty(title)
            && !string.IsNullOrEmpty(wikiNamespace)
            && !string.Equals(wikiNamespace, _wikiOptions.DefaultNamespace, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The {nameof(title)} parameter cannot be empty if the {nameof(wikiNamespace)} parameter is non-empty and not equal to the default namespace.",
                nameof(title));
        }

        var wikiUser = user is null
            ? null
            : await _userManager.GetUserAsync(user);

        var result = await _dataStore.GetWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            title,
            wikiNamespace,
            domain,
            wikiUser,
            noRedirect);
        if (!result.Permission.HasFlag(WikiPermission.Read))
        {
            throw new WikiUnauthorizedException();
        }
        if (result.Item?.IsDeleted != false)
        {
            return new(null, null);
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
                        link.WikiNamespace,
                        link.Domain);
                    if (article?.IsDeleted == false)
                    {
                        preview = true;
                        var domainStr = string.IsNullOrEmpty(article.Domain)
                            ? string.Empty
                            : string.Format(PreviewDomainTemplate, article.Domain);
                        var namespaceStr = article.WikiNamespace == _wikiOptions.DefaultNamespace
                            ? string.Empty
                            : string.Format(PreviewNamespaceTemplate, article.WikiNamespace);
                        html = HtmlEncoder.Default.Encode(string.Format(
                            PreviewTemplate,
                            domainStr,
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

        return new TalkResponse(
            responses,
            result.Item.Id);
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
        var wikiUser = await _userManager.GetUserAsync(user);
        if (wikiUser is null
            || wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            return 0;
        }
        return await _groupManager.UserMaxUploadLimit(wikiUser);
    }

    /// <summary>
    /// Fetches a list of the pages which link to a given resource.
    /// </summary>
    /// <param name="request">a <see cref="WhatLinksHereRequest"/> instance.</param>
    /// <returns>A <see cref="ListResponse"/> instance.</returns>
    public async Task<ListResponse> GetWhatLinksHereAsync(WhatLinksHereRequest request)
    {
        var result = await _dataStore.GetWhatLinksHereAsync(
            _wikiOptions,
            request);
        if (result is null)
        {
            return new(new(null, 1, request.PageSize, 0));
        }
        return new ListResponse(result);
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
        var wikiUser = await _userManager.GetUserAsync(user);
        if (wikiUser is null
            || wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            return null;
        }
        return new WikiUser
        {
            AllowedEditArticles = wikiUser.AllowedEditArticles,
            AllowedViewArticles = wikiUser.AllowedViewArticles,
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
        var wikiUser = await _userManager.FindByIdAsync(query);
        wikiUser ??= await _userManager.FindByNameAsync(query);
        if (wikiUser is null)
        {
            return null;
        }

        var requestingUser = await _userManager.GetUserAsync(user);
        if (requestingUser is null
            || requestingUser.IsDeleted
            || requestingUser.IsDisabled)
        {
            return null;
        }

        var userPage = requestingUser.IsWikiAdmin
            || (!wikiUser.IsDeleted
            && !wikiUser.IsDisabled)
            ? await Article.GetArticleAsync(
                _wikiOptions,
                _dataStore,
                wikiUser.Id,
                _wikiOptions.UserNamespace,
                null,
                true)
            : null;
        if (requestingUser.IsWikiAdmin)
        {
            return new WikiUserInfo(
                wikiUser.Id,
                new WikiUser
                {
                    AllowedEditArticles = wikiUser.AllowedEditArticles,
                    AllowedViewArticles = wikiUser.AllowedViewArticles,
                    AllowedViewDomains = wikiUser.AllowedViewDomains,
                    DisplayName = wikiUser.DisplayName,
                    Groups = wikiUser.Groups,
                    Id = wikiUser.Id,
                    IsDeleted = wikiUser.IsDeleted,
                    IsDisabled = wikiUser.IsDisabled,
                    IsWikiAdmin = wikiUser.IsWikiAdmin,
                    UploadLimit = wikiUser.UploadLimit,
                },
                userPage is not null);
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
            },
            userPage is not null);
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
    public async Task<TalkResponse> PostTalkAsync(
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
            : await _userManager.GetUserAsync(user);
        if (wikiUser?.IsDeleted != false
            || wikiUser.IsDisabled)
        {
            throw new WikiUnauthorizedException();
        }

        var result = await _dataStore.GetWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            reply.TopicId,
            wikiUser);
        if (!result.Permission.HasFlag(WikiPermission.Read))
        {
            throw new WikiUnauthorizedException();
        }
        if (result.Item?.IsDeleted != false)
        {
            return new(null, null);
        }

        _ = await Message.ReplyAsync(
            _wikiOptions,
            _dataStore,
            reply.TopicId,
            wikiUser.Id,
            wikiUser.IsWikiAdmin,
            wikiUser.DisplayName ?? wikiUser.Id,
            reply.Markdown,
            reply.MessageId);

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
                        link.WikiNamespace,
                        link.Domain);
                    if (article?.IsDeleted == false)
                    {
                        preview = true;
                        var domainStr = string.IsNullOrEmpty(article.Domain)
                            ? string.Empty
                            : string.Format(PreviewDomainTemplate, article.Domain);
                        var namespaceStr = article.WikiNamespace == _wikiOptions.DefaultNamespace
                            ? string.Empty
                            : string.Format(PreviewNamespaceTemplate, article.WikiNamespace);
                        html = HtmlEncoder.Default.Encode(string.Format(
                            PreviewTemplate,
                            domainStr,
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

        return new TalkResponse(
            responses,
            result.Item.Id);
    }

    /// <summary>
    /// Gets a preview of the given content in its rendered form.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="request">A <see cref="PreviewRequest"/> instance.</param>
    /// <returns>
    /// A <see cref="string"/> containing the preview; or <see langword="null"/> if there is no such
    /// content, or the user's account is not found, deleted, or disabled.
    /// </returns>
    public async Task<string?> PreviewAsync(ClaimsPrincipal? user, PreviewRequest request)
    {
        if (user is null)
        {
            return null;
        }
        var wikiUser = await _userManager.GetUserAsync(user);
        if (wikiUser is null
            || wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            return null;
        }

        var fullTitle = Article.GetFullTitle(
            _wikiOptions,
            request.Title ?? "Example",
            request.WikiNamespace ?? _wikiOptions.DefaultNamespace,
            request.Domain);

        Console.WriteLine(request.Content);
        return MarkdownItem.RenderHtml(
            _wikiOptions,
            _dataStore,
            await TransclusionParser.TranscludeAsync(
                _wikiOptions,
                _dataStore,
                request.Title,
                fullTitle,
                request.Content));
    }

    /// <summary>
    /// Performs a search.
    /// </summary>
    /// <param name="searchClient">An <see cref="ISearchClient"/> instance.</param>
    /// <param name="user">The user making the request.</param>
    /// <param name="request">The search request.</param>
    /// <returns>
    /// A <see cref="SearchResponse"/> object.
    /// </returns>
    public async Task<SearchResponse> SearchAsync(
        ISearchClient searchClient,
        ClaimsPrincipal? user,
        SearchRequest request)
    {
        if (string.IsNullOrEmpty(request.Query))
        {
            return new SearchResponse(
                request.Descending,
                request.Query,
                new PagedListDTO<SearchHit>(),
                request.Sort,
                request.Owner,
                request.WikiNamespace,
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

                IWikiOwner? foundOwner = await _userManager.FindByIdAsync(ownerId);
                foundOwner ??= await _groupManager.FindByIdAsync(ownerId);
                if (foundOwner is not null)
                {
                    ownerIds.Add(excluded ? $"!{foundOwner.Id}" : foundOwner.Id);
                }
            }
            if (ownerIds.Count == 0)
            {
                return new SearchResponse(
                    request.Descending,
                    request.Query,
                    new PagedListDTO<SearchHit>(),
                    request.Sort,
                    request.Owner,
                    request.WikiNamespace,
                    request.Domain);
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
        var (queryDomain, queryNamespace, title, _, queryIsDefault) = Article.GetTitleParts(_wikiOptions, query);
        if (string.IsNullOrEmpty(queryDomain)
            && !string.IsNullOrEmpty(request.Domain))
        {
            queryDomain = request.Domain;
        }
        if (queryIsDefault && singleSearchNamespace is not null)
        {
            queryNamespace = singleSearchNamespace;
        }

        var exactMatch = await _dataStore.GetWikiItemAsync(_wikiOptions, title, queryNamespace, queryDomain);
        if (exactMatch?.IsDeleted == true)
        {
            exactMatch = null;
        }

        var wikiUser = user is null
            ? null
            : await _userManager.GetUserAsync(user);
        var result = await searchClient.SearchAsync(new SearchRequest
        {
            Descending = request.Descending,
            Owner = ownerQuery,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Query = query,
            Sort = request.Sort,
            WikiNamespace = namespaceQuery,
            Domain = request.Domain,
        }, wikiUser);

        return new SearchResponse(
            request.Descending,
            original,
            new PagedListDTO<SearchHit>(
                result
                    .SearchHits
                    .Select(x => new SearchHit(
                        x.Title,
                        x.WikiNamespace,
                        x.Domain,
                        x.FullTitle,
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
    /// <param name="fileManager">An <see cref="IFileManager"/> instance.</param>
    /// <param name="file"></param>
    /// <param name="options">
    /// An <see cref="UploadRequest"/> instance.
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
    /// <paramref name="options"/> does not specify a <see cref="UploadRequest.Title"/>.
    /// </para>
    /// <para>
    /// Or, the <see cref="UploadRequest.Title"/> specified in <paramref name="options"/> contains a
    /// namespace.
    /// </para>
    /// <para>
    /// Or, the size of <paramref name="file"/> exceeds <see cref="WikiOptions.MaxFileSize"/>, or
    /// the user's own upload limit.
    /// </para>
    /// <para>
    /// Or, the specified <see cref="UploadRequest.Owner"/> does not exist.
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
        IFormFile file,
        UploadRequest options)
    {
        if (string.IsNullOrEmpty(options.Title))
        {
            throw new ArgumentException("A title is required.", nameof(options));
        }
        if (options.Title.Contains(':'))
        {
            throw new ArgumentException("Files may not have namespaces.", nameof(options));
        }
        if (file.Length > _wikiOptions.MaxFileSize)
        {
            throw new ArgumentException($"File size exceeds {_wikiOptions.MaxFileSizeString}.", nameof(file));
        }
        if (user is null)
        {
            throw new WikiUnauthorizedException();
        }
        var wikiUser = await _userManager.GetUserAsync(user);
        if (wikiUser is null
            || wikiUser.IsDeleted
            || wikiUser.IsDisabled)
        {
            throw new WikiUnauthorizedException();
        }
        var limit = await _groupManager.UserMaxUploadLimit(wikiUser);
        if (limit == 0)
        {
            throw new WikiUnauthorizedException();
        }
        if (file.Length > limit)
        {
            throw new ArgumentException("The size of this file exceeds your allotted upload limit.", nameof(file));
        }
        var freeSpace = await fileManager.GetFreeSpaceAsync(wikiUser);
        if (freeSpace >= 0 && file.Length > freeSpace)
        {
            throw new ArgumentException("The size of this file exceeds your remaining upload space.", nameof(file));
        }

        var result = await _dataStore.GetWikiItemAsync(
            _wikiOptions,
            _userManager,
            _groupManager,
            options.Title,
            _wikiOptions.FileNamespace,
            options.Domain,
            wikiUser,
            true);
        if (!result.Permission.HasFlag(WikiPermission.Write)
            || (result.Item is null
            && !result.Permission.HasFlag(WikiPermission.Create)))
        {
            throw new WikiUnauthorizedException();
        }

        var ownerId = options.OwnerSelf
            || string.IsNullOrEmpty(options.Owner)
            ? wikiUser.Id
            : options.Owner;

        if (!result.Permission.HasFlag(WikiPermission.SetOwner)
            && ownerId != result.Item?.Owner)
        {
            throw new WikiUnauthorizedException();
        }

        var intendedOwner = await _userManager.FindByIdAsync(ownerId);
        if (intendedOwner is null)
        {
            throw new ArgumentException("No such owner found.", nameof(options));
        }

        if (result.Item is not null && !options.OverwriteConfirmed)
        {
            throw new WikiConflictException("A file with this title already exists.");
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
            throw;
        }
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new InvalidOperationException("File save operation failed.");
        }

        WikiFile? wikiFile = null;
        var success = true;
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
                success = false;
            }
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
                var editor = await _userManager.FindByIdAsync(id);
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
                var editor = await _groupManager.FindByIdAsync(id);
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
                var editor = await _userManager.FindByIdAsync(id);
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
                    options.Title,
                    wikiUser.Id,
                    storagePath,
                    (int)file.Length,
                    file.ContentType,
                    options.Markdown,
                    options.Domain,
                    options.RevisionComment,
                    intendedOwner.Id,
                    allAllowedEditors,
                    allAllowedViewers,
                    allAllowedEditorGroups,
                    allAllowedViewerGroups);
                return success;
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "User with ID {UserId} failed to upload a new file with title {Title} of size {Length}.",
                    wikiUser.Id,
                    options.Title,
                    file.Length);
                throw;
            }
        }

        var titleCase = options.Title.ToWikiTitleCase();
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
                options.Markdown,
                options.RevisionComment,
                options.Domain,
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
                options.Title,
                file.Length);
            throw;
        }
        return success;
    }
}
