using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tavenem.Wiki.Blazor.Example.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var httpClient = new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
builder.Services.AddScoped(_ => httpClient);

builder.Services.AddAuthorizationCore();
await builder.Services.AddWikiClientAsync(false);

await builder.Build().RunAsync();
