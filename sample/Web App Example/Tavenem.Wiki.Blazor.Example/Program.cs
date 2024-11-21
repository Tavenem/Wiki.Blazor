using Microsoft.AspNetCore.Components.Authorization;
using System.Text.Json;
using Tavenem.DataStorage;
using Tavenem.Wiki;
using Tavenem.Wiki.Blazor.Example;
using Tavenem.Wiki.Blazor.Example.Components;
using Tavenem.Wiki.Blazor.Example.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthenticationStateProvider>());

var userManager = new DefaultUserManager();
builder.Services.AddScoped<IWikiUserManager>(_ => userManager);

builder.Services.AddScoped<WikiGroupManager>();
builder.Services.AddScoped<IWikiGroupManager>(services =>
    services.GetRequiredService<WikiGroupManager>());

var dataStore = new InMemoryDataStore();
builder.Services.AddScoped<IDataStore>(_ => dataStore);

builder.Services.AddWikiServer(
    ExampleWikiOptions.Instance,
    options => options.ConfigureUserManager(typeof(DefaultUserManager)));

var archiveText = File.ReadAllText(Path.Combine(builder.Environment.WebRootPath, "archive.json"));
var archive = JsonSerializer.Deserialize<Archive>(archiveText, WikiArchiveJsonSerializerOptions.Instance);
if (archive is not null)
{
    await archive.RestoreAsync(dataStore, ExampleWikiOptions.Instance, "sample");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapWiki();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(Tavenem.Wiki.Blazor.Example.Client._Imports).Assembly,
        typeof(Tavenem.Wiki.Blazor.Client._Imports).Assembly);

app.Run();
