namespace Tavenem.Wiki.Blazor.Models;

/// <summary>
/// A <see cref="Message"/> with its rendered content.
/// </summary>
public class MessageResponse
{
    /// <summary>
    /// The HTML content of the message.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// The ID of this message.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The ID of the message to which this reply is addressed (<see langword="null"/> for
    /// messages addressed directly to a topic).
    /// </summary>
    public string? ReplyMessageId { get; }

    /// <summary>
    /// The ID of the sender of this message.
    /// </summary>
    public string SenderId { get; set; }

    /// <summary>
    /// Whether the sender of this message is an admin.
    /// </summary>
    public bool SenderIsAdmin { get; set; }

    /// <summary>
    /// The name of the sender of this message.
    /// </summary>
    public string SenderName { get; set; }

    /// <summary>
    /// The timestamp when this message was sent, in UTC Ticks.
    /// </summary>
    public long TimestampTicks { get; set; }

    /// <summary>
    /// Initialize a new instance of <see cref="MessageResponse"/>.
    /// </summary>
    public MessageResponse()
    {
        Id = string.Empty;
        Content = string.Empty;
        SenderId = string.Empty;
        SenderName = string.Empty;
    }

    /// <summary>
    /// Initialize a new instance of <see cref="MessageResponse"/>.
    /// </summary>
    /// <param name="message">The <see cref="Message"/>.</param>
    /// <param name="html">
    /// The HTML content of the message.
    /// </param>
    public MessageResponse(Message message, string html)
    {
        Content = html;
        Id = message.Id;
        ReplyMessageId = message.ReplyMessageId;
        SenderId = message.SenderId;
        SenderIsAdmin = message.SenderIsAdmin;
        SenderName = message.SenderName;
        TimestampTicks = message.TimestampTicks;
    }
}
