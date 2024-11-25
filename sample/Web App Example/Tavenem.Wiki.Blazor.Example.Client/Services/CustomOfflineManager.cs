using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Example.Client.Services;

public class CustomOfflineManager : IOfflineManager
{
    /// <inheritdoc />
    public ValueTask<bool> CanEditOfflineAsync(string title, string wikiNamespace, string? domain)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask<bool> IsOfflineDomainAsync(string domain)
        => ValueTask.FromResult(false);
}
