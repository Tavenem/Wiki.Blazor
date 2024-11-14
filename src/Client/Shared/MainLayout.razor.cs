using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// <para>
/// The default main layout for the <see cref="Wiki"/> component.
/// </para>
/// <para>
/// This is used if you do not specify your own layout in <see cref="WikiBlazorClientOptions"/>.
/// </para>
/// </summary>
public partial class MainLayout
{
    [Inject, NotNull] private WikiBlazorOptions? WikiBlazorClientOptions { get; set; }

    [Inject, NotNull] private WikiOptions? WikiOptions { get; set; }

    private RenderFragment? AppBarRender { get; set; }

    /// <inheritdoc/>
    protected override void OnInitialized() => AppBarRender = AppBarRenderer();

    private RenderFragment AppBarRenderer() => builder =>
    {
        if (WikiBlazorClientOptions.AppBar is not null)
        {
            builder.OpenComponent(0, WikiBlazorClientOptions.AppBar);
            builder.AddComponentRenderMode(WikiBlazorClientOptions.AppBarRenderMode);
            builder.CloseComponent();
        }
    };
}