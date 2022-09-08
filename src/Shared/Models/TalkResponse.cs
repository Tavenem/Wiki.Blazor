using Tavenem.Wiki.Blazor.SignalR;

namespace Tavenem.Wiki.Blazor.Models;

/// <summary>
/// The result of a request for a talk page.
/// </summary>
public record TalkResponse(
    IList<MessageResponse>? Messages,
    string? TopicId);
