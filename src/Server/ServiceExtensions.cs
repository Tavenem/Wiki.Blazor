﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Text.Json.Serialization.Metadata;
using Tavenem.Wiki;
using Tavenem.Wiki.Blazor;
using Tavenem.Wiki.Blazor.Server;
using Tavenem.Wiki.Blazor.Services;

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
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiServer(
        this IServiceCollection services,
        IWikiUserManager userManager,
        IWikiGroupManager groupManager,
        WikiOptions? wikiOptions = null,
        WikiBlazorServerOptions? wikiBlazorOptions = null,
        IFileManager? fileManager = null)
    {
        services.AddMemoryCache();

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
            services.AddScoped(_ => new WikiBlazorServerOptions());
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

        return services.AddWikiJsonContext();
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
    /// <param name="fileManagerType">
    /// <para>
    /// The type of <see cref="IFileManager"/> to register.
    /// </para>
    /// <para>
    /// If omitted, <see cref="LocalFileManager"/> will be used.
    /// </para>
    /// </param>
    /// <param name="wikiOptions">
    /// The options used to configure the wiki system.
    /// </param>
    /// <param name="wikiBlazorOptions">
    /// The options used to configure the wiki Blazor system.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiServer(
        this IServiceCollection services,
        Type userManagerType,
        Type groupManagerType,
        Type? fileManagerType,
        WikiOptions? wikiOptions = null,
        WikiBlazorServerOptions? wikiBlazorOptions = null)
    {
        services.AddMemoryCache();

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
            services.AddScoped(_ => new WikiBlazorServerOptions());
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

        return services.AddWikiJsonContext();
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
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiServer(
        this IServiceCollection services,
        Func<IServiceProvider, IWikiUserManager> userManagerBuilder,
        Func<IServiceProvider, IWikiGroupManager> groupManagerBuilder,
        WikiOptions? wikiOptions = null,
        WikiBlazorServerOptions? wikiBlazorOptions = null,
        Func<IServiceProvider, IFileManager>? fileManagerBuilder = null)
    {
        services.AddMemoryCache();

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
            services.AddScoped(_ => new WikiBlazorServerOptions());
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

        return services.AddWikiJsonContext();
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
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiServer(
        this IServiceCollection services,
        IWikiUserManager userManager,
        IWikiGroupManager groupManager,
        Func<IServiceProvider, WikiOptions> wikiOptionsBuilder,
        Func<IServiceProvider, WikiBlazorServerOptions> wikiBlazorOptionsBuilder,
        IFileManager? fileManager = null)
    {
        services
            .AddMemoryCache()
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

        return services.AddWikiJsonContext();
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
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiServer(
        this IServiceCollection services,
        Type userManagerType,
        Type groupManagerType,
        Func<IServiceProvider, WikiOptions> wikiOptionsBuilder,
        Func<IServiceProvider, WikiBlazorServerOptions> wikiBlazorOptionsBuilder,
        Type? fileManagerType)
    {
        services
            .AddMemoryCache()
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

        return services.AddWikiJsonContext();
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
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWikiServer(
        this IServiceCollection services,
        Func<IServiceProvider, IWikiUserManager> userManagerBuilder,
        Func<IServiceProvider, IWikiGroupManager> groupManagerBuilder,
        Func<IServiceProvider, WikiOptions> wikiOptionsBuilder,
        Func<IServiceProvider, WikiBlazorServerOptions> wikiBlazorOptionsBuilder,
        Func<IServiceProvider, IFileManager>? fileManagerBuilder = null)
    {
        services
            .AddMemoryCache()
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

        return services.AddWikiJsonContext();
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
        var options = provider.GetRequiredService<WikiBlazorServerOptions>();

        endpoints.MapControllerRoute(
            "tavenem_wiki_route",
            $$"""{{(options?.WikiServerApiRoute ?? WikiBlazorServerOptions.DefaultWikiServerApiRoute).TrimStart('/')}}/{action}""",
            new { area = "Wiki", controller = "Wiki" },
            new { area = "Wiki", controller = "Wiki" });
    }

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
