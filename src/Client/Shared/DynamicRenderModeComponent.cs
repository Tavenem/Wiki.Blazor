using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace Tavenem.Wiki.Blazor.Client.Shared;

/// <summary>
/// Dynamically renders a component with a render mode.
/// </summary>
public class DynamicRenderModeComponent : ComponentBase
{
    /// <summary>
    /// Whether the current user has permission to edit this article.
    /// </summary>
    [Parameter] public bool CanEdit { get; set; }

    /// <summary>
    /// The article to display.
    /// </summary>
    [Parameter] public Page? Page { get; set; }

    /// <summary>
    /// The current user (may be null if the current user is browsing anonymously).
    /// </summary>
    [Parameter] public IWikiUser? User { get; set; }

    /// <summary>
    /// The type of component to render.
    /// </summary>
    [Parameter]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? ComponentType { get; set; }

    /// <summary>
    /// The render mode to use, or <see langword="null"/> if static rendering should be used.
    /// </summary>
    [Parameter] public IComponentRenderMode? RenderMode { get; set; }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ComponentType is null)
        {
            return;
        }

        builder.OpenComponent(0, ComponentType);
        builder.AddAttribute(1, "Article", Page);
        builder.AddAttribute(2, "CanEdit", CanEdit);
        builder.AddAttribute(3, "User", User);
        builder.AddComponentRenderMode(RenderMode);
        builder.CloseComponent();
    }
}