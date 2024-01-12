using Tavenem.Wiki;
using Tavenem.Wiki.Blazor;
using Tavenem.Wiki.Blazor.Example;
using Tavenem.Wiki.Blazor.Example.Components;
using Tavenem.Wiki.Blazor.Example.Services;
using Tavenem.Wiki.Blazor.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWikiServer(
    typeof(DefaultUserManager),
    typeof(WikiGroupManager),
    _ => ExampleWikiOptions.Instance,
    _ => new WikiBlazorServerOptions
    {
        LoginPath = "/account/login",
    },
    null,
    null);

builder.Services.AddAuthorization();
await builder.Services.AddWikiClientAsync(true, builder.Environment.WebRootPath);

builder.Services.AddScoped<WikiDataManager>();

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

app.MapWiki();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(Tavenem.Wiki.Blazor.Example.Client._Imports).Assembly,
        typeof(Tavenem.Wiki.Blazor.Client._Imports).Assembly);

app.Run();
