namespace Tavenem.Wiki.Blazor.Models;

/// <summary>
/// The wiki upload request object.
/// </summary>
public record UploadRequest(
    PageTitle Title,
    string? Markdown = null,
    bool OverwriteConfirmed = false,
    string? RevisionComment = null,
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
