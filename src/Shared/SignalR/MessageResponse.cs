namespace Tavenem.Wiki.Blazor.SignalR;

/// <summary>
/// A compact form of <see cref="Message"/> suitable for SignalR transport.
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
    /// Whether the sender of this message exists.
    /// </summary>
    public bool SenderExists { get; set; }

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
    /// Whether the sender of this message has a wiki user page.
    /// </summary>
    public bool SenderPageExists { get; set; }

    /// <summary>
    /// The timestamp when this message was sent, in UTC Ticks.
    /// </summary>
    public long TimestampTicks { get; set; }

    /// <summary>
    /// The ID of the topic to which this message was addressed.
    /// </summary>
    public string TopicId { get; set; }

    /// <summary>
    /// Initialize a new instance of <see cref="MessageResponse"/>.
    /// </summary>
    public MessageResponse()
    {
        Id = string.Empty;
        Content = string.Empty;
        SenderId = string.Empty;
        SenderName = string.Empty;
        TopicId = string.Empty;
    }

    /// <summary>
    /// Initialize a new instance of <see cref="MessageResponse"/>.
    /// </summary>
    /// <param name="message">The <see cref="Message"/>.</param>
    /// <param name="html">
    /// The HTML content of the message.
    /// </param>
    /// <param name="senderExists">
    /// <see langword="true"/> if the sender of the message still exists as a wiki user.
    /// </param>
    /// <param name="senderPageExists">
    /// <see langword="true"/> if the sender of the message has a wiki user page.
    /// </param>
    public MessageResponse(Message message, string html, bool senderExists, bool senderPageExists)
    {
        Content = html;
        Id = message.Id;
        ReplyMessageId = message.ReplyMessageId;
        SenderExists = senderExists;
        SenderId = message.SenderId;
        SenderIsAdmin = message.SenderIsAdmin;
        SenderName = message.SenderName;
        SenderPageExists = senderPageExists;
        TimestampTicks = message.TimestampTicks;
        TopicId = message.TopicId;
    }
}
