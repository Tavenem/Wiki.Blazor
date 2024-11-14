using Microsoft.Extensions.DependencyInjection.Extensions;
using Tavenem.Wiki;
using Tavenem.Wiki.Blazor.Client;
using Tavenem.Wiki.Blazor.Client.Configuration;
using Tavenem.Wiki.Blazor.Client.Services;

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
        options ??= new();

        InteractiveRenderSettings.InteractiveRenderMode = options.InteractiveRenderMode;

        if (options.DataStore is not null)
        {
            services.TryAddScoped(_ => options.DataStore);
        }
        services.TryAddScoped<WikiOptions>(_ => options);
        services.AddScoped(_ => options);

        return services
            .AddTavenemFramework()
            .AddScoped<WikiState>()
            .AddScoped<ClientWikiDataService>()
            .AddScoped<WikiDataService>();
    }

    /// <summary>
    /// Add the required services for <c>Tavenem.Wiki.Blazor</c>.
    /// </summary>
    /// <param name="services">Your <see cref="IServiceCollection"/> instance.</param>
    /// <param name="config">Configures the options used to configure the wiki.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiClient(
        this IServiceCollection services,
        Action<WikiBlazorClientServiceOptions> config)
    {
        var options = new WikiBlazorClientServiceOptions();
        config.Invoke(options);
        return options.Add(services);
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
        Action<WikiBlazorClientServiceOptions> config)
    {
        var configuredOptions = new WikiBlazorClientServiceOptions(options);
        config.Invoke(configuredOptions);
        return configuredOptions.Add(services);
    }
}
