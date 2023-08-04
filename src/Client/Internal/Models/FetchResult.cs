namespace Tavenem.Wiki.Blazor.Client.Internal.Models;

/// <summary>
/// The result of performing an internal wiki API call.
/// </summary>
/// <param name="Success">Whether the call was successful.</param>
/// <param name="Message">Any message returned by the call.</param>
public record FetchResult(bool Success, string? Message = null);
