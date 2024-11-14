﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Tavenem.Wiki.Blazor.Client;
using Tavenem.Wiki.Blazor.Client.Configuration;
using Tavenem.Wiki.Blazor.Server.Authorization;
using Tavenem.Wiki.Blazor.Services;

namespace Tavenem.Wiki.Blazor.Server.Configuration;

/// <summary>
/// Options for configuring <c>Tavenem.Wiki.Blazor.Server</c>.
/// </summary>
public class WikiBlazorServerServiceOptions : WikiBlazorClientServiceOptions
{
    private IFileManager? _fileManager;
    private Func<IServiceProvider, IFileManager>? _fileManagerConfig;
    private Type? _fileManagerType;
    private IWikiGroupManager? _groupManager;
    private Func<IServiceProvider, IWikiGroupManager>? _groupManagerConfig;
    private Type? _groupManagerType;
    private IWikiUserManager? _userManager;
    private Func<IServiceProvider, IWikiUserManager>? _userManagerConfig;
    private Type? _userManagerType;

    /// <summary>
    /// <para>
    /// Supply an instance of <see cref="IFileManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="LocalFileManager"/>.
    /// </para>
    /// </summary>
    public IFileManager? FileManager
    {
        get => _fileManager;
        set
        {
            _fileManager = value;
            _fileManagerConfig = null;
            _fileManagerType = null;
        }
    }

    /// <summary>
    /// <para>
    /// Supply an instance of <see cref="IWikiGroupManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="WikiGroupManager"/>.
    /// </para>
    /// </summary>
    public IWikiGroupManager? GroupManager
    {
        get => _groupManager;
        set
        {
            _groupManager = value;
            _groupManagerConfig = null;
            _groupManagerType = null;
        }
    }

    /// <summary>
    /// <para>
    /// Supply an instance of <see cref="IWikiUserManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="WikiUserManager"/>.
    /// </para>
    /// </summary>
    public IWikiUserManager? UserManager
    {
        get => _userManager;
        set
        {
            _userManager = value;
            _userManagerConfig = null;
            _userManagerType = null;
        }
    }

    /// <summary>
    /// <para>
    /// If true (the default), default authorization handlers will be used to determine permission
    /// for wiki operations at the server controller. The default read handler imposes no
    /// requirements (grants anonymous access). The default edit handler requires an authenticated
    /// user.
    /// </para>
    /// <para>
    /// If set to false, custom <see cref="AuthorizationHandler{TRequirement,TResource}"/>
    /// implementations should be registered for <see cref="WikiDefaultRequirement"/> (for read
    /// operations) and <see cref="WikiEditRequirement"/> (for edit operations). Both receive a <see
    /// cref="PageTitle"/> for the resource parameter, although it may be set to a default value
    /// (i.e. the main wiki page) for operations which do not reference a specific wiki page, such
    /// as search.
    /// </para>
    /// <para>
    /// Note that permissions for individual wiki operations are also determined by internal wiki
    /// access controls. This parameter and the <see cref="IAuthorizationHandler"/> implementations
    /// are designed to help prevent unauthorized users from consuming server resources with
    /// potentially expensive calls to determine granular user permissions.
    /// </para>
    /// </summary>
    public bool UseDefaultAuthorization { get; set; } = true;

    /// <summary>
    /// Constructs a new instance of <see cref="WikiBlazorClientServiceOptions"/>.
    /// </summary>
    public WikiBlazorServerServiceOptions() { }

    /// <summary>
    /// Constructs a new instance of <see cref="WikiBlazorClientServiceOptions"/>.
    /// </summary>
    /// <param name="other">
    /// An instance of <see cref="WikiBlazorOptions"/> from which to copy settings.
    /// </param>
    public WikiBlazorServerServiceOptions(WikiBlazorOptions other) : base(other) { }

