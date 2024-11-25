using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Text.Json.Serialization.Metadata;
using Tavenem.Wiki;
using Tavenem.Wiki.Blazor;
using Tavenem.Wiki.Blazor.Client;
using Tavenem.Wiki.Blazor.Server.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension to <c>Microsoft.Extensions.DependencyInjection</c> for
/// <c>Tavenem.Wiki.Blazor</c>.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// The relative URL of the wiki's server API used if one is not provided.
    /// </summary>
    private const string DefaultWikiServerApiRoute = "/wikiapi";

    /// <summary>
    /// Adds support for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor client.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="options">The options used to configure the wiki.</param>
    public static IServiceCollection AddWikiServer(
        this IServiceCollection services,
        WikiBlazorOptions? options = null)
    {
        var configuredOptions = options is null
            ? new()
            : new WikiBlazorServerOptions(options);
        return configuredOptions.Add(services);
    }

    /// <summary>
    /// Adds support for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor client.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="config">Configures the options used to configure the wiki.</param>
    public static IServiceCollection AddWikiServer(
        this IServiceCollection services,
        Action<WikiBlazorServerOptions> config)
    {
        var options = new WikiBlazorServerOptions();
        config.Invoke(options);
        return options.Add(services);
    }

    /// <summary>
    /// Adds support for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor client.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="options">The options used to configure the wiki.</param>
    /// <param name="config">Configures the options used to configure the wiki.</param>
    public static IServiceCollection AddWikiServer(
        this IServiceCollection services,
        WikiBlazorOptions options,
        Action<WikiBlazorServerOptions> config)
    {
        var configuredOptions = new WikiBlazorServerOptions(options);
        config?.Invoke(configuredOptions);
        return configuredOptions.Add(services);
    }

    /// <summary>
    /// <para>
    /// Adds endpoints for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor WebAssembly client.
    /// </para>
    /// <para>
    /// Should be added before all other endpoint mapping to ensure that wiki patterns are
    /// matched before falling back to default routing logic.
    /// </para>
    /// </summary>
    /// <param name="endpoints">An <see cref="IEndpointRouteBuilder"/> instance.</param>
    /// <param name="wikiServerApiRoute">
    /// <para>
    /// The relative URL of the wiki's server API.
    /// </para>
    /// <para>
    /// If omitted, the path "/wikiapi" will be used.
    /// </para>
    /// </param>
    public static void MapWiki(this IEndpointRouteBuilder endpoints, string? wikiServerApiRoute = null)
        => endpoints.MapControllerRoute(
            "tavenem_wiki_route",
            $$"""{{(wikiServerApiRoute ?? DefaultWikiServerApiRoute).TrimStart('/')}}/{action}""",
            new { area = "Wiki", controller = "Wiki" },
            new { area = "Wiki", controller = "Wiki" });

    /// <summary>
    /// Adds the <see cref="WikiBlazorJsonSerializerContext"/> and <see
    /// cref="WikiJsonSerializerContext"/> to the <see cref="IJsonTypeInfoResolver"/> chain resolver used
    /// by Mvc.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    /// <remarks>
    /// This is called by all overloads of <c>AddWikiServer</c> and should not be called separately.
    /// </remarks>
    public static IServiceCollection AddWikiJsonContext(this IServiceCollection services)
    {
        return services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, WikiJsonSerializerContext.Default);
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, WikiBlazorJsonSerializerContext.Default);
        });
    }
}
