namespace Tavenem.Wiki.Blazor.Models;

/// <summary>
/// The preview request object.
/// </summary>
public record PreviewRequest(
    string Content,
    string? Title = null,
    string? WikiNamespace = null);
