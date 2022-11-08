using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Client.Internal.Models;
using Tavenem.Wiki.Blazor.Client.Shared;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The history view.
/// </summary>
public partial class HistoryView : OfflineSupportComponent, IAsyncDisposable
{
    private bool _disposedValue;
    private IJSObjectReference? _module;

    /// <summary>
    /// The requested editor (a filter).
    /// </summary>
    [Parameter] public string? Editor { get; set; }

    /// <summary>
    /// The requested end datetime (as a number of ticks, in UTC).
    /// </summary>
    [Parameter] public long? End { get; set; }

    /// <summary>
    /// The requested page number.
    /// </summary>
    [Parameter] public int? PageNumber { get; set; }

    /// <summary>
    /// The requested page size.
    /// </summary>
    [Parameter] public int? PageSize { get; set; }

    /// <summary>
    /// The requested start datetime (as a number of ticks, in UTC).
    /// </summary>
    [Parameter] public long? Start { get; set; }

    private DateTimeOffset? CurrentEnd { get; set; }

    private ulong CurrentPageNumber { get; set; }

    private int CurrentPageSize { get; set; } = 50;

    private DateTimeOffset? CurrentStart { get; set; }

    private long? FirstRevision { get; set; }

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private IPagedList<RevisionInfo>? Revisions { get; set; }

    private long? SecondRevision { get; set; }

    private List<WikiUserInfo> SelectedEditor { get; set; } = new();

    private TimeSpan TimezoneOffset { get; set; }

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
        => RefreshAsync();

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        _module = await JSRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./_content/Tavenem.Wiki.Blazor.Client/tavenem-timezone.js");

        var offset = await _module.InvokeAsync<int>("getTimezoneOffset");
        TimezoneOffset = TimeSpan.FromMinutes(offset);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Do not change this code. Put cleanup code in 'DisposeAsync(bool disposing)' method
        await DisposeAsync(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (_module is not null)
                {
                    await _module.DisposeAsync();
                }
            }

            _disposedValue = true;
        }
    }

    /// <inheritdoc/>
    protected override async Task RefreshAsync()
    {
        Revisions = null;

        var request = new HistoryRequest(
            WikiState.WikiTitle,
            WikiState.WikiNamespace,
            WikiState.WikiDomain,
            PageNumber ?? 1,
            PageSize ?? 50,
            Editor,
            Start,
            End);
        var history = await PostAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/history",
            request,
            WikiBlazorJsonSerializerContext.Default.HistoryRequest,
            WikiBlazorJsonSerializerContext.Default.PagedRevisionInfo,
            user => WikiDataManager.GetHistoryAsync(user, request));
        Revisions = history?.Revisions is null
            ? null
            : new PagedList<RevisionInfo>(
                history.Revisions.List.Select(x => new RevisionInfo(
                    x,
                    history.Editors?.FirstOrDefault(y => string.Equals(y.Id, x.Editor))
                        ?? new WikiUserInfo(x.Editor, null, false))),
                history.Revisions.PageNumber,
                history.Revisions.PageSize,
                history.Revisions.TotalCount);
    }

    private string GetCompareWithCur(RevisionInfo revision) => WikiState.LinkHere(
        query: $"rev={revision.Revision.TimestampTicks}&diff=cur");

    private string GetCompareWithPrev(RevisionInfo revision) => WikiState.LinkHere(
        query: $"rev{revision.Revision.TimestampTicks}&diff=prev");

    private void OnCompare()
    {
        if (!FirstRevision.HasValue
            || !SecondRevision.HasValue)
        {
            return;
        }

        if (SecondRevision > FirstRevision)
        {
            (FirstRevision, SecondRevision) = (SecondRevision, FirstRevision);
        }

        NavigationManager.NavigateTo(WikiState.LinkHere(
            query: $"rev={FirstRevision.Value}&diff={SecondRevision.Value}"));
    }

    private void OnFilter() => NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters(
        new Dictionary<string, object?>
        {
            { nameof(Wiki.Editor), SelectedEditor.FirstOrDefault()?.Id },
            { nameof(Wiki.End), CurrentEnd },
            { nameof(Wiki.Start), CurrentStart },
        }));

    private void OnNextRequested()
    {
        if (Revisions?.HasNextPage == true)
        {
            CurrentPageNumber++;
            NavigationManager.NavigateTo(
                NavigationManager.GetUriWithQueryParameter(
                    nameof(Wiki.PageNumber),
                    (int)(CurrentPageNumber + 1)));
        }
    }

    private void OnPageNumberChanged() => NavigationManager.NavigateTo(
        NavigationManager.GetUriWithQueryParameter(
            nameof(Wiki.PageNumber),
            (int)(CurrentPageNumber + 1)));

    private void OnPageSizeChanged() => NavigationManager.NavigateTo(
        NavigationManager.GetUriWithQueryParameter(
            nameof(Wiki.PageSize),
            CurrentPageSize));

    private void OnSelectRevision(RevisionInfo revision, bool value)
    {
        if (value)
        {
            if (SecondRevision.HasValue)
            {
                FirstRevision = revision.Revision.TimestampTicks;
                (FirstRevision, SecondRevision) = (SecondRevision, FirstRevision);
            }
            else
            {
                SecondRevision = revision.Revision.TimestampTicks;
            }
        }
        else if (SecondRevision == revision.Revision.TimestampTicks)
        {
            SecondRevision = null;
        }
        else if (FirstRevision == revision.Revision.TimestampTicks)
        {
            FirstRevision = null;
            if (SecondRevision.HasValue)
            {
                FirstRevision = SecondRevision;
                SecondRevision = null;
            }
        }
    }
}