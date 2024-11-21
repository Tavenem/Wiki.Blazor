using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// The default wiki layout.
/// </summary>
public partial class WikiLayout : LayoutComponentBase, IDisposable
{
    private bool _disposedValue;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private Type ResolvedCompactLayout => WikiBlazorClientOptions.CompactLayout ?? typeof(CompactLayout);

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private Type ResolvedMainLayout => WikiBlazorClientOptions.MainLayout ?? typeof(MainLayout);

    [Inject, NotNull] private WikiBlazorOptions? WikiBlazorClientOptions { get; set; }

    [Inject, NotNull] private WikiState? WikiState { get; set; }

    /// <inheritdoc/>
    protected override void OnInitialized() => WikiState.CompactChanged += CompactChanged;

    /// <inheritdoc/>
    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2111:RequiresUnreferencedCode",
        Justification = "OpenComponent already has the right set of attributes")]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<LayoutView>(0);
        builder.AddComponentParameter(
            1,
            nameof(LayoutView.Layout),
            WikiState.IsCompact
                ? ResolvedCompactLayout
                : ResolvedMainLayout);
        builder.AddComponentParameter(2, nameof(LayoutView.ChildContent), Body);
        builder.CloseComponent();
    }

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