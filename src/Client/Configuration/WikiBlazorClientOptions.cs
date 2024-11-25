using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client.Configuration;

/// <summary>
/// Options for configuring <c>Tavenem.Wiki.Blazor.Client</c>.
/// </summary>
public class WikiBlazorClientOptions() : WikiBlazorOptions
{
    private IArticleRenderManager? _articleRenderManager;
    private Func<IServiceProvider, IArticleRenderManager>? _articleRenderManagerConfig;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private Type? _articleRenderManagerType;

    private IOfflineManager? _offlineManager;
    private Func<IServiceProvider, IOfflineManager>? _offlineManagerConfig;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private Type? _offlineManagerType;

    /// <summary>
    /// <para>
    /// Supply an instance of <see cref="IArticleRenderManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="ArticleRenderManager"/>.
    /// </para>
    /// </summary>
    public IArticleRenderManager? ArticleRenderManager
    {
        get => _articleRenderManager;
        set
        {
            _articleRenderManager = value;
            _articleRenderManagerConfig = null;
            _articleRenderManagerType = null;
        }
    }

    /// <summary>
    /// <para>
    /// Supply an instance of <see cref="IOfflineManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="OfflineManager"/>.
    /// </para>
    /// </summary>
    public IOfflineManager? OfflineManager
    {
        get => _offlineManager;
        set
        {
            _offlineManager = value;
            _offlineManagerConfig = null;
            _offlineManagerType = null;
        }
    }

    /// <summary>
    /// Constructs a new instance of <see cref="WikiBlazorClientOptions"/>.
    /// </summary>
    /// <param name="other">
    /// An instance of <see cref="WikiBlazorOptions"/> from which to copy settings.
    /// </param>
    public WikiBlazorClientOptions(WikiBlazorOptions other) : this()
    {
        AboutPageTitle = other.AboutPageTitle;
        AppBar = other.AppBar;
        AppBarRenderMode = other.AppBarRenderMode;
        CategoriesTitle = other.CategoriesTitle;
        CategoryNamespace = other.CategoryNamespace;
        CompactLayout = other.CompactLayout;
        CompactRouteHostPart = other.CompactRouteHostPart;
        CompactRouteHostPosition = other.CompactRouteHostPosition;
        CompactRoutePort = other.CompactRoutePort;
        ContactPageTitle = other.ContactPageTitle;
        ContentsPageTitle = other.ContentsPageTitle;
        CopyrightPageTitle = other.CopyrightPageTitle;
        CustomAdminNamespaces = other.CustomAdminNamespaces;
        CustomReservedNamespaces = other.CustomReservedNamespaces;
        DefaultAnonymousPermission = other.DefaultAnonymousPermission;
        DefaultRegisteredPermission = other.DefaultRegisteredPermission;
        DefaultTableOfContentsDepth = other.DefaultTableOfContentsDepth;
        DefaultTableOfContentsTitle = other.DefaultTableOfContentsTitle;
        DomainArchivePermission = other.DomainArchivePermission;
        FileNamespace = other.FileNamespace;
        GroupNamespace = other.GroupNamespace;
        HelpPageTitle = other.HelpPageTitle;
        InteractiveRenderMode = other.InteractiveRenderMode;
        LinkTemplate = other.LinkTemplate;
        LoginPath = other.LoginPath;
        MainLayout = other.MainLayout;
        MainPageTitle = other.MainPageTitle;
        MaxFileSize = other.MaxFileSize;
        MinimumTableOfContentsHeadings = other.MinimumTableOfContentsHeadings;
        PolicyPageTitle = other.PolicyPageTitle;
        Postprocessors = other.Postprocessors;
        ScriptNamespace = other.ScriptNamespace;
        SiteName = other.SiteName;
        SystemNamespace = other.SystemNamespace;
        TenorAPIKey = other.TenorAPIKey;
        TransclusionNamespace = other.TransclusionNamespace;
        UserDomains = other.UserDomains;
        UserNamespace = other.UserNamespace;
        WikiLinkPrefix = other.WikiLinkPrefix;
        WikiServerApiRoute = other.WikiServerApiRoute;
    }

    /// <inheritdoc />
    public override IServiceCollection Add(IServiceCollection services)
    {
        AddArticleRenderManager(services);
        AddOfflineManager(services);

        return base.Add(services);
    }

    /// <summary>
    /// <para>
    /// Supply a type of <see cref="IArticleRenderManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="ArticleRenderManager"/>.
    /// </para>
    /// </summary>
    public void ConfigureArticleRenderManager(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type)
    {
        _articleRenderManagerConfig = null;
        _articleRenderManager = null;
        _articleRenderManagerType = type;
    }

    /// <summary>
    /// <para>
    /// Supply a function which returns an instance of <see cref="IArticleRenderManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="ArticleRenderManager"/>.
    /// </para>
    /// </summary>
    public void ConfigureArticleRenderManager(Func<IServiceProvider, IArticleRenderManager> config)
    {
        _articleRenderManagerConfig = config;
        _articleRenderManager = null;
        _articleRenderManagerType = null;
    }

    /// <summary>
    /// <para>
    /// Supply a type of <see cref="IOfflineManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="OfflineManager"/>.
    /// </para>
    /// </summary>
    public void ConfigureOfflineManager(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type)
    {
        _offlineManagerConfig = null;
        _offlineManager = null;
        _offlineManagerType = type;
    }

    /// <summary>
    /// <para>
    /// Supply a function which returns an instance of <see cref="IOfflineManager"/>.
    /// </para>
    /// <para>
    /// May be omitted to use the default <see cref="OfflineManager"/>.
    /// </para>
    /// </summary>
    public void ConfigureOfflineManager(Func<IServiceProvider, IOfflineManager> config)
    {
        _offlineManagerConfig = config;
        _offlineManager = null;
        _offlineManagerType = null;
    }

    private void AddArticleRenderManager(IServiceCollection services)
    {
        if (ArticleRenderManager is not null)
        {
            services.AddScoped(_ => ArticleRenderManager);
        }
        else if (_articleRenderManagerConfig is not null)
        {
            services.AddScoped(_articleRenderManagerConfig);
        }
        else if (_articleRenderManagerType is not null)
        {
            services.AddScoped(typeof(IArticleRenderManager), _articleRenderManagerType);
        }
        else
        {
            services.AddScoped<IArticleRenderManager, ArticleRenderManager>();
        }
    }

    private void AddOfflineManager(IServiceCollection services)
    {
        if (OfflineManager is not null)
        {
            services.AddScoped(_ => OfflineManager);
        }
        else if (_offlineManagerConfig is not null)
        {
            services.AddScoped(_offlineManagerConfig);
        }
        else if (_offlineManagerType is not null)
        {
            services.AddScoped(typeof(IOfflineManager), _offlineManagerType);
        }
        else
        {
            services.AddScoped<IOfflineManager, OfflineManager>();
        }
    }
}
