using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Client.Internal.Models;
using Tavenem.Wiki.Blazor.Client.Services;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The history view.
/// </summary>
public partial class HistoryView
{
    /// <summary>
    /// The requested editor (a filter).
    /// </summary>
    [Parameter] public string? Editor { get; set; }

    /// <summary>
    /// The requested end datetime.
    /// </summary>
    [Parameter] public string? End { get; set; }

    /// <summary>
    /// The requested page number.
    /// </summary>
    [Parameter] public int? PageNumber { get; set; }

    /// <summary>
    /// The requested page size.
    /// </summary>
    [Parameter] public int? PageSize { get; set; }

    /// <summary>
    /// The requested start datetime.
    /// </summary>
    [Parameter] public string? Start { get; set; }

    private ulong CurrentPageNumber { get; set; }

    private IPagedList<RevisionInfo>? Revisions { get; set; }

    private List<IWikiOwner> SelectedEditor { get; set; } = [];

    [Inject, NotNull] private WikiDataService? WikiDataService { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        Revisions = null;
        CurrentPageNumber = (ulong)Math.Max(1, PageNumber ?? 1);

        var request = new HistoryRequest(
            new PageTitle(WikiState.WikiTitle, WikiState.WikiNamespace, WikiState.WikiDomain),
            (int)CurrentPageNumber,
            PageSize ?? 50,
            Editor,
            string.IsNullOrEmpty(Start)
                || !DateTimeOffset.TryParse(Start, out var start)
                ? null
                : start.UtcTicks,
            string.IsNullOrEmpty(End)
                || !DateTimeOffset.TryParse(End, out var end)
                ? null
                : end.UtcTicks);
        var history = await WikiDataService.GetHistoryAsync(request);
        Revisions = history?.Revisions is null
            ? null
            : new PagedList<RevisionInfo>(
                history.Revisions.Items?.Select(x => new RevisionInfo(
                    x,
                    history.Editors?.FirstOrDefault(y => string.Equals(y.Id, x.Editor))
                        ?? new WikiUser { Id = x.Editor })),
                history.Revisions.PageNumber,
                history.Revisions.PageSize,
                history.Revisions.TotalCount);
    }

    private string GetCompareWithCur(RevisionInfo revision) => WikiState.LinkHere(
        query: $"rev={revision.Revision.TimestampTicks}&rev=cur");

    private string GetCompareWithPrev(RevisionInfo revision) => WikiState.LinkHere(
        query: $"rev={revision.Revision.TimestampTicks}&rev=prev");
}