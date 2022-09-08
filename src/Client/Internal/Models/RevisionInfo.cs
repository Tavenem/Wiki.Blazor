using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Internal.Models;

/// <summary>
/// Information about a revision.
/// </summary>
public record RevisionInfo(Revision Revision, WikiUserInfo Editor);
