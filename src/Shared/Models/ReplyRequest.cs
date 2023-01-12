namespace Tavenem.Wiki.Blazor.Models;

/// <summary>
/// A reply made to a topic.
/// </summary>
public class ReplyRequest
{
    /// <summary>
    /// The markdown content of this reply.
    /// </summary>
    public string Markdown { get; set; }

    /// <summary>
    /// The ID of the message to which this reply is addressed (<see langword="null"/> for
    /// messages addressed directly to a topic).
    /// </summary>
    public string? MessageId { get; }

    /// <summary>
    /// The ID of the topic to which this reply has been addressed.
    /// </summary>
    public string TopicId { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="ReplyRequest"/>.
    /// </summary>
    /// <param name="topicId">
    /// The ID of the topic to which this reply has been addressed.
    /// </param>
    /// <param name="markdown">
    /// The markdown content of this reply.
    /// </param>
    /// <param name="messageId">
    /// The ID of the message to which this reply is addressed (<see langword="null"/> for
    /// messages addressed directly to a topic).
    /// </param>
    public ReplyRequest(string topicId, string markdown, string? messageId = null)
    {
        if (string.IsNullOrWhiteSpace(topicId))
        {
            throw new ArgumentNullException(nameof(topicId), $"{nameof(topicId)} cannot be empty");
        }
        TopicId = topicId;
        Markdown = markdown;
        MessageId = messageId;
    }
}
