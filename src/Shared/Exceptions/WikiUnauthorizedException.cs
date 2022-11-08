namespace Tavenem.Wiki.Blazor.Exceptions;

/// <summary>
/// Indicates that a user attempted an action for which that user is not authorized.
/// </summary>
public class WikiUnauthorizedException : Exception
{
    /// <summary>
    /// Constructs a new instance of <see cref="WikiUnauthorizedException"/>.
    /// </summary>
    public WikiUnauthorizedException() { }

    /// <summary>
    /// Constructs a new instance of <see cref="WikiUnauthorizedException"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WikiUnauthorizedException(string? message) : base(message) { }

    /// <summary>
    /// Constructs a new instance of <see cref="WikiUnauthorizedException"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or a null reference.
    /// </param>
    public WikiUnauthorizedException(string? message, Exception? innerException) : base(message, innerException) { }
}
