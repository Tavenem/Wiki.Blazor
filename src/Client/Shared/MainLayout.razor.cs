using Microsoft.AspNetCore.Components;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// <para>
/// The default main layout for the <see cref="Wiki"/> component.
/// </para>
/// <para>
/// This is used if you do not specify your own layout in <see cref="IWikiBlazorClientOptions"/>.
/// </para>
/// </summary>
public partial class MainLayout
{
    [Inject] private IWikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    private RenderFragment? AppBarRender { get; set; }

    /// <inheritdoc/>
    protected override void OnInitialized() => AppBarRender = AppBarRenderer();

    private RenderFragment AppBarRenderer() => builder =>
    {
        if (WikiBlazorClientOptions.AppBar is not null)
        {
            builder.OpenComponent(0, WikiBlazorClientOptions.AppBar);
            builder.CloseComponent();
        }
    };
}