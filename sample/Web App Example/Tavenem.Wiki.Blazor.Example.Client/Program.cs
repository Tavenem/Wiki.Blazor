using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tavenem.Wiki.Blazor.Example;
using Tavenem.Wiki.Blazor.Example.Client.Services;
using Tavenem.Wiki.Blazor.Example.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var httpClient = new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
builder.Services.AddScoped(_ => httpClient);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthenticationStateProvider>());

builder.Services.AddWikiClient(
    ExampleWikiOptions.Instance,
    config =>
    {
        config.ConfigureArticleRenderManager(typeof(CustomArticleRenderManager));
        config.ConfigureOfflineManager(typeof(CustomOfflineManager));
    });

await builder.Build().RunAsync();
