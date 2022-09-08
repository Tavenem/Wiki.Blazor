using Tavenem.Wiki.Blazor.SignalR;

namespace Tavenem.Wiki.Blazor.Client.Internal.Models;

/// <summary>
/// Represents a talk message.
/// </summary>
public class TalkMessageModel
{
    /// <summary>
    /// The message.
    /// </summary>
    public MessageResponse Message { get; set; }

    /// <summary>
    /// Any reactions.
    /// </summary>
    public Dictionary<string, List<MessageResponse>>? Reactions { get; set; }

    /// <summary>
    /// Any replies.
    /// </summary>
    public List<TalkMessageModel>? Replies { get; set; }

    /// <summary>
    /// Constructs a new instance of <see cref="TalkMessageModel"/>.
    /// </summary>
    /// <param name="message"></param>
    public TalkMessageModel(MessageResponse message) => Message = message;
}
