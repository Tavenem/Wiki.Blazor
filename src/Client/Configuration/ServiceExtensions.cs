using Tavenem.Wiki.Blazor.Client;
using Tavenem.Wiki.Blazor.Client.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension to <c>Microsoft.Extensions.DependencyInjection</c> for
/// <c>Tavenem.Wiki.Blazor</c>.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Add the required services for <c>Tavenem.Wiki.Blazor</c>.
    /// </summary>
    /// <param name="services">Your <see cref="IServiceCollection"/> instance.</param>
    /// <param name="options">The options used to configure the wiki.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiClient(
        this IServiceCollection services,
        WikiBlazorOptions? options = null)
    {
        var configuredOptions = options is null
            ? new()
            : new WikiBlazorClientOptions(options);
        return configuredOptions.Add(services);
    }

    /// <summary>
    /// Add the required services for <c>Tavenem.Wiki.Blazor</c>.
    /// </summary>
    /// <param name="services">Your <see cref="IServiceCollection"/> instance.</param>
    /// <param name="options">The options used to configure the wiki.</param>
    /// <param name="config">Configures the options used to configure the wiki.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiClient(
        this IServiceCollection services,
        WikiBlazorOptions options,
        Action<WikiBlazorClientOptions> config)
    {
        var configuredOptions = new WikiBlazorClientOptions(options);
        config.Invoke(configuredOptions);
        return services.AddWikiClient(configuredOptions);
    }

    /// <summary>
    /// Add the required services for <c>Tavenem.Wiki.Blazor</c>.
    /// </summary>
    /// <param name="services">Your <see cref="IServiceCollection"/> instance.</param>
    /// <param name="config">Configures the options used to configure the wiki.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiClient(
        this IServiceCollection services,
        Action<WikiBlazorClientOptions> config)
    {
        var options = new WikiBlazorClientOptions();
        config.Invoke(options);
        return options.Add(services);
    }
}
