namespace Tavenem.Wiki.Blazor.Exceptions;

/// <summary>
/// Indicates that a user attempted to create content which already exists.
/// </summary>
public class WikiConflictException : Exception
{
    /// <summary>
    /// Constructs a new instance of <see cref="WikiConflictException"/>.
    /// </summary>
    public WikiConflictException() { }

    /// <summary>
    /// Constructs a new instance of <see cref="WikiConflictException"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WikiConflictException(string? message) : base(message) { }

    /// <summary>
    /// Constructs a new instance of <see cref="WikiConflictException"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or a null reference.
    /// </param>
    public WikiConflictException(string? message, Exception? innerException) : base(message, innerException) { }
}
