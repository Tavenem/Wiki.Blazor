using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki;
using Tavenem.Wiki.Blazor;
using Tavenem.Wiki.Blazor.Client;
using Tavenem.Wiki.Blazor.Services.Search;

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
    /// <param name="searchClient">
    /// <para>
    /// An <see cref="ISearchClient"/> instance.
    /// </para>
    /// <para>
    /// If omitted, an instance of <see cref="DefaultSearchClient"/> will be used. Note: the
    /// default client is not recommended for production use.
    /// </para>
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiClient(
        this IServiceCollection services,
        WikiOptions? wikiOptions = null,
        WikiBlazorClientOptions? wikiBlazorOptions = null,
        ISearchClient? searchClient = null)
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
            services.AddScoped(_ => new WikiBlazorClientOptions());
        }

        if (searchClient is null)
        {
            services.AddScoped<ISearchClient, DefaultSearchClient>();
        }
        else
        {
            services.AddScoped(_ => searchClient);
        }

        services.AddScoped<WikiDataManager>();

        return services;
    }

    /// <summary>
    /// Add the required services for <c>Tavenem.Wiki.Blazor</c>.
    /// </summary>
    /// <param name="services">Your <see cref="IServiceCollection"/> instance.</param>
    /// <param name="searchClientType">
    /// <para>
    /// The type of <see cref="ISearchClient"/> to register.
    /// </para>
    /// <para>
    /// If omitted, <see cref="DefaultSearchClient"/> will be used. Note: the default client is
    /// not recommended for production use.
    /// </para>
    /// </param>
    /// <param name="wikiOptions">
    /// The options used to configure the wiki system.
    /// </param>
    /// <param name="wikiBlazorOptions">
    /// The options used to configure the wiki Blazor system.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiClient(
        this IServiceCollection services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type searchClientType,
        WikiOptions? wikiOptions = null,
        WikiBlazorClientOptions? wikiBlazorOptions = null)
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
            services.AddScoped(_ => new WikiBlazorClientOptions());
        }

        if (searchClientType is null)
        {
            services.AddScoped<ISearchClient, DefaultSearchClient>();
        }
        else
        {
            services.AddScoped(typeof(ISearchClient), searchClientType);
        }

        services.AddScoped<WikiDataManager>();

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
    /// <param name="searchClientBuilder">
    /// <para>
    /// A function which provides an <see cref="ISearchClient"/> instance.
    /// </para>
    /// <para>
    /// If omitted, an instance of <see cref="DefaultSearchClient"/> will be used. Note: the
    /// default client is not recommended for production use.
    /// </para>
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiClient(
        this IServiceCollection services,
        Func<IServiceProvider, WikiOptions> wikiOptionsBuilder,
        Func<IServiceProvider, WikiBlazorClientOptions> wikiBlazorOptionsBuilder,
        Func<IServiceProvider, ISearchClient>? searchClientBuilder = null)
    {
        services
            .AddTavenemFramework()
            .AddScoped<WikiState>()
            .AddScoped(wikiOptionsBuilder)
            .AddScoped(wikiBlazorOptionsBuilder);

        if (searchClientBuilder is null)
        {
            services.AddScoped<ISearchClient, DefaultSearchClient>();
        }
        else
        {
            services.AddScoped(searchClientBuilder);
        }

        return services.AddScoped<WikiDataManager>();
    }
}
