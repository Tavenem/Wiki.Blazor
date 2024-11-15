using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;
using Tavenem.Blazor.Framework;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Exceptions;
using Tavenem.Wiki.Models;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Services;

/// <summary>
/// Facilitates data operations for the wiki.
/// </summary>
public class ClientWikiDataService(
    WikiDataService wikiDataService,
    ILoggerFactory loggerFactory,
    NavigationManager navigationManager,
    IServiceProvider serviceProvider,
    SnackbarService snackbarService,
    WikiBlazorOptions wikiBlazorClientOptions,
    WikiOptions wikiOptions,
    WikiState wikiState)
{
    private readonly IDataStore? _dataStore = serviceProvider.GetService<IDataStore>();
    private readonly ILogger _logger = loggerFactory.CreateLogger("Wiki");

    private AuthenticationStateProvider? _authenticationStateProvider;
    private HttpClient? _httpClient;

    /// <summary>
    /// Performs the requested edit operation.
    /// </summary>
    /// <param name="request">
    /// An <see cref="EditRequest"/> instance describing the edit.
    /// </param>
    /// <param name="failMessage">
    /// A message to supply when the operation fails, and no message is returned.
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
    public Task<bool> EditAsync(EditRequest request, string? failMessage = null) => PostAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/edit",
        request,
        WikiBlazorJsonSerializerContext.Default.EditRequest,
        user => wikiDataService.EditAsync(user, request),
        failMessage);

    /// <summary>
    /// Retrieve an archive of a domain, or the entire wiki.
    /// </summary>
    /// <param name="domain">
    /// The domain to be archived; or an empty string to archive content with no domain; or <see
    /// langword="null"/> to archive the entire wiki.
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
    public Task<Archive?> GetArchiveAsync(string? domain = null)
    {
        var url = new StringBuilder(wikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/archive");
        if (!string.IsNullOrEmpty(domain))
        {
            url.Append("?domain=")
                .Append(domain);
        }
        return FetchDataAsync(
            url.ToString(),
            WikiArchiveJsonSerializerContext.Default.Archive,
            async user => await wikiDataService.GetArchiveAsync(
                user,
                domain));
    }

    /// <summary>
    /// Gets information about the category with the given <paramref name="title"/>.
    /// </summary>
    /// <param name="title">The requested category title.</param>
    /// <returns>
    /// A <see cref="Category"/> object.
    /// </returns>
    public Task<Category?> GetCategoryAsync(PageTitle title) => FetchDataAsync(
        new StringBuilder(wikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/category?title=")
            .Append(title.Title)
            .ToString(),
        WikiJsonSerializerContext.Default.Category,
        async user => await wikiDataService.GetCategoryAsync(
            user,
            new PageTitle(title.Title, wikiOptions.CategoryNamespace, title.Domain)));

    /// <summary>
    /// Fetches edit info for the given content.
    /// </summary>
    /// <param name="title">The title of the requested content.</param>
    /// <returns>A <see cref="Page"/> instance.</returns>
    public Task<Page?> GetEditInfoAsync(PageTitle title)
    {
        var url = new StringBuilder(wikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/editinfo?title=")
            .Append(title.Title);
        if (!string.IsNullOrEmpty(title.Namespace))
        {
            url.Append("&namespace=")
                .Append(title.Namespace);
        }
        if (!string.IsNullOrEmpty(title.Domain))
        {
            url.Append("&domain=")
                .Append(title.Domain);
        }

        return FetchDataAsync(
            url.ToString(),
            WikiJsonSerializerContext.Default.Page,
            async user => await wikiDataService.GetEditInfoAsync(user, title));
    }

    /// <summary>
    /// Fetches information about the group page with the given <paramref name="title"/>.
    /// </summary>
    /// <param name="title">
    /// The title of a group page (i.e. the group's <see cref="IIdItem.Id"/>).
    /// </param>
    /// <returns>
    /// A <see cref="GroupPage"/> instance.
    /// </returns>
    public Task<GroupPage?> GetGroupPageAsync(string title) => FetchDataAsync(
        new StringBuilder(wikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/group?title=")
            .Append(title)
            .ToString(),
        WikiJsonSerializerContext.Default.GroupPage,
        async user => await wikiDataService.GetGroupPageAsync(user, title));

    /// <summary>
    /// Gets revision information for the requested content.
    /// </summary>
    /// <param name="request">A <see cref="HistoryRequest"/> instance.</param>
    /// <returns>
    /// A <see cref="PagedRevisionInfo"/> instance; or <see langword="null"/> if no such
    /// page exists.
    /// </returns>
    public Task<PagedRevisionInfo?> GetHistoryAsync(HistoryRequest request) => PostAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/history",
        request,
        WikiJsonSerializerContext.Default.HistoryRequest,
        WikiJsonSerializerContext.Default.PagedRevisionInfo,
        user => wikiDataService.GetHistoryAsync(user, request));

    /// <summary>
    /// Fetches information about the given wiki page.
    /// </summary>
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
    /// <param name="diff">Whether a diff is requested.</param>
    /// <returns>A <see cref="Page"/> instance.</returns>
    public Task<Page?> GetItemAsync(
        PageTitle title,
        bool noRedirect = false,
        DateTimeOffset? firstTime = null,
        DateTimeOffset? secondTime = null,
        bool diff = false)
    {
        var url = new StringBuilder(wikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/item?title=")
            .Append(title.Title);
        if (!string.IsNullOrEmpty(title.Namespace))
        {
            url.Append("&namespace=")
                .Append(title.Namespace);
        }
        if (!string.IsNullOrEmpty(title.Domain))
        {
            url.Append("&domain=")
                .Append(title.Domain);
        }

        if (noRedirect)
        {
            url.Append("&noRedirect=true");
        }
        if (diff)
        {
            url.Append("&diff=true");
        }
        if (firstTime.HasValue)
        {
            url.Append("&firstTime=")
                .Append(firstTime.Value.ToUniversalTime().Ticks);
        }
        if (secondTime.HasValue)
        {
            url.Append("&secondTime=")
                .Append(secondTime.Value.ToUniversalTime().Ticks);
        }

        return FetchDataAsync(
            url.ToString(),
            WikiJsonSerializerContext.Default.Page,
            async user => await wikiDataService.GetItemAsync(
                user,
                title,
                noRedirect,
                firstTime,
                secondTime,
                diff));
    }

    /// <summary>
    /// Fetches a special list for the given request.
    /// </summary>
    /// <param name="request">A <see cref="SpecialListRequest"/> instance.</param>
    /// <returns>A <see cref="PagedList{T}"/> of <see cref="LinkInfo"/> instances.</returns>
    public Task<PagedList<LinkInfo>?> GetListAsync(SpecialListRequest request) => PostAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/list",
        request,
        WikiJsonSerializerContext.Default.SpecialListRequest,
        WikiBlazorJsonSerializerContext.Default.PagedListLinkInfo,
        async user => await wikiDataService.GetListAsync(request));

    /// <summary>
    /// Gets the preview content of an article.
    /// </summary>
    /// <param name="title">The title of the requested page.</param>
    /// <returns>
    /// The preview content; or <see langword="null"/> if there is no such article, or the given
    /// user does not have permission to view it.
    /// </returns>
    public Task<string?> GetPreviewAsync(PageTitle title)
    {
        var url = new StringBuilder(wikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/preview?title=")
            .Append(title.Title);
        if (!string.IsNullOrEmpty(title.Namespace))
        {
            url.Append("&namespace=")
                .Append(title.Namespace);
        }
        if (!string.IsNullOrEmpty(title.Domain))
        {
            url.Append("&domain=")
                .Append(title.Domain);
        }
        return FetchStringAsync(
            url.ToString(),
            async user => await wikiDataService.GetPreviewAsync(user, title));
    }

    /// <summary>
    /// Get the talk messages for a given page.
    /// </summary>
    /// <param name="title">The title of the requested content.</param>
    /// <param name="noRedirect">
    /// Whether to prevent redirects when fetching content.
    /// </param>
    /// <returns>
    /// A <see cref="MessageResponse"/> instance.
    /// </returns>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have <see cref="WikiPermission.Read"/> permission for the given content.
    /// </exception>
    public async Task<List<MessageResponse>?> GetTalkAsync(PageTitle title, bool noRedirect = false)
    {
        _httpClient ??= serviceProvider.GetService<HttpClient>();
        if (_httpClient is null)
        {
            return null;
        }
        List<MessageResponse>? messages = null;
        try
        {
            var url = new StringBuilder(wikiBlazorClientOptions.WikiServerApiRoute)
                .Append("/talk?title=")
                .Append(title.Title);
            if (!string.IsNullOrEmpty(title.Namespace))
            {
                url.Append("&namespace=")
                    .Append(title.Namespace);
            }
            if (!string.IsNullOrEmpty(title.Domain))
            {
                url.Append("&domain=")
                    .Append(title.Domain);
            }
            if (noRedirect)
            {
                url.Append("&noRedirect=true");
            }
            var response = await _httpClient.GetAsync(url.ToString());
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                wikiState.NotAuthorized = true;
            }
            else if (response.StatusCode is System.Net.HttpStatusCode.BadRequest
                or System.Net.HttpStatusCode.NoContent)
            {
                navigationManager.NavigateTo(
                    wikiState.Link(title.Title, title.Namespace, title.Domain),
                    replace: true);
            }
            else
            {
                messages = await response.Content.ReadFromJsonAsync(WikiBlazorJsonSerializerContext.Default.ListMessageResponse);
            }
        }
        catch (Exception ex)
        {
            _logger.Log(
                LogLevel.Error,
                ex,
                "Error getting talk messages for wiki item with title {Title}.",
                title);
            snackbarService.Add("An error occurred", ThemeColor.Danger);
        }
        return messages;
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
    public Task<int> GetUploadLimitAsync() => FetchIntAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/uploadlimit",
        wikiDataService.GetUploadLimitAsync);

    /// <summary>
    /// Fetches information about the group page with the given <paramref name="title"/>.
    /// </summary>
    /// <param name="title">
    /// The title of a group page (i.e. the group's <see cref="IIdItem.Id"/>).
    /// </param>
    /// <returns>
    /// A <see cref="UserPage"/> instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="title"/> was empty.
    /// </exception>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have permission to view the requested page.
    /// </exception>
    public Task<UserPage?> GetUserPageAsync(string title) => FetchDataAsync(
        new StringBuilder(wikiBlazorClientOptions.WikiServerApiRoute)
            .Append("/userpage?title=")
            .Append(title)
            .ToString(),
        WikiJsonSerializerContext.Default.UserPage,
        async user => await wikiDataService.GetUserPageAsync(user, title));

    /// <summary>
    /// Fetches a list of the pages which link to a given resource.
    /// </summary>
    /// <param name="request">a <see cref="TitleRequest"/> instance.</param>
    /// <returns>A <see cref="PagedList{T}"/> of <see cref="LinkInfo"/> instances.</returns>
    public Task<PagedList<LinkInfo>?> GetWhatLinksHereAsync(TitleRequest request) => PostAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/whatlinkshere",
        request,
        WikiJsonSerializerContext.Default.TitleRequest,
        WikiBlazorJsonSerializerContext.Default.PagedListLinkInfo,
        async user => await wikiDataService.GetWhatLinksHereAsync(request));

    /// <summary>
    /// Gets a list of the given content's embedded wiki links.
    /// </summary>
    /// <param name="request">A <see cref="PreviewRequest"/> instance.</param>
    /// <returns>
    /// A <see cref="List{T}"/> of <see cref="WikiLink"/>s (possibly empty); or <see
    /// langword="null"/> if the user's account is not found, deleted, or disabled.
    /// </returns>
    public Task<List<WikiLink>?> GetWikiLinksAsync(PreviewRequest request) => PostAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/preview",
        request,
        WikiBlazorJsonSerializerContext.Default.PreviewRequest,
        WikiBlazorJsonSerializerContext.Default.ListWikiLink,
        _ => wikiDataService.GetWikiLinksAsync(request));

    /// <summary>
    /// Fetches information about a given wiki owner.
    /// </summary>
    /// <param name="query">
    /// A wiki user ID or username.
    /// </param>
    /// <returns>
    /// An <see cref="IWikiOwner"/> instance; or <see langword="null"/> if there is no such owner.
    /// </returns>
    public Task<IWikiOwner?> GetWikiOwnerAsync(string query) => FetchDataAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/wikiowner?query={query}",
        WikiJsonSerializerContext.Default.IWikiOwner,
        async user =>
        {
            IWikiOwner? wikiOwner = await wikiDataService.GetWikiUserAsync(user, query);
            wikiOwner ??= await wikiDataService.GetWikiGroupAsync(query);
            return wikiOwner;
        });

    /// <summary>
    /// Gets the current wiki user.
    /// </summary>
    /// <returns>
    /// An <see cref="IWikiUser"/> instance; or <see langword="null"/> if there is no such user, or
    /// if the given user is deleted or disabled.
    /// </returns>
    public Task<WikiUser?> GetWikiUserAsync() => FetchDataAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/currentuser",
        WikiJsonSerializerContext.Default.WikiUser,
        wikiDataService.GetWikiUserAsync);

    /// <summary>
    /// Gets the given content's rendered HTML.
    /// </summary>
    /// <param name="request">A <see cref="PreviewRequest"/> instance.</param>
    /// <returns>
    /// A <see cref="string"/> containing the HTML; or <see langword="null"/> if there is no such
    /// content, or the user's account is not found, deleted, or disabled.
    /// </returns>
    public Task<string?> RenderHtmlAsync(PreviewRequest request) => PostForStringAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/html",
        request,
        WikiBlazorJsonSerializerContext.Default.PreviewRequest,
        _ => wikiDataService.RenderHtmlAsync(request));

    /// <summary>
    /// Gets a preview of the given content's rendered HTML.
    /// </summary>
    /// <param name="request">A <see cref="PreviewRequest"/> instance.</param>
    /// <returns>
    /// A <see cref="string"/> containing the preview; or <see langword="null"/> if there is no such
    /// content, or the user's account is not found, deleted, or disabled.
    /// </returns>
    public Task<string?> RenderPreviewAsync(PreviewRequest request) => PostForStringAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/preview",
        request,
        WikiBlazorJsonSerializerContext.Default.PreviewRequest,
        _ => wikiDataService.RenderPreviewAsync(request));

    /// <summary>
    /// Restores an <see cref="Archive"/> to the wiki.
    /// </summary>
    /// <param name="archive">An <see cref="Archive"/> instance.</param>
    /// <exception cref="WikiUnauthorizedException">
    /// The user does not have appropriate permission to restore all the pages in the archive.
    /// </exception>
    public Task RestoreArchiveAsync(Archive archive) => PostAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/restorearchive",
        archive,
        WikiArchiveJsonSerializerContext.Default.Archive,
        async user =>
        {
            await wikiDataService.RestoreArchiveAsync(user, archive);
            return true;
        });

    /// <summary>
    /// Performs a search.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <returns>
    /// A <see cref="SearchResult"/> object.
    /// </returns>
    public Task<SearchResult?> SearchAsync(SearchRequest request) => PostAsync(
        $"{wikiBlazorClientOptions.WikiServerApiRoute}/search",
        request,
        WikiJsonSerializerContext.Default.SearchRequest,
        WikiBlazorJsonSerializerContext.Default.SearchResult,
        async user => await wikiDataService.SearchAsync(user, request));

    /// <summary>
    /// Fetches data from the wiki server, or the offline store.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve.</typeparam>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="type">THe JSON type info</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <returns>The result.</returns>
    private async Task<T?> FetchDataAsync<T>(
        string url,
        JsonTypeInfo<T> type,
        Func<ClaimsPrincipal?, Task<T?>> fetchLocal)
    {
        T? result = default;

        var isLocal = string.IsNullOrEmpty(wikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(wikiState.WikiDomain)
            && wikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await wikiBlazorClientOptions.IsOfflineDomain.Invoke(wikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        _authenticationStateProvider ??= serviceProvider.GetService<AuthenticationStateProvider>();
        if (_authenticationStateProvider is not null)
        {
            state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        var fetchedFromServer = false;
        _httpClient ??= serviceProvider.GetService<HttpClient>();
        if (!isLocal && _httpClient is not null)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning(
                        "Bad request when fetching data from url {URL}.",
                        url);
                    snackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    result = await response.Content.ReadFromJsonAsync(type);
                    fetchedFromServer = true;
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error fetching data from url {URL}.",
                    url);
                snackbarService.Add("An error occurred", ThemeColor.Danger);
                return default;
            }
        }

        if (!fetchedFromServer)
        {
            try
            {
                result = await fetchLocal.Invoke(user);
            }
            catch (WikiUnauthorizedException)
            {
                HandleUnauthorized(state);
                return default;
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error fetching data locally for url {URL}.",
                    url);
                wikiState.LoadError = true;
                snackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return default;
            }
        }

        return result;
    }

    /// <summary>
    /// Fetches an integer from the wiki server, or the offline store.
    /// </summary>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <returns>The result.</returns>
    private async Task<int> FetchIntAsync(
        string url,
        Func<ClaimsPrincipal?, Task<int>> fetchLocal)
    {
        var isLocal = string.IsNullOrEmpty(wikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(wikiState.WikiDomain)
            && wikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await wikiBlazorClientOptions.IsOfflineDomain.Invoke(wikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        _authenticationStateProvider ??= serviceProvider.GetService<AuthenticationStateProvider>();
        if (_authenticationStateProvider is not null)
        {
            state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        _httpClient ??= serviceProvider.GetService<HttpClient>();
        if (!isLocal && _httpClient is not null)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return 0;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning(
                        "Bad request when fetching data from url {URL}.",
                        url);
                    snackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return 0;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(result, out var value))
                    {
                        return value;
                    }
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error fetching data from url {URL}.",
                    url);
                snackbarService.Add("An error occurred", ThemeColor.Danger);
                return 0;
            }
        }

        if (_dataStore is not null)
        {
            try
            {
                return await fetchLocal.Invoke(user);
            }
            catch (WikiUnauthorizedException)
            {
                HandleUnauthorized(state);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error fetching data locally for url {URL}.",
                    url);
                snackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return 0;
            }
        }

        return 0;
    }

    /// <summary>
    /// Fetches a string from the wiki server, or the offline store.
    /// </summary>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <returns>The result.</returns>
    private async Task<string?> FetchStringAsync(
        string url,
        Func<ClaimsPrincipal?, Task<string?>> fetchLocal)
    {
        var isLocal = string.IsNullOrEmpty(wikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(wikiState.WikiDomain)
            && wikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await wikiBlazorClientOptions.IsOfflineDomain.Invoke(wikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        _authenticationStateProvider ??= serviceProvider.GetService<AuthenticationStateProvider>();
        if (_authenticationStateProvider is not null)
        {
            state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        _httpClient ??= serviceProvider.GetService<HttpClient>();
        if (!isLocal && _httpClient is not null)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning(
                        "Bad request when fetching data from url {URL}.",
                        url);
                    snackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error fetching data from url {URL}.",
                    url);
                snackbarService.Add("An error occurred", ThemeColor.Danger);
                return null;
            }
        }

        if (_dataStore is not null)
        {
            try
            {
                return await fetchLocal.Invoke(user);
            }
            catch (WikiUnauthorizedException)
            {
                HandleUnauthorized(state);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error fetching data locally for url {URL}.",
                    url);
                snackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// POSTs data to the wiki server, or the offline store.
    /// </summary>
    /// <typeparam name="TSend">The type of data to send.</typeparam>
    /// <typeparam name="TReturn">The type of data which will be returned.</typeparam>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="value">The data to send.</param>
    /// <param name="postedType">JSON type info for the data sent.</param>
    /// <param name="returnType">JSON type info for the data to be returned.</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <returns>The result.</returns>
    private async Task<TReturn?> PostAsync<TSend, TReturn>(
        string url,
        TSend value,
        JsonTypeInfo<TSend> postedType,
        JsonTypeInfo<TReturn> returnType,
        Func<ClaimsPrincipal?, Task<TReturn?>> fetchLocal)
    {
        TReturn? result = default;

        var isLocal = string.IsNullOrEmpty(wikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(wikiState.WikiDomain)
            && wikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await wikiBlazorClientOptions.IsOfflineDomain.Invoke(wikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        _authenticationStateProvider ??= serviceProvider.GetService<AuthenticationStateProvider>();
        if (_authenticationStateProvider is not null)
        {
            state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        var triedServer = false;
        var fetchedFromServer = false;
        _httpClient ??= serviceProvider.GetService<HttpClient>();
        if (!isLocal && _httpClient is not null)
        {
            triedServer = true;
            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, value, postedType);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning(
                        "Bad request when posting data to url {URL}.",
                        url);
                    snackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    result = await response.Content.ReadFromJsonAsync(returnType);
                    fetchedFromServer = true;
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error posting data to url {URL}.",
                    url);
                snackbarService.Add("An error occurred", ThemeColor.Danger);
                return default;
            }
        }

        if (!fetchedFromServer)
        {
            if (_dataStore is not null)
            {
                try
                {
                    result = await fetchLocal.Invoke(user);
                }
                catch (WikiUnauthorizedException)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                catch (Exception ex)
                {
                    _logger.Log(
                        LogLevel.Error,
                        ex,
                        "Error posting data locally for url {URL}.",
                        url);
                    snackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
            }
            else if (triedServer)
            {
                wikiState.LoadError = true;
            }
        }

        return result;
    }

    /// <summary>
    /// POSTs data to the wiki server, or the offline store.
    /// </summary>
    /// <typeparam name="T">The type of data to send.</typeparam>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="value">The data to send.</param>
    /// <param name="type">JSON type info for the data sent.</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <param name="failMessage">
    /// A message to supply when the operation fails, and no message is returned.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the call succeeded; otherwise <see langword="false"/>.
    /// </returns>
    private async Task<bool> PostAsync<T>(
        string url,
        T value,
        JsonTypeInfo<T> type,
        Func<ClaimsPrincipal?, Task<bool>> fetchLocal,
        string? failMessage = null)
    {
        var isLocal = string.IsNullOrEmpty(wikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(wikiState.WikiDomain)
            && wikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await wikiBlazorClientOptions.IsOfflineDomain.Invoke(wikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        _authenticationStateProvider ??= serviceProvider.GetService<AuthenticationStateProvider>();
        if (_authenticationStateProvider is not null)
        {
            state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        _httpClient ??= serviceProvider.GetService<HttpClient>();
        if (!isLocal && _httpClient is not null)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, value, type);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return false;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning(
                        "Bad request when posting data to url {URL}.",
                        url);
                    snackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return false;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    return true;
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error posting data to url {URL}.",
                    url);
                snackbarService.Add("An error occurred", ThemeColor.Danger);
                return false;
            }
        }

        if (_dataStore is not null)
        {
            try
            {
                var success = await fetchLocal.Invoke(user);
                if (!success && !string.IsNullOrEmpty(failMessage))
                {
                    snackbarService.Add(failMessage, ThemeColor.Warning);
                }
                return true;
            }
            catch (WikiUnauthorizedException)
            {
                HandleUnauthorized(state);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error posting data locally for url {URL}.",
                    url);
                snackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// POSTs data to the wiki server, or the offline store.
    /// </summary>
    /// <typeparam name="T">The type of data to send.</typeparam>
    /// <param name="url">The URL to call when online.</param>
    /// <param name="value">The data to send.</param>
    /// <param name="type">JSON type info for the data sent.</param>
    /// <param name="fetchLocal">The function to execute when offline.</param>
    /// <returns>A string.</returns>
    private async Task<string?> PostForStringAsync<T>(
        string url,
        T value,
        JsonTypeInfo<T> type,
        Func<ClaimsPrincipal?, Task<string?>> fetchLocal)
    {
        string? result = null;

        var isLocal = string.IsNullOrEmpty(wikiBlazorClientOptions.WikiServerApiRoute);
        if (!isLocal
            && !string.IsNullOrEmpty(wikiState.WikiDomain)
            && wikiBlazorClientOptions.IsOfflineDomain is not null)
        {
            isLocal = await wikiBlazorClientOptions.IsOfflineDomain.Invoke(wikiState.WikiDomain);
        }
        ClaimsPrincipal? user = null;
        AuthenticationState? state = null;
        _authenticationStateProvider ??= serviceProvider.GetService<AuthenticationStateProvider>();
        if (_authenticationStateProvider is not null)
        {
            state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        var fetchedFromServer = false;
        _httpClient ??= serviceProvider.GetService<HttpClient>();
        if (!isLocal && _httpClient is not null)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, value, type);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(state);
                    return default;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning(
                        "Bad request when posting data to url {URL}.",
                        url);
                    snackbarService.Add(response.ReasonPhrase ?? "Invalid operation", ThemeColor.Warning);
                    return default;
                }
                else if (response.IsSuccessStatusCode
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    result = await response.Content.ReadAsStringAsync();
                    fetchedFromServer = true;
                }
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (HttpRequestException) { }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error posting data to url {URL}.",
                    url);
                snackbarService.Add("An error occurred", ThemeColor.Danger);
                return default;
            }
        }

        if (!fetchedFromServer
            && _dataStore is not null)
        {
            try
            {
                result = await fetchLocal.Invoke(user);
            }
            catch (WikiUnauthorizedException)
            {
                HandleUnauthorized(state);
                return default;
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Error,
                    ex,
                    "Error posting data locally for url {URL}.",
                    url);
                snackbarService.Add(ex.Message ?? "Invalid operation", ThemeColor.Warning);
                return default;
            }
        }

        return result;
    }

    private void HandleUnauthorized(AuthenticationState? state)
    {
        if (state?.User.Identity?.IsAuthenticated != true)
        {
            if (state is null
                || string.IsNullOrEmpty(wikiBlazorClientOptions.LoginPath))
            {
                navigationManager.NavigateTo(
                    navigationManager.GetUriWithQueryParameter(
                        nameof(Wiki.Unauthenticated),
                        true));
            }
            else
            {
                var path = new StringBuilder(wikiBlazorClientOptions.LoginPath)
                    .Append(wikiBlazorClientOptions.LoginPath.Contains('?')
                        ? '&' : '?')
                    .Append("returnUrl=")
                    .Append(UrlEncoder.Default.Encode(navigationManager.Uri));
                navigationManager.NavigateTo(path.ToString());
            }
        }
        wikiState.NotAuthorized = true;
    }
}
