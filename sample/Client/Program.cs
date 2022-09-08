using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tavenem.Wiki;
using Tavenem.Wiki.Blazor.Client;
using Tavenem.Wiki.Blazor.Sample.Client;
using Tavenem.Wiki.Blazor.Sample.Client.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddTavenemWikiClient(
    new WikiOptions
    {
        ContactPageTitle = null,
        ContentsPageTitle = null,
        CopyrightPageTitle = null,
        LinkTemplate = WikiBlazorClientOptions.DefaultLinkTemplate,
        MaxFileSize = 0,
        PolicyPageTitle = null,
    },
    new WikiBlazorClientOptions()
    {
        AppBar = typeof(TopAppBar),
        LoginPath = "/",
    });

await builder.Build().RunAsync();
