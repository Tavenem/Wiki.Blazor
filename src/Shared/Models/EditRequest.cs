namespace Tavenem.Wiki.Blazor.Models;

/// <summary>
/// The wiki edit request object.
/// </summary>
public record EditRequest(
    PageTitle Title,
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
    PageTitle? OriginalTitle = null);
