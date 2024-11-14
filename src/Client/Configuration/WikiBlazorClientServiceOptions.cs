using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Client.Configuration;

/// <summary>
/// Options for configuring <c>Tavenem.Wiki.Blazor.Client</c>.
/// </summary>
public class WikiBlazorClientServiceOptions() : WikiBlazorOptions
{
    private IDataStore? _dataStore;
    private Func<IServiceProvider, IDataStore>? _dataStoreConfig;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private Type? _dataStoreType;

    /// <summary>
    /// An optional data store which the client can access directly (i.e. without reaching the
    /// server).
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the <see cref="WikiBlazorOptions.WikiServerApiRoute"/> has also been defined, the client
    /// will try to reach the server first for all wiki operations. If the server cannot be reached
    /// or the requested content is unavailable at the server, the client will fall back to the
    /// local data store.
    /// </para>
    /// <para>
    /// If both the server and the local data store are unavailable, the wiki will remain
    /// operational, but will show no content and will not allow any content to be added.
    /// </para>
    /// <para>
    /// No automatic synchronization occurs from the local data store to the server (for instance
    /// when an offline client reestablishes network connectivity). If your app model requires
    /// synchronization of offline content to a server, that logic must be implemented separately.
    /// </para>
    /// </remarks>
    public override IDataStore? DataStore
    {
        get => _dataStore;
        set
        {
            _dataStore = value;
            _dataStoreConfig = null;
            _dataStoreType = null;
        }
    }

    /// <summary>
    /// Constructs a new instance of <see cref="WikiBlazorClientServiceOptions"/>.
    /// </summary>
    /// <param name="other">
    /// An instance of <see cref="WikiBlazorOptions"/> from which to copy settings.
    /// </param>
    public WikiBlazorClientServiceOptions(WikiBlazorOptions other) : this()
    {
        AboutPageTitle = other.AboutPageTitle;
        AppBar = other.AppBar;
        AppBarRenderMode = other.AppBarRenderMode;
        ArticleEndMatter = other.ArticleEndMatter;
        ArticleEndMatterRenderMode = other.ArticleEndMatterRenderMode;
        ArticleFrontMatter = other.ArticleFrontMatter;
        ArticleFrontMatterRenderMode = other.ArticleFrontMatterRenderMode;
        CanEditOffline = other.CanEditOffline;
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
        DataStore = other.DataStore;
        DefaultAnonymousPermission = other.DefaultAnonymousPermission;
        DefaultRegisteredPermission = other.DefaultRegisteredPermission;
        DefaultTableOfContentsDepth = other.DefaultTableOfContentsDepth;
        DefaultTableOfContentsTitle = other.DefaultTableOfContentsTitle;
        DomainArchivePermission = other.DomainArchivePermission;
        FileNamespace = other.FileNamespace;
        GetDomainPermission = other.GetDomainPermission;
        GroupNamespace = other.GroupNamespace;
        HelpPageTitle = other.HelpPageTitle;
        InteractiveRenderMode = other.InteractiveRenderMode;
        IsOfflineDomain = other.IsOfflineDomain;
        LinkTemplate = other.LinkTemplate;
        LoginPath = other.LoginPath;
        MainLayout = other.MainLayout;
        MainPageTitle = other.MainPageTitle;
        MaxFileSize = other.MaxFileSize;
        MinimumTableOfContentsHeadings = other.MinimumTableOfContentsHeadings;
        OnCreated = other.OnCreated;
        OnDeleted = other.OnDeleted;
        OnEdited = other.OnEdited;
        OnRenamed = other.OnRenamed;
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

    /// <summary>
    /// <para>
    /// Supply a type of <see cref="IDataStore"/>.
    /// </para>
    /// <para>
    /// Optional. See <see cref="DataStore"/>.
    /// </para>
    /// </summary>
    public void ConfigureDataStore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type)
    {
        _dataStoreConfig = null;
        _dataStore = null;
        _dataStoreType = type;
    }

    /// <summary>
    /// <para>
    /// Supply a function which returns an instance of <see cref="IDataStore"/>.
    /// </para>
    /// <para>
    /// Optional. See <see cref="DataStore"/>.
    /// </para>
    /// </summary>
    public void ConfigureDataStore(Func<IServiceProvider, IDataStore> config)
    {
        _dataStoreConfig = config;
        _dataStore = null;
        _dataStoreType = null;
    }

    /// <summary>
    /// Add these configured options to the service collection.
    /// </summary>
    public virtual IServiceCollection Add(IServiceCollection services)
    {
        InteractiveRenderSettings.InteractiveRenderMode = InteractiveRenderMode;

        AddDataStore(services);
        services.TryAddScoped<WikiOptions>(_ => this);
        services.AddScoped<WikiBlazorOptions>(_ => this);

        return services
            .AddTavenemFramework()
            .AddScoped<WikiState>()
            .AddScoped<ClientWikiDataService>()
            .AddScoped<WikiDataService>();
    }

    private void AddDataStore(IServiceCollection services)
    {
        if (DataStore is not null)
        {
            services.TryAddScoped(_ => DataStore);
        }
        else if (_dataStoreConfig is not null)
        {
            services.TryAddScoped(_dataStoreConfig);
        }
        else if (_dataStoreType is not null)
        {
            services.TryAddScoped(typeof(IDataStore), _dataStoreType);
        }
    }
}
