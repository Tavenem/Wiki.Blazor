using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;
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

    [CascadingParameter] private bool IsInteractive { get; set; }

    [Inject, NotNull] private IJSRuntime? JSRuntime { get; set; }

    [Inject, NotNull] private QueryStateService? QueryStateService { get; set; }

    private IPagedList<RevisionInfo>? Revisions { get; set; }

    private long? SecondRevision { get; set; }

    private List<IWikiOwner> SelectedEditor { get; set; } = [];

    private TimeSpan TimezoneOffset { get; set; }

    private UserSelector? UserSelector { get; set; }

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
        => RefreshAsync();

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        var pageSizes = QueryStateService.RegisterProperty(
            "pg",
            "ps",
            OnPageSizeChangedAsync,
            50);
        if (pageSizes?.Count > 0
            && int.TryParse(pageSizes[0], out var pageSize))
        {
            CurrentPageSize = Math.Clamp(pageSize, 5, 500);
        }

        var starts = QueryStateService.RegisterProperty(
            "h",
            "s",
            OnStartChangedAsync);
        if (starts?.Count > 0
            && long.TryParse(starts[0], out var start))
        {
            CurrentStart = new DateTimeOffset(start, TimeSpan.Zero);
        }

        var ends = QueryStateService.RegisterProperty(
            "h",
            "e",
            OnEndChangedAsync,
            false);
        if (ends?.Count > 0
            && long.TryParse(ends[0], out var end))
        {
            CurrentEnd = new DateTimeOffset(end, TimeSpan.Zero);
        }

        var editors = QueryStateService.RegisterProperty(
            "h",
            "ed",
            OnEditorChangedAsync,
            false);
        if (editors?.Count > 0)
        {
            Editor = editors[0];
        }
    }

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
        CurrentEnd = End.HasValue
            ? new DateTimeOffset(End.Value, TimeSpan.Zero)
            : null;
        CurrentPageNumber = (ulong)Math.Max(0, PageNumber ?? 1);
        CurrentPageSize = PageSize ?? 50;
        CurrentStart = Start.HasValue
            ? new DateTimeOffset(Start.Value, TimeSpan.Zero)
            : null;

        var request = new HistoryRequest(
            new PageTitle(WikiState.WikiTitle, WikiState.WikiNamespace, WikiState.WikiDomain),
            (int)CurrentPageNumber,
            PageSize ?? 50,
            Editor,
            Start,
            End);
        var history = await PostAsync(
            $"{WikiBlazorClientOptions.WikiServerApiRoute}/history",
            request,
            WikiJsonSerializerContext.Default.HistoryRequest,
            WikiJsonSerializerContext.Default.PagedRevisionInfo,
            user => WikiDataManager.GetHistoryAsync(user, request));
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

    private async Task OnEditorChangedAsync(QueryChangeEventArgs args)
    {
        if (UserSelector is not null
            && !string.Equals(args.Value, Editor))
        {
            Editor = args.Value;
            var editor = string.IsNullOrEmpty(args.Value)
                ? null
                : await UserSelector.OnAddUserAsync(args.Value);
            SelectedEditor = editor is null
                ? []
                : [editor];
            await RefreshAsync();
        }
    }

    private async Task OnEndChangedAsync(QueryChangeEventArgs args)
    {
        if (long.TryParse(args.Value, out var end)
            && end != CurrentEnd?.Ticks)
        {
            CurrentEnd = new DateTimeOffset(end, TimeSpan.Zero);
            End = end;
            await RefreshAsync();
        }
    }

    private async Task OnFilterAsync()
    {
        Editor = SelectedEditor.FirstOrDefault()?.Id;
        End = CurrentEnd?.UtcTicks;
        Start = CurrentStart?.UtcTicks;

        QueryStateService.SetPropertyValue(
            "h",
            "ed",
            Editor);

        QueryStateService.SetPropertyValue(
            "h",
            "e",
            End);

        QueryStateService.SetPropertyValue(
            "h",
            "s",
            Start);

        await RefreshAsync();
    }

    private async Task OnNextRequestedAsync()
    {
        if (Revisions?.HasNextPage == true)
        {
            CurrentPageNumber++;
            PageNumber = (int)CurrentPageNumber;
            await RefreshAsync();
        }
    }

    private async Task OnPageNumberChangedAsync()
    {
        PageNumber = (int)CurrentPageNumber;
        await RefreshAsync();
    }

    private async Task OnPageSizeChangedAsync()
    {
        PageSize = CurrentPageSize;

        QueryStateService.SetPropertyValue(
            "pg",
            "ps",
            CurrentPageSize);

        await RefreshAsync();
    }

    private async Task OnPageSizeChangedAsync(QueryChangeEventArgs args)
    {
        if (int.TryParse(args.Value, out var pageSize)
            && pageSize != CurrentPageSize)
        {
            CurrentPageSize = Math.Clamp(pageSize, 5, 500);
            PageSize = CurrentPageSize;
            await RefreshAsync();
        }
    }

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

    private async Task OnStartChangedAsync(QueryChangeEventArgs args)
    {
        if (long.TryParse(args.Value, out var start)
            && start != CurrentStart?.Ticks)
        {
            CurrentStart = new DateTimeOffset(start, TimeSpan.Zero);
            Start = start;
            await RefreshAsync();
        }
    }
}