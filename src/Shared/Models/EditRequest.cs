namespace Tavenem.Wiki.Blazor.Models;

/// <summary>
/// The wiki edit request object.
/// </summary>
public record EditRequest(
    string? Title = null,
    string? WikiNamespace = null,
    string? Domain = null,
    string? Markdown = null,
    string? RevisionComment = null,
    bool IsDeleted = false,
    bool LeaveRedirect = false,
    bool OwnerSelf = false,
    string? Owner = null,
    bool EditorSelf = false,
    bool ViewerSelf = false,
    IList<string>? AllowedEditors = null,
    IList<string>? AllowedViewers = null,
    IList<string>? AllowedEditorGroups = null,
    IList<string>? AllowedViewerGroups = null,
    string? OriginalTitle = null,
    string? OriginalWikiNamespace = null,
    string? OriginalDomain = null);
