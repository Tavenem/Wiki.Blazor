using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Tavenem.DataStorage;

namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// <para>
/// The default search client performs a naive search of the <see cref="IDataStore"/>, looking
/// for exact string matches of the query in the title and markdown of each item.
/// </para>
/// <para>
/// Although this default is functional, it is neither powerful nor fast, nor does it rank
/// results intelligently. A more robust search solution is recommended. The default is supplied
/// only to ensure that search functions when no client is provided.
/// </para>
/// </summary>
public class DefaultSearchClient : ISearchClient
{
    private readonly IDataStore _dataStore;
    private readonly ILogger<DefaultSearchClient> _logger;
    private readonly WikiOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultSearchClient"/>.
    /// </summary>
    public DefaultSearchClient(
        IDataStore dataStore,
        ILogger<DefaultSearchClient> logger,
        WikiOptions options)
    {
        _dataStore = dataStore;
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Search for wiki content which matches the given search criteria.
    /// </summary>
    /// <param name="request">
    /// An <see cref="ISearchRequest" /> instance with search criteria.
    /// </param>
    /// <param name="user">
    /// The <see cref="IWikiUser" /> making the request.
    /// </param>
    /// <returns>An <see cref="ISearchResult" /> instance with search results.</returns>
    public async Task<ISearchResult> SearchAsync(ISearchRequest request, IWikiUser? user)
    {
        var queryEmpty = string.IsNullOrWhiteSpace(request.Query);
        var domainEmpty = string.IsNullOrWhiteSpace(request.Domain);
        var namespaceEmpty = string.IsNullOrWhiteSpace(request.WikiNamespace);
        var ownerEmpty = string.IsNullOrWhiteSpace(request.Owner);
        var uploaderEmpty = string.IsNullOrWhiteSpace(request.Uploader);
        if (queryEmpty && domainEmpty && namespaceEmpty && ownerEmpty && uploaderEmpty)
        {
            return new SearchResult
            {
                Descending = request.Descending,
                Query = request.Query,
                Sort = request.Sort,
            };
        }

        if (!domainEmpty)
        {
            var domainPermission = WikiPermission.None;
            if (user is not null)
            {
                if (_options.GetDomainPermission is not null)
                {
                    domainPermission = await _options.GetDomainPermission(user.Id, request.Domain!);
                }
                if (user.AllowedViewDomains?.Contains(request.Domain!) == true)
                {
                    domainPermission |= WikiPermission.Read;
                }
            }
            if (!domainPermission.HasFlag(WikiPermission.Read))
            {
                return new SearchResult
                {
                    Descending = request.Descending,
                    Query = request.Query,
                    Sort = request.Sort,
                };
            }
        }

        var namespaces = namespaceEmpty
            ? Array.Empty<string>()
            : request.WikiNamespace!.Split(';');
        var excludedNamespaces = namespaces
            .Where(x => x[0] == '!')
            .Select(x => x[1..])
            .ToList();
        var anyExcludedNamespaces = excludedNamespaces.Count > 0;
        var includedNamespaces = namespaces
            .Where(x => x[0] != '!')
            .ToList();
        var anyIncludedNamespaces = includedNamespaces.Count > 0;

        var owners = ownerEmpty
            ? Array.Empty<string>()
            : request.Owner!.Split(';');
        var excludedOwners = owners
            .Where(x => x[0] == '!')
            .Select(x => x[1..])
            .ToList();
        var anyExcludedOwners = excludedOwners.Count > 0;
        var includedOwners = owners
            .Where(x => x[0] != '!')
            .ToList();
        var anyIncludedOwners = includedOwners.Count > 0;

        var uploaders = uploaderEmpty
            ? Array.Empty<string>()
            : request.Uploader!.Split(';');
        var excludedUploaders = uploaders
            .Where(x => x[0] == '!')
            .Select(x => x[1..])
            .ToList();
        var anyExcludedUploaders = excludedUploaders.Count > 0;
        var includedUploaders = uploaders
            .Where(x => x[0] != '!')
            .ToList();
        var anyIncludedUploaders = includedUploaders.Count > 0;

        try
        {
            var hits = new List<SearchHit>();
            long pageNumber, pageSize;
            long? totalCount;

            if (anyIncludedUploaders
                || (includedNamespaces.Count == 1
                && includedNamespaces[0] == _options.FileNamespace))
            {
                var files = await GetPageAsync<WikiFile>(
                    request,
                    user,
                    queryEmpty,
                    excludedNamespaces,
                    anyExcludedNamespaces,
                    includedNamespaces,
                    anyIncludedNamespaces,
                    excludedOwners,
                    anyExcludedOwners,
                    includedOwners,
                    anyIncludedOwners);
                pageNumber = files.PageNumber;
                pageSize = files.PageSize;
                totalCount = files.TotalCount;
                foreach (var file in files)
                {
                    hits.Add(new SearchHit(
                        file.Title,
                        file.WikiNamespace,
                        file.Domain,
                        Article.GetFullTitle(_options, file.Title, file.WikiNamespace, file.Domain)));
                }
            }
            else if (includedNamespaces.Count == 1
                && includedNamespaces[0] == _options.CategoryNamespace)
            {
                var categories = await GetPageAsync<Category>(
                    request,
                    user,
                    queryEmpty,
                    excludedNamespaces,
                    anyExcludedNamespaces,
                    includedNamespaces,
                    anyIncludedNamespaces,
                    excludedOwners,
                    anyExcludedOwners,
                    includedOwners,
                    anyIncludedOwners);
                pageNumber = categories.PageNumber;
                pageSize = categories.PageSize;
                totalCount = categories.TotalCount;
                foreach (var category in categories)
                {
                    var excerpt = await GetExcerptAsync(request, queryEmpty, category)
                        .ConfigureAwait(false);

                    hits.Add(new SearchHit(
                        category.Title,
                        category.WikiNamespace,
                        category.Domain,
                        Article.GetFullTitle(_options, category.Title, category.WikiNamespace, category.Domain),
                        excerpt));
                }
            }
            else
            {
                var articles = await GetPageAsync<Article>(
                    request,
                    user,
                    queryEmpty,
                    excludedNamespaces,
                    anyExcludedNamespaces,
                    includedNamespaces,
                    anyIncludedNamespaces,
                    excludedOwners,
                    anyExcludedOwners,
                    includedOwners,
                    anyIncludedOwners);
                pageNumber = articles.PageNumber;
                pageSize = articles.PageSize;
                totalCount = articles.TotalCount;
                if (articles.Count == 0
                    && namespaceEmpty)
                {
                    var categories = await GetPageAsync<Category>(
                        request,
                        user,
                        queryEmpty,
                        excludedNamespaces,
                        anyExcludedNamespaces,
                        includedNamespaces,
                        anyIncludedNamespaces,
                        excludedOwners,
                        anyExcludedOwners,
                        includedOwners,
                        anyIncludedOwners);
                    pageNumber = categories.PageNumber;
                    pageSize = categories.PageSize;
                    totalCount = categories.TotalCount;
                    if (categories.Count == 0)
                    {
                        var files = await GetPageAsync<WikiFile>(
                            request,
                            user,
                            queryEmpty,
                            excludedNamespaces,
                            anyExcludedNamespaces,
                            includedNamespaces,
                            anyIncludedNamespaces,
                            excludedOwners,
                            anyExcludedOwners,
                            includedOwners,
                            anyIncludedOwners);
                        pageNumber = files.PageNumber;
                        pageSize = files.PageSize;
                        totalCount = files.TotalCount;
                        foreach (var file in files)
                        {
                            hits.Add(new SearchHit(
                                file.Title,
                                file.WikiNamespace,
                                file.Domain,
                                Article.GetFullTitle(_options, file.Title, file.WikiNamespace, file.Domain)));
                        }
                    }
                    else
                    {
                        foreach (var category in categories)
                        {
                            var excerpt = await GetExcerptAsync(request, queryEmpty, category)
                                .ConfigureAwait(false);

                            hits.Add(new SearchHit(
                                category.Title,
                                category.WikiNamespace,
                                category.Domain,
                                Article.GetFullTitle(_options, category.Title, category.WikiNamespace, category.Domain),
                                excerpt));
                        }
                    }
                }
                else
                {
                    foreach (var article in articles)
                    {
                        var excerpt = await GetExcerptAsync(request, queryEmpty, article)
                            .ConfigureAwait(false);

                        hits.Add(new SearchHit(
                            article.Title,
                            article.WikiNamespace,
                            article.Domain,
                            Article.GetFullTitle(_options, article.Title, article.WikiNamespace, article.Domain),
                            excerpt));
                    }
                }
            }

            return new SearchResult
            {
                Descending = request.Descending,
                Query = request.Query,
                SearchHits = new PagedList<SearchHit>(
                    hits,
                    pageNumber,
                    pageSize,
                    totalCount),
                Sort = request.Sort,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in search for query: {Query}", request.Query);
            return new SearchResult
            {
                Descending = request.Descending,
                Query = request.Query,
                Sort = request.Sort,
            };
        }
    }

    private async Task<string> GetExcerptAsync(ISearchRequest request, bool queryEmpty, Article article)
    {
        var hitIndex = queryEmpty
            ? 0
            : Math.Max(0,
                article.MarkdownContent.IndexOf(
                    request.Query!,
                    StringComparison.OrdinalIgnoreCase));
        var startIndex = hitIndex;
        if (hitIndex > 0)
        {
            // backup to previous space
            startIndex = Math.Max(0,
                article.MarkdownContent.LastIndexOf(
                    ' ',
                    hitIndex) - 1);
            // back up again to prior space
            startIndex = Math.Max(0,
                article.MarkdownContent.LastIndexOf(
                    ' ',
                    startIndex));
        }

        if (queryEmpty)
        {
            hitIndex = Math.Min(
                article.MarkdownContent.Length,
                hitIndex + 128);
        }
        else
        {
            hitIndex = article.MarkdownContent.LastIndexOf(request.Query!) + request.Query!.Length;
            if (hitIndex == -1)
            {
                hitIndex = 0;
            }
            else if (hitIndex > startIndex + 128)
            {
                hitIndex = startIndex + 128;
            }
        }
        // skip ahead to next space
        var endIndex = article.MarkdownContent.IndexOf(
            ' ',
            hitIndex);
        if (endIndex == -1)
        {
            endIndex = hitIndex;
        }
        // skip ahead again to next space
        if (endIndex < article.MarkdownContent.Length - 1)
        {
            var newEndIndex = article.MarkdownContent.IndexOf(
                ' ',
                endIndex + 1);
            if (newEndIndex != -1)
            {
                endIndex = newEndIndex;
            }
        }
        if (endIndex > hitIndex)
        {
            endIndex = Math.Min(
                article.MarkdownContent.Length,
                endIndex + 1);
        }

        var excerpt = await article.GetPlainTextAsync(
            _options,
            _dataStore,
            article.MarkdownContent[startIndex..endIndex])
            .ConfigureAwait(false);
        if (!queryEmpty)
        {
            excerpt = Regex.Replace(
                excerpt,
                $"({Regex.Escape(request.Query!)})",
                "<strong class=\"wiki-search-hit\">$1</strong>",
                RegexOptions.IgnoreCase);
        }
        if (endIndex < article.MarkdownContent.Length)
        {
            excerpt += "...";
        }

        return excerpt;
    }

    private async Task<IPagedList<T>> GetPageAsync<T>(
        ISearchRequest request,
        IWikiUser? user,
        bool queryEmpty,
        List<string> excludedNamespaces,
        bool anyExcludedNamespaces,
        List<string> includedNamespaces,
        bool anyIncludedNamespaces,
        List<string> excludedOwners,
        bool anyExcludedOwners,
        List<string> includedOwners,
        bool anyIncludedOwners) where T : Article
    {
        Expression<Func<T, bool>> exp = x => !x.IsDeleted;

        if (!queryEmpty)
        {
            if (request.TitleMatchOnly)
            {
                exp = exp.AndAlso(x => x.Title.Contains(request.Query!, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                exp = exp.AndAlso(x => x.Title.Contains(request.Query!, StringComparison.OrdinalIgnoreCase)
                    || x.MarkdownContent.Contains(request.Query!, StringComparison.OrdinalIgnoreCase));
            }
        }

        exp = exp.AndAlso(x => x.Domain == request.Domain);

        if (anyIncludedNamespaces)
        {
            exp = exp.AndAlso(x => includedNamespaces.Contains(x.WikiNamespace));
        }
        if (anyExcludedNamespaces)
        {
            exp = exp.AndAlso(x => !excludedNamespaces.Contains(x.WikiNamespace));
        }

        if (anyIncludedOwners)
        {
            exp = exp.AndAlso(x => x.Owner != null && includedOwners.Contains(x.Owner));
        }
        if (anyExcludedOwners)
        {
            exp = exp.AndAlso(x => x.Owner == null || !excludedOwners.Contains(x.Owner));
        }

        if (user is null)
        {
            if (anyIncludedOwners)
            {
                exp = exp.AndAlso(x => x.AllowedEditors == null || x.AllowedViewers == null);
            }
            else
            {
                exp = exp.AndAlso(x => x.Owner == null || x.AllowedEditors == null || x.AllowedViewers == null);
            }
        }
        else if (!user.IsWikiAdmin)
        {
            var groupIds = user.Groups ?? new List<string>();
            if (anyIncludedOwners)
            {
                exp = exp.AndAlso(x => x.AllowedEditors == null
                    || x.AllowedViewers == null
                    || user.Id == x.Owner
                    || x.AllowedEditors.Contains(user.Id)
                    || x.AllowedViewers.Contains(user.Id)
                    || (user.AllowedEditArticles != null
                    && user.AllowedEditArticles.Contains(x.Id))
                    || (user.AllowedViewArticles != null
                    && user.AllowedViewArticles.Contains(x.Id))
                    || groupIds.Contains(x.Owner!)
                    || x.AllowedEditors.Any(y => groupIds.Contains(y))
                    || x.AllowedViewers.Any(y => groupIds.Contains(y)));
            }
            else
            {
                exp = exp.AndAlso(x => x.Owner == null
                    || x.AllowedEditors == null
                    || x.AllowedViewers == null
                    || user.Id == x.Owner
                    || x.AllowedEditors.Contains(user.Id)
                    || x.AllowedViewers.Contains(user.Id)
                    || (user.AllowedEditArticles != null
                    && user.AllowedEditArticles.Contains(x.Id))
                    || (user.AllowedViewArticles != null
                    && user.AllowedViewArticles.Contains(x.Id))
                    || groupIds.Contains(x.Owner)
                    || x.AllowedEditors.Any(y => groupIds.Contains(y))
                    || x.AllowedViewers.Any(y => groupIds.Contains(y)));
            }
        }

        var query = _dataStore.Query<T>().Where(exp);

        if (string.Equals(request.Sort, "timestamp", StringComparison.OrdinalIgnoreCase))
        {
            query = query.OrderBy(x => x.TimestampTicks, request.Descending);
        }
        else if (string.Equals(request.Sort, "title", StringComparison.OrdinalIgnoreCase))
        {
            query = query.OrderBy(x => x.Title, request.Descending);
        }
        else
        {
            query = query
                .OrderBy(x => request.Query != null
                    && x.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : 1,
                    request.Descending)
                .ThenBy(x => request.Query == null
                    ? 1
                    : x.MarkdownContent.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
                        ? 0
                        : 1,
                    request.Descending)
                .ThenBy(x => request.Query == null
                    ? -1
                    : x.MarkdownContent.IndexOf(request.Query, StringComparison.OrdinalIgnoreCase),
                    request.Descending)
                .ThenBy(x => x.Title, request.Descending);
        }

        return await query.GetPageAsync(request.PageNumber, request.PageSize)
            .ConfigureAwait(false);
    }
}
