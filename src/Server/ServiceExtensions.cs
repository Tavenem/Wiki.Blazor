using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Tavenem.Wiki;
using Tavenem.Wiki.Blazor;
using Tavenem.Wiki.Blazor.Server;
using Tavenem.Wiki.Blazor.Server.Hubs;
using Tavenem.Wiki.Blazor.Services.FileManager;
using Tavenem.Wiki.Blazor.Services.Search;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension to <c>Microsoft.Extensions.DependencyInjection</c> for
/// <c>Tavenem.Wiki.Blazor</c>.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds support for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor client.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="userManager">An <see cref="IWikiUserManager"/> instance.</param>
    /// <param name="groupManager">An <see cref="IWikiGroupManager"/> instance.</param>
    /// <param name="wikiOptions">
    /// The options used to configure the wiki system.
    /// </param>
    /// <param name="wikiBlazorOptions">
    /// The options used to configure the wiki Blazor system.
    /// </param>
    /// <param name="fileManager">
    /// <para>
    /// An <see cref="IFileManager"/> instance.
    /// </para>
    /// <para>
    /// If omitted, an instance of <see cref="LocalFileManager"/> will be used.
    /// </para>
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
    public static void AddWiki(
        this IServiceCollection services,
        IWikiUserManager userManager,
        IWikiGroupManager groupManager,
        WikiOptions? wikiOptions = null,
        IWikiBlazorServerOptions? wikiBlazorOptions = null,
        IFileManager? fileManager = null,
        ISearchClient? searchClient = null)
    {
        if (wikiOptions is not null)
        {
            services.AddScoped(_ => wikiOptions);
        }
        else
        {
            services.AddScoped(_ => new WikiOptions
            {
                LinkTemplate = WikiBlazorServerOptions.DefaultLinkTemplate,
            });
        }
        if (wikiBlazorOptions is not null)
        {
            services.AddScoped(_ => wikiBlazorOptions);
        }
        else
        {
            services.AddScoped<IWikiBlazorServerOptions>(_ => new WikiBlazorServerOptions());
        }

        services.AddScoped(_ => userManager);
        services.AddScoped(_ => groupManager);

        if (fileManager is null or LocalFileManager)
        {
            services.AddHttpContextAccessor();
        }
        if (fileManager is null)
        {
            services.AddScoped<IFileManager, LocalFileManager>();
        }
        else
        {
            services.AddScoped(_ => fileManager);
        }

        if (searchClient is null)
        {
            services.AddScoped<ISearchClient, DefaultSearchClient>();
        }
        else
        {
            services.AddScoped(_ => searchClient);
        }
    }

    /// <summary>
    /// Adds support for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor client.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="userManagerType">
    /// The type of <see cref="IWikiUserManager"/> to register.
    /// </param>
    /// <param name="groupManagerType">
    /// The type of <see cref="IWikiGroupManager"/> to register.
    /// </param>
    /// <param name="wikiOptions">
    /// The options used to configure the wiki system.
    /// </param>
    /// <param name="wikiBlazorOptions">
    /// The options used to configure the wiki Blazor system.
    /// </param>
    /// <param name="fileManagerType">
    /// <para>
    /// The type of <see cref="IFileManager"/> to register.
    /// </para>
    /// <para>
    /// If omitted, <see cref="LocalFileManager"/> will be used.
    /// </para>
    /// </param>
    /// <param name="searchClientType">
    /// <para>
    /// The type of <see cref="ISearchClient"/> to register.
    /// </para>
    /// <para>
    /// If omitted, <see cref="DefaultSearchClient"/> will be used. Note: the default client is
    /// not recommended for production use.
    /// </para>
    /// </param>
    public static void AddWiki(
        this IServiceCollection services,
        Type userManagerType,
        Type groupManagerType,
        WikiOptions? wikiOptions = null,
        IWikiBlazorServerOptions? wikiBlazorOptions = null,
        Type? fileManagerType = null,
        Type? searchClientType = null)
    {
        if (wikiOptions is not null)
        {
            services.AddScoped(_ => wikiOptions);
        }
        else
        {
            services.AddScoped(_ => new WikiOptions
            {
                LinkTemplate = WikiBlazorServerOptions.DefaultLinkTemplate,
            });
        }
        if (wikiBlazorOptions is not null)
        {
            services.AddScoped(_ => wikiBlazorOptions);
        }
        else
        {
            services.AddScoped<IWikiBlazorServerOptions>(_ => new WikiBlazorServerOptions());
        }

        services.AddScoped(typeof(IWikiUserManager), userManagerType);
        services.AddScoped(typeof(IWikiGroupManager), groupManagerType);

        if (fileManagerType is null)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IFileManager, LocalFileManager>();
        }
        else
        {
            services.AddScoped(typeof(IFileManager), fileManagerType);
        }

        if (searchClientType is null)
        {
            services.AddScoped<ISearchClient, DefaultSearchClient>();
        }
        else
        {
            services.AddScoped(typeof(ISearchClient), searchClientType);
        }
    }

    /// <summary>
    /// Adds support for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor client.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="userManagerBuilder">
    /// A function which provides an <see cref="IWikiUserManager"/> instance.
    /// </param>
    /// <param name="groupManagerBuilder">
    /// A function which provides an <see cref="IWikiGroupManager"/> instance.
    /// </param>
    /// <param name="wikiOptions">
    /// The options used to configure the wiki system.
    /// </param>
    /// <param name="wikiBlazorOptions">
    /// The options used to configure the wiki Blazor system.
    /// </param>
    /// <param name="fileManagerBuilder">
    /// <para>
    /// A function which provides an <see cref="IFileManager"/> instance.
    /// </para>
    /// <para>
    /// If omitted, an instance of <see cref="LocalFileManager"/> will be used.
    /// </para>
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
    public static void AddWiki(
        this IServiceCollection services,
        Func<IServiceProvider, IWikiUserManager> userManagerBuilder,
        Func<IServiceProvider, IWikiGroupManager> groupManagerBuilder,
        WikiOptions? wikiOptions = null,
        IWikiBlazorServerOptions? wikiBlazorOptions = null,
        Func<IServiceProvider, IFileManager>? fileManagerBuilder = null,
        Func<IServiceProvider, ISearchClient>? searchClientBuilder = null)
    {
        if (wikiOptions is not null)
        {
            services.AddScoped(_ => wikiOptions);
        }
        else
        {
            services.AddScoped(_ => new WikiOptions
            {
                LinkTemplate = WikiBlazorServerOptions.DefaultLinkTemplate,
            });
        }
        if (wikiBlazorOptions is not null)
        {
            services.AddScoped(_ => wikiBlazorOptions);
        }
        else
        {
            services.AddScoped<IWikiBlazorServerOptions>(_ => new WikiBlazorServerOptions());
        }

        services.AddScoped(userManagerBuilder);
        services.AddScoped(groupManagerBuilder);

        if (fileManagerBuilder is null)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IFileManager, LocalFileManager>();
        }
        else
        {
            services.AddScoped(fileManagerBuilder);
        }

        if (searchClientBuilder is null)
        {
            services.AddScoped<ISearchClient, DefaultSearchClient>();
        }
        else
        {
            services.AddScoped(searchClientBuilder);
        }
    }

    /// <summary>
    /// Adds support for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor client.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="userManager">An <see cref="IWikiUserManager"/> instance.</param>
    /// <param name="groupManager">An <see cref="IWikiGroupManager"/> instance.</param>
    /// <param name="wikiOptionsBuilder">
    /// A function which provides the options used to configure the wiki system.
    /// </param>
    /// <param name="wikiBlazorOptionsBuilder">
    /// A function which provides the options used to configure the wiki Blazor system.
    /// </param>
    /// <param name="fileManager">
    /// <para>
    /// An <see cref="IFileManager"/> instance.
    /// </para>
    /// <para>
    /// If omitted, an instance of <see cref="LocalFileManager"/> will be used.
    /// </para>
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
    public static void AddWiki(
        this IServiceCollection services,
        IWikiUserManager userManager,
        IWikiGroupManager groupManager,
        Func<IServiceProvider, WikiOptions> wikiOptionsBuilder,
        Func<IServiceProvider, IWikiBlazorServerOptions> wikiBlazorOptionsBuilder,
        IFileManager? fileManager = null,
        ISearchClient? searchClient = null)
    {
        services
            .AddScoped(wikiOptionsBuilder)
            .AddScoped(wikiBlazorOptionsBuilder)
            .AddScoped(_ => userManager)
            .AddScoped(_ => groupManager);

        if (fileManager is null or LocalFileManager)
        {
            services.AddHttpContextAccessor();
        }
        if (fileManager is null)
        {
            services.AddScoped<IFileManager, LocalFileManager>();
        }
        else
        {
            services.AddScoped(_ => fileManager);
        }

        if (searchClient is null)
        {
            services.AddScoped<ISearchClient, DefaultSearchClient>();
        }
        else
        {
            services.AddScoped(_ => searchClient);
        }
    }

    /// <summary>
    /// Adds support for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor client.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="userManagerType">
    /// The type of <see cref="IWikiUserManager"/> to register.
    /// </param>
    /// <param name="groupManagerType">
    /// The type of <see cref="IWikiGroupManager"/> to register.
    /// </param>
    /// <param name="wikiOptionsBuilder">
    /// A function which provides the options used to configure the wiki system.
    /// </param>
    /// <param name="wikiBlazorOptionsBuilder">
    /// A function which provides the options used to configure the wiki Blazor system.
    /// </param>
    /// <param name="fileManagerType">
    /// <para>
    /// The type of <see cref="IFileManager"/> to register.
    /// </para>
    /// <para>
    /// If omitted, <see cref="LocalFileManager"/> will be used.
    /// </para>
    /// </param>
    /// <param name="searchClientType">
    /// <para>
    /// The type of <see cref="ISearchClient"/> to register.
    /// </para>
    /// <para>
    /// If omitted, <see cref="DefaultSearchClient"/> will be used. Note: the default client is
    /// not recommended for production use.
    /// </para>
    /// </param>
    public static void AddWiki(
        this IServiceCollection services,
        Type userManagerType,
        Type groupManagerType,
        Func<IServiceProvider, WikiOptions> wikiOptionsBuilder,
        Func<IServiceProvider, IWikiBlazorServerOptions> wikiBlazorOptionsBuilder,
        Type? fileManagerType = null,
        Type? searchClientType = null)
    {
        services
            .AddScoped(wikiOptionsBuilder)
            .AddScoped(wikiBlazorOptionsBuilder)
            .AddScoped(typeof(IWikiUserManager), userManagerType)
            .AddScoped(typeof(IWikiGroupManager), groupManagerType);

        if (fileManagerType is null)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IFileManager, LocalFileManager>();
        }
        else
        {
            services.AddScoped(typeof(IFileManager), fileManagerType);
        }

        if (searchClientType is null)
        {
            services.AddScoped<ISearchClient, DefaultSearchClient>();
        }
        else
        {
            services.AddScoped(typeof(ISearchClient), searchClientType);
        }
    }

    /// <summary>
    /// Adds support for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor client.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="userManagerBuilder">
    /// A function which provides an <see cref="IWikiUserManager"/> instance.
    /// </param>
    /// <param name="groupManagerBuilder">
    /// A function which provides an <see cref="IWikiGroupManager"/> instance.
    /// </param>
    /// <param name="wikiOptionsBuilder">
    /// A function which provides the options used to configure the wiki system.
    /// </param>
    /// <param name="wikiBlazorOptionsBuilder">
    /// A function which provides the options used to configure the wiki Blazor system.
    /// </param>
    /// <param name="fileManagerBuilder">
    /// <para>
    /// A function which provides an <see cref="IFileManager"/> instance.
    /// </para>
    /// <para>
    /// If omitted, an instance of <see cref="LocalFileManager"/> will be used.
    /// </para>
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
    public static void AddWiki(
        this IServiceCollection services,
        Func<IServiceProvider, IWikiUserManager> userManagerBuilder,
        Func<IServiceProvider, IWikiGroupManager> groupManagerBuilder,
        Func<IServiceProvider, WikiOptions> wikiOptionsBuilder,
        Func<IServiceProvider, IWikiBlazorServerOptions> wikiBlazorOptionsBuilder,
        Func<IServiceProvider, IFileManager>? fileManagerBuilder = null,
        Func<IServiceProvider, ISearchClient>? searchClientBuilder = null)
    {
        services
            .AddScoped(wikiOptionsBuilder)
            .AddScoped(wikiBlazorOptionsBuilder)
            .AddScoped(userManagerBuilder)
            .AddScoped(groupManagerBuilder);

        if (fileManagerBuilder is null)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IFileManager, LocalFileManager>();
        }
        else
        {
            services.AddScoped(fileManagerBuilder);
        }

        if (searchClientBuilder is null)
        {
            services.AddScoped<ISearchClient, DefaultSearchClient>();
        }
        else
        {
            services.AddScoped(searchClientBuilder);
        }
    }

    /// <summary>
    /// <para>
    /// Adds endpoints for the Tavenem.Wiki library to the ASP.NET Host server app for a Blazor WebAssembly client.
    /// </para>
    /// <para>
    /// Should be added after setting <see cref="WikiOptions.MainPageTitle"/>, if a custom
    /// value is to be set.
    /// </para>
    /// <para>
    /// Should be added before all other endpoint mapping to ensure that wiki patterns are
    /// matched before falling back to default routing logic.
    /// </para>
    /// </summary>
    /// <param name="endpoints">An <see cref="IEndpointRouteBuilder"/> instance.</param>
    public static void MapWiki(this IEndpointRouteBuilder endpoints)
    {
        var provider = endpoints.ServiceProvider.CreateScope().ServiceProvider;
        var options = provider.GetRequiredService<IWikiBlazorServerOptions>();

        endpoints.MapHub<WikiTalkHub>(options?.TalkHubRoute ?? WikiBlazorServerOptions.DefaultTalkHubRoute);

        endpoints.MapControllerRoute(
            "tavenem_wiki_route",
            $$"""{{(options?.WikiServerApiRoute ?? WikiBlazorServerOptions.DefaultWikiServerApiRoute).TrimStart('/')}}/{action}""",
            new { area = "Wiki", controller = "Wiki" },
            new { area = "Wiki", controller = "Wiki" });
    }

    public static IServiceCollection AddWikiJsonContext(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
        {
            options
                .JsonSerializerOptions
                .AddContext<WikiBlazorJsonSerializerContext>();
        });
        services
            .AddSignalR()
            .AddJsonProtocol(options =>
             {
                 options
                    .PayloadSerializerOptions
                    .AddContext<WikiBlazorJsonSerializerContext>();
             });
        services.AddResponseCompression(options =>
        {
            options.MimeTypes = ResponseCompressionDefaults
                .MimeTypes
                .Concat(new[] { "application/octet-stream" });
        });
        return services;
    }
}