    /// <summary>
    /// <para>
    /// Supply a type of <see cref="IFileManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="LocalFileManager"/>.
    /// </para>
    /// </summary>
    public void ConfigureFileManager(Type? type)
    {
        _fileManagerConfig = null;
        _fileManager = null;
        _fileManagerType = type;
    }

    /// <summary>
    /// <para>
    /// Supply a function which returns an instance of <see cref="IFileManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="LocalFileManager"/>.
    /// </para>
    /// </summary>
    public void ConfigureFileManager(Func<IServiceProvider, IFileManager> config)
    {
        _fileManagerConfig = config;
        _fileManager = null;
        _fileManagerType = null;
    }

    /// <summary>
    /// <para>
    /// Supply a type of <see cref="IWikiGroupManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="WikiGroupManager"/>.
    /// </para>
    /// </summary>
    public void ConfigureGroupManager(Type? type)
    {
        _groupManagerConfig = null;
        _groupManager = null;
        _groupManagerType = type;
    }

    /// <summary>
    /// <para>
    /// Supply a function which returns an instance of <see cref="IWikiGroupManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="WikiGroupManager"/>.
    /// </para>
    /// </summary>
    public void ConfigureGroupManager(Func<IServiceProvider, IWikiGroupManager> config)
    {
        _groupManagerConfig = config;
        _groupManager = null;
        _groupManagerType = null;
    }

    /// <summary>
    /// <para>
    /// Supply a type of <see cref="IWikiUserManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="WikiUserManager"/>.
    /// </para>
    /// </summary>
    public void ConfigureUserManager(Type? type)
    {
        _userManagerConfig = null;
        _userManager = null;
        _userManagerType = type;
    }

    /// <summary>
    /// <para>
    /// Supply a function which returns an instance of <see cref="IWikiUserManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="WikiUserManager"/>.
    /// </para>
    /// </summary>
    public void ConfigureUserManager(Func<IServiceProvider, IWikiUserManager> config)
    {
        _userManagerConfig = config;
        _userManager = null;
        _userManagerType = null;
    }

    /// <inheritdoc />
    public override IServiceCollection Add(IServiceCollection services)
    {
        AddFileManager(services);
        AddGroupManager(services);
        AddUserManager(services);

        if (UseDefaultAuthorization)
        {
            services.AddSingleton<IAuthorizationHandler, WikiDefaultAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, WikiEditAuthorizationHandler>();
        }

        return base
            .Add(services)
            .AddMemoryCache()
            .AddWikiJsonContext();
    }

    private void AddFileManager(IServiceCollection services)
    {
        if (FileManager is not null)
        {
            if (FileManager is LocalFileManager)
            {
                services.AddHttpContextAccessor();
            }
            services.AddScoped(_ => FileManager);
        }
        else if (_fileManagerConfig is not null)
        {
            services.AddScoped(_fileManagerConfig);
        }
        else if (_fileManagerType is not null)
        {
            services.AddScoped(typeof(IFileManager), _fileManagerType);
        }
        else
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IFileManager, LocalFileManager>();
        }
    }

    private void AddGroupManager(IServiceCollection services)
    {
        if (GroupManager is not null)
        {
            services.AddScoped(_ => GroupManager);
        }
        else if (_groupManagerConfig is not null)
        {
            services.AddScoped(_groupManagerConfig);
        }
        else if (_groupManagerType is not null)
        {
            services.AddScoped(typeof(IWikiGroupManager), _groupManagerType);
        }
        else
        {
            services.AddScoped<IWikiGroupManager, WikiGroupManager>();
        }
    }

    private void AddUserManager(IServiceCollection services)
    {
        if (UserManager is not null)
        {
            services.AddScoped(_ => UserManager);
        }
        else if (_userManagerConfig is not null)
        {
            services.AddScoped(_userManagerConfig);
        }
        else if (_userManagerType is not null)
        {
            services.AddScoped(typeof(IWikiUserManager), _userManagerType);
        }
        else
        {
            services.AddScoped<IWikiUserManager, WikiUserManager>();
        }
    }
}