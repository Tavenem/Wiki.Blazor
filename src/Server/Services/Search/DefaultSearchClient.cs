using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Tavenem.DataStorage;

namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// <para>
/// The default search client performs a naive search of the <see cref="_dataStore"/>, looking
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
        var namespaceEmpty = string.IsNullOrWhiteSpace(request.WikiNamespace);
        var ownerEmpty = string.IsNullOrWhiteSpace(request.Owner);
        var uploaderEmpty = string.IsNullOrWhiteSpace(request.Uploader);
        if (queryEmpty && namespaceEmpty && ownerEmpty && uploaderEmpty)
        {
            return new SearchResult
            {
                Descending = request.Descending,
                Query = request.Query,
                Sort = request.Sort,
            };
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

        if (anyExcludedNamespaces
            && includedNamespaces.Count == 1
            && includedNamespaces[0] == _options.CategoryNamespace)
        {
            return await SearchCategoryAsync(request, user).ConfigureAwait(false);
        }

        if (anyExcludedNamespaces
            && includedNamespaces.Count == 1
            && includedNamespaces[0] == _options.FileNamespace)
        {
            return await SearchFilesAsync(request, user).ConfigureAwait(false);
        }

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

        if (anyIncludedUploaders)
        {
            return await SearchFilesAsync(request, user).ConfigureAwait(false);
        }

        System.Linq.Expressions.Expression<Func<Article, bool>> exp = x => !x.IsDeleted;

        if (!queryEmpty)
        {
            exp = exp.AndAlso(x => x.Title.Contains(request.Query!, StringComparison.OrdinalIgnoreCase)
                || x.MarkdownContent.Contains(request.Query!, StringComparison.OrdinalIgnoreCase));
        }

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
                    || groupIds.Contains(x.Owner)
                    || x.AllowedEditors.Any(y => groupIds.Contains(y))
                    || x.AllowedViewers.Any(y => groupIds.Contains(y)));
            }
        }

        try
        {
            var query = _dataStore.Query<Article>().Where(exp);
            if (string.Equals(request.Sort, "timestamp", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(x => x.TimestampTicks, request.Descending);
            }
            else
            {
                query = query.OrderBy(x => x.Title, request.Descending);
            }
            var articles = await query.GetPageAsync(request.PageNumber, request.PageSize)
                .ConfigureAwait(false);

            var hits = new List<SearchHit>();
            foreach (var article in articles)
            {
                hits.Add(new SearchHit(
                    article.Title,
                    article.WikiNamespace,
                    Article.GetFullTitle(_options, article.Title, article.WikiNamespace),
                    queryEmpty
                        ? await article.GetPlainTextAsync(_options, _dataStore)
                        : Regex.Replace(
                            await article.GetPlainTextAsync(_options, _dataStore, article.MarkdownContent[
                                Math.Max(0, article.MarkdownContent.LastIndexOf(
                                    ' ',
                                    Math.Max(0, article.MarkdownContent.LastIndexOf(
                                        ' ',
                                        Math.Max(0, article.MarkdownContent.IndexOf(
                                            request.Query!,
                                            StringComparison.OrdinalIgnoreCase))) - 1)))..]),
                            $"({Regex.Escape(request.Query!)})",
                            "<strong class=\"wiki-search-hit\">$1</strong>",
                            RegexOptions.IgnoreCase)));
            }
            var page = new PagedList<SearchHit>(
                hits,
                articles.PageNumber,
                articles.PageSize,
                articles.TotalCount);

            return new SearchResult
            {
                Descending = request.Descending,
                Query = request.Query,
                SearchHits = page,
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

    /// <summary>
    /// Search for wiki categories which matches the given search criteria.
    /// </summary>
    /// <param name="request">
    /// An <see cref="ISearchRequest" /> instance with search criteria.
    /// </param>
    /// <param name="user">
    /// The <see cref="IWikiUser" /> making the request.
    /// </param>
    /// <returns>An <see cref="ISearchResult" /> instance with search results.</returns>
    public async Task<ISearchResult> SearchCategoryAsync(ISearchRequest request, IWikiUser? user)
    {
        var queryEmpty = string.IsNullOrWhiteSpace(request.Query);
        var ownerEmpty = string.IsNullOrWhiteSpace(request.Owner);
        if (queryEmpty && ownerEmpty)
        {
            return new SearchResult
            {
                Descending = request.Descending,
                Query = request.Query,
                Sort = request.Sort,
            };
        }

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

        System.Linq.Expressions.Expression<Func<Category, bool>> exp = x => !x.IsDeleted;

        if (!queryEmpty)
        {
            exp = exp.AndAlso(x => x.Title.Contains(request.Query!, StringComparison.OrdinalIgnoreCase)
                || x.MarkdownContent.Contains(request.Query!, StringComparison.OrdinalIgnoreCase));
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
                    || groupIds.Contains(x.Owner)
                    || x.AllowedEditors.Any(y => groupIds.Contains(y))
                    || x.AllowedViewers.Any(y => groupIds.Contains(y)));
            }
        }

        try
        {
            var query = _dataStore.Query<Category>().Where(exp);
            if (string.Equals(request.Sort, "timestamp", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(x => x.TimestampTicks, request.Descending);
            }
            else
            {
                query = query.OrderBy(x => x.Title, request.Descending);
            }
            var articles = await query.GetPageAsync(request.PageNumber, request.PageSize)
                .ConfigureAwait(false);

            var hits = new List<SearchHit>();
            foreach (var article in articles)
            {
                hits.Add(new SearchHit(
                    article.Title,
                    article.WikiNamespace,
                    Article.GetFullTitle(_options, article.Title, article.WikiNamespace),
                    queryEmpty
                        ? await article.GetPlainTextAsync(_options, _dataStore)
                        : Regex.Replace(
                            await article.GetPlainTextAsync(_options, _dataStore, article.MarkdownContent[
                                Math.Max(0, article.MarkdownContent.LastIndexOf(
                                    ' ',
                                    Math.Max(0, article.MarkdownContent.LastIndexOf(
                                        ' ',
                                        Math.Max(0, article.MarkdownContent.IndexOf(
                                            request.Query!,
                                            StringComparison.OrdinalIgnoreCase))) - 1)))..]),
                            $"({Regex.Escape(request.Query!)})",
                            "<strong class=\"wiki-search-hit\">$1</strong>",
                            RegexOptions.IgnoreCase)));
            }
            var page = new PagedList<SearchHit>(
                hits,
                articles.PageNumber,
                articles.PageSize,
                articles.TotalCount);

            return new SearchResult
            {
                Descending = request.Descending,
                Query = request.Query,
                SearchHits = page,
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
    public async Task<ISearchResult> SearchFilesAsync(ISearchRequest request, IWikiUser? user)
    {
        var queryEmpty = string.IsNullOrWhiteSpace(request.Query);
        var ownerEmpty = string.IsNullOrWhiteSpace(request.Owner);
        var uploaderEmpty = string.IsNullOrWhiteSpace(request.Uploader);
        if (queryEmpty && ownerEmpty && uploaderEmpty)
        {
            return new SearchResult
            {
                Descending = request.Descending,
                Query = request.Query,
                Sort = request.Sort,
            };
        }

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

        System.Linq.Expressions.Expression<Func<WikiFile, bool>> exp = x => !x.IsDeleted;

        if (!queryEmpty)
        {
            exp = exp.AndAlso(x => x.Title.Contains(request.Query!, StringComparison.OrdinalIgnoreCase)
                || x.MarkdownContent.Contains(request.Query!, StringComparison.OrdinalIgnoreCase));
        }

        if (anyIncludedOwners)
        {
            exp = exp.AndAlso(x => x.Owner != null && includedOwners.Contains(x.Owner));
        }
        if (anyExcludedOwners)
        {
            exp = exp.AndAlso(x => x.Owner == null || !excludedOwners.Contains(x.Owner));
        }

        if (anyIncludedUploaders)
        {
            exp = exp.AndAlso(x => x.Uploader != null && includedOwners.Contains(x.Uploader));
        }
        if (anyExcludedUploaders)
        {
            exp = exp.AndAlso(x => x.Uploader == null || !excludedOwners.Contains(x.Uploader));
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
                    || groupIds.Contains(x.Owner)
                    || x.AllowedEditors.Any(y => groupIds.Contains(y))
                    || x.AllowedViewers.Any(y => groupIds.Contains(y)));
            }
        }

        try
        {
            var query = _dataStore.Query<WikiFile>().Where(exp);
            if (string.Equals(request.Sort, "timestamp", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(x => x.TimestampTicks, request.Descending);
            }
            else
            {
                query = query.OrderBy(x => x.Title, request.Descending);
            }
            var articles = await query.GetPageAsync(request.PageNumber, request.PageSize)
                .ConfigureAwait(false);

            var hits = new List<SearchHit>();
            foreach (var article in articles)
            {
                hits.Add(new SearchHit(
                    article.Title,
                    article.WikiNamespace,
                    Article.GetFullTitle(_options, article.Title, article.WikiNamespace),
                    queryEmpty
                        ? await article.GetPlainTextAsync(_options, _dataStore)
                        : Regex.Replace(
                            await article.GetPlainTextAsync(_options, _dataStore, article.MarkdownContent[
                                Math.Max(0, article.MarkdownContent.LastIndexOf(
                                    ' ',
                                    Math.Max(0, article.MarkdownContent.LastIndexOf(
                                        ' ',
                                        Math.Max(0, article.MarkdownContent.IndexOf(
                                            request.Query!,
                                            StringComparison.OrdinalIgnoreCase))) - 1)))..]),
                            $"({Regex.Escape(request.Query!)})",
                            "<strong class=\"wiki-search-hit\">$1</strong>",
                            RegexOptions.IgnoreCase)));
            }
            var page = new PagedList<SearchHit>(
                hits,
                articles.PageNumber,
                articles.PageSize,
                articles.TotalCount);

            return new SearchResult
            {
                Descending = request.Descending,
                Query = request.Query,
                SearchHits = page,
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
}
