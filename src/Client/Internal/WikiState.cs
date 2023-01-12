using System.Diagnostics.CodeAnalysis;

namespace Tavenem.Wiki.Blazor.Client;

internal class WikiState
{
    private readonly WikiOptions _wikiOptions;

    public bool DefaultNamespace { get; set; }

    private string? _displayTitle;
    [AllowNull]
    public string DisplayTitle
    {
        get => _displayTitle ?? WikiTitle ?? _wikiOptions.MainPageTitle;
        set => _displayTitle = value;
    }

    public bool IsCompact { get; set; }

    public bool IsSystem { get; set; }

    public bool IsTalk { get; set; }

    public bool LoadError { get; set; }

    public bool NotAuthorized { get; set; }

    public string PageTitle { get; private set; }

    public string? WikiDomain { get; set; }

    public string? WikiNamespace { get; set; }

    public string? WikiTitle { get; set; }

    public event EventHandler<bool>? CompactChanged;

    public WikiState(WikiOptions wikiOptions)
    {
        _wikiOptions = wikiOptions;
        UpdateTitle();
    }

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

    public void SetIsCompact(bool value)
    {
        IsCompact = value;
        CompactChanged?.Invoke(this, value);
    }

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
}
