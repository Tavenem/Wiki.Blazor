using Tavenem.Wiki;
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
    /// <param name="wikiOptions">
    /// The options used to configure the wiki system.
    /// </param>
    /// <param name="wikiBlazorOptions">
    /// The options used to configure the wiki Blazor system.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddTavenemWikiClient(
        this IServiceCollection services,
        WikiOptions? wikiOptions = null,
        IWikiBlazorClientOptions? wikiBlazorOptions = null)
    {
        services
            .AddTavenemFramework()
            .AddScoped<WikiState>();

        if (wikiOptions is not null)
        {
            services.AddScoped(_ => wikiOptions);
        }
        else
        {
            services.AddScoped(_ => new WikiOptions { LinkTemplate = WikiBlazorClientOptions.DefaultLinkTemplate });
        }
        if (wikiBlazorOptions is not null)
        {
            services.AddScoped(_ => wikiBlazorOptions);
        }
        else
        {
            services.AddScoped<IWikiBlazorClientOptions>(_ => new WikiBlazorClientOptions());
        }
        return services;
    }

    /// <summary>
    /// Add the required services for <c>Tavenem.Wiki.Blazor</c>.
    /// </summary>
    /// <param name="services">Your <see cref="IServiceCollection"/> instance.</param>
    /// <param name="wikiOptionsBuilder">
    /// A function which provides the options used to configure the wiki system.
    /// </param>
    /// <param name="wikiBlazorOptionsBuilder">
    /// A function which provides the options used to configure the wiki Blazor system.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddTavenemWikiClient(
        this IServiceCollection services,
        Func<IServiceProvider, WikiOptions> wikiOptionsBuilder,
        Func<IServiceProvider, IWikiBlazorClientOptions> wikiBlazorOptionsBuilder) => services
        .AddTavenemFramework()
        .AddScoped<WikiState>()
        .AddScoped(wikiOptionsBuilder)
        .AddScoped(wikiBlazorOptionsBuilder);
}
