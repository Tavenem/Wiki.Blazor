namespace Tavenem.Wiki.Blazor.Client.Internal.Models;

/// <summary>
/// Represents a talk message.
/// </summary>
/// <param name="message"></param>
public class TalkMessageModel(MessageResponse message)
{
    /// <summary>
    /// The message.
    /// </summary>
    public MessageResponse Message { get; set; } = message;

    /// <summary>
    /// Any reactions.
    /// </summary>
    public Dictionary<string, List<MessageResponse>>? Reactions { get; set; }

    /// <summary>
    /// Any replies.
    /// </summary>
    public List<TalkMessageModel>? Replies { get; set; }
}
