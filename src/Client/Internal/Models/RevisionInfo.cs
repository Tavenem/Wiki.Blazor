namespace Tavenem.Wiki.Blazor.Client.Internal.Models;

/// <summary>
/// Information about a revision.
/// </summary>
public record RevisionInfo(Revision Revision, IWikiUser Editor)
{
    /// <summary>
    /// The display name for the editor of this revision. 
    /// </summary>
    public string EditorName => Editor.DisplayName ?? Editor.Id ?? Revision.Editor;
}
