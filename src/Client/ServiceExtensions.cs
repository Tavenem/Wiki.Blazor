using Tavenem.Wiki.Blazor.Client;

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
        WikiBlazorOptions? options = null) => (options ?? new()).Add(services);

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
        Action<WikiBlazorOptions> config)
    {
        config.Invoke(options);
        return services.AddWikiClient(options);
    }

    /// <summary>
    /// Add the required services for <c>Tavenem.Wiki.Blazor</c>.
    /// </summary>
    /// <param name="services">Your <see cref="IServiceCollection"/> instance.</param>
    /// <param name="config">Configures the options used to configure the wiki.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiClient(
        this IServiceCollection services,
        Action<WikiBlazorOptions> config) => services.AddWikiClient(new(), config);
}
