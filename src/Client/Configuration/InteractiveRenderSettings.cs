using Microsoft.AspNetCore.Components;

namespace Tavenem.Wiki.Blazor.Client;

/// <summary>
/// Controls the interactive render mode used by interactive components.
/// </summary>
public static class InteractiveRenderSettings
{
    /// <summary>
    /// The interactive render mode used by interactive components.
    /// </summary>
    public static IComponentRenderMode? InteractiveRenderMode { get; set; }
}
