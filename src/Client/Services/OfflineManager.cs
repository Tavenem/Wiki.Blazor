using Tavenem.DataStorage;

namespace Tavenem.Wiki.Blazor.Client.Services;

/// <summary>
/// A default implementation of <see cref="IOfflineManager"/> which always returns <see
/// langword="false"/>.
/// </summary>
public class OfflineManager : IOfflineManager
{
    /// <summary>
    /// Determines whether the given content may be edited locally. Always returns <see
    /// langword="false"/> in the default implementation.
    /// </summary>
    /// <param name="title">The title of the content to be edited.</param>
    /// <param name="wikiNamespace">The namespace of the content to be edited.</param>
    /// <param name="domain">The domain of the content to be edited (if any).</param>
    /// <returns>
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Locally means in the local <see cref="IDataStore"/> instance, rather than via the <see
    /// cref="WikiBlazorOptions.WikiServerApiRoute"/>.
    /// </remarks>
    public ValueTask<bool> CanEditOfflineAsync(string title, string wikiNamespace, string? domain)
        => ValueTask.FromResult(false);

    /// <summary>
    /// A function which determines whether the given domain should always be retrieved from the
    /// local <see cref="IDataStore"/>, and never from the server. Always returns <see
    /// langword="false"/> in the default implementation.
    /// </summary>
    /// <param name="domain">A wiki domain name.</param>
    /// <returns>
    /// <see langword="false"/>.
    /// </returns>
    public ValueTask<bool> IsOfflineDomainAsync(string domain) => ValueTask.FromResult(false);
}
