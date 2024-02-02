namespace Tavenem.Wiki.Blazor.Services.Search;

/// <summary>
/// An object with search criteria.
/// </summary>
public interface ISearchRequest
{
    /// <summary>
    /// Whether to sort in descending order (rather than ascending).
    /// </summary>
    bool Descending { get; set; }

    /// <summary>
    /// An optional domain within which to search.
    /// </summary>
    /// <remarks>
    /// Only one domain may be searched at a time.
    /// </remarks>
    string? Domain { get; set; }

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
    string? Namespace { get; set; }

    /// <summary>
    /// The current page number in a list of results (1-based).
    /// </summary>
    int PageNumber { get; set; }

    /// <summary>
    /// The number of results per page.
    /// </summary>
    int PageSize { get; set; }

    /// <summary>
    /// The search query text.
    /// </summary>
    string? Query { get; set; }

    /// <summary>
    /// <para>
    /// The field by which to sort results.
    /// </para>
    /// <para>
    /// Note: not all fields may be supported.
    /// </para>
    /// </summary>
    string? Sort { get; set; }

    /// <summary>
    /// <para>
    /// An optional owner or owners for whom to restrict results, or to exclude.
    /// </para>
    /// <para>
    /// Each entry should be a user or group ID. May be a semicolon-delimited list, and any entry
    /// may be prefixed with an exclamation mark to indicate that it should be excluded.
    /// </para>
    /// </summary>
    string? Owner { get; set; }

    /// <summary>
    /// Whether the query string should only consider matches int he title, rather than in the
    /// content of the article as well.
    /// </summary>
    bool TitleMatchOnly { get; set; }

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
    string? Uploader { get; set; }
}
