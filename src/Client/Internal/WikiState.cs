using System.Diagnostics.CodeAnalysis;

namespace Tavenem.Wiki.Blazor.Client;

/// <summary>
/// The state of the current wiki view.
/// </summary>
public class WikiState
{
    private readonly WikiOptions _wikiOptions;

    /// <summary>
    /// Whether the namespace of the current page is the default namespace.
    /// </summary>
    public bool DefaultNamespace { get; internal set; }

    private string? _displayTitle;
    /// <summary>
    /// The display title of the current page.
    /// </summary>
    [AllowNull]
    public string DisplayTitle
    {
        get => _displayTitle ?? WikiTitle ?? _wikiOptions.MainPageTitle;
        internal set => _displayTitle = value;
    }

    /// <summary>
    /// Whether the wiki is in compact view mode.
    /// </summary>
    public bool IsCompact { get; internal set; }

    /// <summary>
    /// Whether the current page is a special system page.
    /// </summary>
    public bool IsSystem { get; internal set; }

    /// <summary>
    /// Whether the current page's talk view is displayed.
    /// </summary>
    public bool IsTalk { get; internal set; }

    /// <summary>
    /// Whether an error occurred while loading the current content.
    /// </summary>
    public bool LoadError { get; internal set; }

    private bool _notAuthorized;
    /// <summary>
    /// <para>
    /// Whether the current user is not authorized to complete the current action (e.g. view or edit
    /// a page).
    /// </para>
    /// <para>
    /// This value can be set to <see langword="true"/> externally, but can only be set to <see langword="false"/> by
    /// the library.
    /// </para>
    /// </summary>
    public bool NotAuthorized
    {
        get => _notAuthorized;
        set => _notAuthorized |= value;
    }

    /// <summary>
    /// The full title of the current wiki page, as a string.
    /// </summary>
    public string PageTitle { get; private set; }

    /// <summary>
    /// The current user (if non-anonymous).
    /// </summary>
    public WikiUser? User { get; internal set; }

    /// <summary>
    /// The domain of the current wiki page.
    /// </summary>
    public string? WikiDomain { get; internal set; }

    /// <summary>
    /// The namespace of the current wiki page.
    /// </summary>
    public string? WikiNamespace { get; internal set; }

    /// <summary>
    /// The title of the current wiki page.
    /// </summary>
    public string? WikiTitle { get; internal set; }

    /// <summary>
    /// Raised when the compact view is turned on or off.
    /// </summary>
    public event EventHandler<bool>? CompactChanged;

    /// <summary>
    /// Constructs a new instance of <see cref="WikiState"/>.
    /// </summary>
    /// <param name="wikiOptions">A <see cref="WikiOptions"/> instance.</param>
    public WikiState(WikiOptions wikiOptions)
    {
        _wikiOptions = wikiOptions;
        UpdateTitle();
    }

    /// <summary>
    /// Gets a link to a wiki page.
    /// </summary>
    /// <param name="title">The page's title.</param>
    /// <param name="query">Any custom query string to be appended.</param>
    /// <param name="route">Any custom route to be appended.</param>
    /// <returns>A relative URL for the current page.</returns>
    public string Link(
        PageTitle title,
        string? query = null,
        string? route = null)
    {
        if (IsCompact)
        {
            query = string.IsNullOrEmpty(query)
                ? "compact=true"
                : "compact=true&" + query;
        }
        return _wikiOptions.GetWikiPageUrl(title, route, query);
    }

    /// <summary>
    /// Gets a link to a wiki page.
    /// </summary>
    /// <param name="title">The page's title.</param>
    /// <param name="namespace">The page's namespace.</param>
    /// <param name="domain">The page's domain.</param>
    /// <param name="query">Any custom query string to be appended.</param>
    /// <param name="route">Any custom route to be appended.</param>
    /// <returns>A relative URL for the current page.</returns>
    public string Link(
        string? title = null,
        string? @namespace = null,
        string? domain = null,
        string? query = null,
        string? route = null)
    {
        if (IsCompact)
        {
            query = string.IsNullOrEmpty(query)
                ? "compact=true"
                : "compact=true&" + query;
        }
        return _wikiOptions.GetWikiPageUrl(new PageTitle(title, @namespace, domain), route, query);
    }

    /// <summary>
    /// Gets a link to the current wiki page.
    /// </summary>
    /// <param name="talk">Whether a link to the talk page should be generated.</param>
    /// <param name="edit">Whether a link to the edit page should be generated.</param>
    /// <param name="history">Whether a link to the history page should be generated.</param>
    /// <param name="whatLinksHere">
    /// Whether a link to the special "what links here" page should be generated.
    /// </param>
    /// <param name="query">Any custom query string to be appended.</param>
    /// <returns>A relative URL for the current page.</returns>
    public string LinkHere(
        bool talk = false,
        bool edit = false,
        bool history = false,
        bool whatLinksHere = false,
        string? query = null)
    {
        if (IsCompact)
        {
            query = string.IsNullOrEmpty(query)
                ? "compact=true"
                : "compact=true&" + query;
        }
        string? route = null;
        if (talk)
        {
            route = "Talk";
        }
        else if (edit)
        {
            route = "Edit";
        }
        else if (history)
        {
            route = "History";
        }
        else if (whatLinksHere)
        {
            route = "WhatLinksHere";
        }
        return _wikiOptions.GetWikiPageUrl(
            new PageTitle(WikiTitle, WikiNamespace, WikiDomain),
            route,
            query);
    }

    /// <summary>
    /// Gets the current <see cref="Tavenem.Wiki.PageTitle"/>.
    /// </summary>
    public PageTitle GetCurrentPageTitle() => new(WikiTitle, WikiNamespace, WikiDomain);

    /// <summary>
    /// Updates the display title of the current page.
    /// </summary>
    /// <param name="displayTitle">The new display title.</param>
    [MemberNotNull(nameof(PageTitle))]
    public void UpdateTitle(string? displayTitle = null)
    {
        DisplayTitle = displayTitle;
        if (string.IsNullOrEmpty(DisplayTitle))
        {
            PageTitle = _wikiOptions.SiteName;
        }
        else
        {
            PageTitle = new PageTitle(
                DisplayTitle,
                WikiNamespace,
                WikiDomain)
                .ToString();
        }
    }

    internal void SetIsCompact(bool value)
    {
        IsCompact = value;
        CompactChanged?.Invoke(this, value);
    }
}
