using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Json;
using System.Text.Json;
using Tavenem.DataStorage;
using Tavenem.Wiki;
using Tavenem.Wiki.Blazor;
using Tavenem.Wiki.Blazor.Client;
using Tavenem.Wiki.Blazor.Sample;
using Tavenem.Wiki.Blazor.Sample.Services;
using Tavenem.Wiki.Blazor.Sample.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthenticationStateProvider>());

var httpClient = new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
builder.Services.AddScoped(_ => httpClient);

var dataStore = new InMemoryDataStore();
builder.Services.AddScoped<IDataStore>(_ => dataStore);

var userManager = new DefaultUserManager();
builder.Services.AddScoped<IWikiUserManager>(_ => userManager);

builder.Services.AddScoped<WikiGroupManager>();
builder.Services.AddScoped<IWikiGroupManager>(services =>
    services.GetRequiredService<WikiGroupManager>());

var wikiOptions = new WikiOptions
{
    ContactPageTitle = null,
    ContentsPageTitle = null,
    CopyrightPageTitle = null,
    LinkTemplate = WikiBlazorClientOptions.DefaultLinkTemplate,
    MaxFileSize = 0,
    PolicyPageTitle = null,
};

using (var response = await httpClient.GetAsync("archive.json"))
{
    var archive = await response.Content.ReadFromJsonAsync<Archive>(new JsonSerializerOptions
    {
        TypeInfoResolver = WikiBlazorJsonSerializerContext.Default,
        WriteIndented = true,
    });
    if (archive is not null)
    {
        await archive.RestoreAsync(dataStore, wikiOptions);
    }
}

builder.Services.AddTavenemWikiClient(
    wikiOptions,
    new WikiBlazorClientOptions()
    {
        AppBar = typeof(TopAppBar),
        CanEditOffline = (_, _, _) => ValueTask.FromResult(true),
        DataStore = dataStore,
        LoginPath = "/",
    });

builder.Services.AddScoped<WikiDataManager>();

await builder.Build().RunAsync();
