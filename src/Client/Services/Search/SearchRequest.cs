namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// An object with search criteria.
/// </summary>
public class SearchRequest : ISearchRequest
{
    /// <summary>
    /// Whether to sort in descending order (rather than ascending).
    /// </summary>
    public bool Descending { get; set; }

    /// <summary>
    /// An optional domain within which to search.
    /// </summary>
    /// <remarks>
    /// Only one domain may be searched at a time.
    /// </remarks>
    public string? Domain { get; set; }

    /// <summary>
    /// <para>
    /// An optional wiki namespace within which to restrict results, or to exclude.
    /// </para>
    /// <para>
    /// Each entry should already be in correct wiki title case (i.e. searching may be exact,
    /// and can disregard case-insensitive matches). May be a semicolon-delimited list, and any
    /// entry may be prefixed with an exclamation mark to indicate that it should be excluded.
    /// </para>
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// <para>
    /// The current page number in a list of results (1-based).
    /// </para>
    /// <para>
    /// Defaults to 1.
    /// </para>
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// <para>
    /// The number of results per page.
    /// </para>
    /// <para>
    /// Defaults to 50.
    /// </para>
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// The search query text.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// <para>
    /// The field by which to sort results.
    /// </para>
    /// <para>
    /// Note: not all fields may be supported.
    /// </para>
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// <para>
    /// An optional owner or owners for whom to restrict results, or to exclude.
    /// </para>
    /// <para>
    /// Each entry should be a user or group ID. May be a semicolon-delimited list, and any entry
    /// may be prefixed with an exclamation mark to indicate that it should be excluded.
    /// </para>
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Whether the query string should only consider matches int he title, rather than in the
    /// content of the article as well.
    /// </summary>
    public bool TitleMatchOnly { get; set; }

    /// <summary>
    /// <para>
    /// An optional uploader or uploaders for whom to restrict results, or to exclude.
    /// </para>
    /// <para>
    /// Each entry should be a user ID. May be a semicolon-delimited list, and any entry may be
    /// prefixed with an exclamation mark to indicate that it should be excluded.
    /// </para>
    /// <para>
    /// Only applies to <see cref="WikiFile"/>s.
    /// </para>
    /// </summary>
    public string? Uploader { get; set; }
}
