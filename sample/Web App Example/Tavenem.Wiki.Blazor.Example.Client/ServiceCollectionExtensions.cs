using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Tavenem.DataStorage;
using Tavenem.Wiki.Blazor.Client;
using Tavenem.Wiki.Blazor.Example.Client;

namespace Tavenem.Wiki.Blazor.Example.Services;

public static class ServiceCollectionExtensions
{
    public static void AddWikiClient(
        this IServiceCollection services,
        bool server)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<CustomAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(provider =>
            provider.GetRequiredService<CustomAuthenticationStateProvider>());

        var userManager = new DefaultUserManager();
        services.AddScoped<IWikiUserManager>(_ => userManager);

        services.AddScoped<WikiGroupManager>();
        services.AddScoped<IWikiGroupManager>(services =>
            services.GetRequiredService<WikiGroupManager>());

        services.AddWikiClient(
            _ => ExampleWikiOptions.Instance,
            provider => new WikiBlazorClientOptions()
            {
                AppBar = typeof(TopAppBar),
                AppBarRenderMode = RenderMode.InteractiveWebAssembly,
                ArticleFrontMatter = page => string.IsNullOrEmpty(page.Title.Namespace)
                    && string.IsNullOrEmpty(page.Title.Title)
                    ? typeof(MainFrontMatter)
                    : null,
                ArticleFrontMatterRenderMode = page => string.IsNullOrEmpty(page.Title.Namespace)
                    && string.IsNullOrEmpty(page.Title.Title)
                    ? RenderMode.InteractiveWebAssembly
                    : null,
                CanEditOffline = (_, _, _) => ValueTask.FromResult(true),
                DataStore = provider.GetService<IDataStore>(),
                LoginPath = "/",
                WikiServerApiRoute = server
                    ? null
                    : WikiBlazorClientOptions.DefaultWikiServerApiRoute,
            });
    }
}
