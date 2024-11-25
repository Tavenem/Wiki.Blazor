using Tavenem.DataStorage;

namespace Tavenem.Wiki.Blazor.Client.Services;

/// <summary>
/// Determines whether actions can be taken offline.
/// </summary>
public interface IOfflineManager
{
    /// <summary>
    /// Determines whether the given content may be edited locally.
    /// </summary>
    /// <param name="title">The title of the content to be edited.</param>
    /// <param name="wikiNamespace">The namespace of the content to be edited.</param>
    /// <param name="domain">The domain of the content to be edited (if any).</param>
    /// <returns>
    /// <see langword="true"/> if the content can be edited locally; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Locally means in the local <see cref="IDataStore"/> instance, rather than via the <see
    /// cref="WikiBlazorOptions.WikiServerApiRoute"/>.
    /// </remarks>
    ValueTask<bool> CanEditOfflineAsync(string title, string wikiNamespace, string? domain);

    /// <summary>
    /// A function which determines whether the given domain should always be retrieved from the local
    /// <see cref="IDataStore"/>, and never from the server.
    /// </summary>
    /// <param name="domain">A wiki domain name.</param>
    /// <returns>
    /// <see langword="true"/> if the content should always be retrieved from the local <see
    /// cref="IDataStore"/>; <see langword="false"/> if the content should be retrieved from the server
    /// when possible.
    /// </returns>
    ValueTask<bool> IsOfflineDomainAsync(string domain);
}
