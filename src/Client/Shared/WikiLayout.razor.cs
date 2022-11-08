using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// The default wiki layout.
/// </summary>
public partial class WikiLayout : IDisposable
{
    private bool _disposedValue;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private Type ResolvedCompactLayout => WikiBlazorClientOptions.CompactLayout ?? typeof(CompactLayout);

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private Type ResolvedMainLayout => WikiBlazorClientOptions.MainLayout ?? typeof(MainLayout);

    [Inject] private WikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnInitialized() => WikiState.CompactChanged += CompactChanged;

    private void CompactChanged(object? sender, bool e) => StateHasChanged();

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                WikiState.CompactChanged -= CompactChanged;
            }

            _disposedValue = true;
        }
    }
}