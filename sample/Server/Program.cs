using Tavenem.DataStorage;
using Tavenem.Wiki;
using Tavenem.Wiki.Blazor.Sample.Server.Data;
using Tavenem.Wiki.Blazor.Sample.Server.Services;
using Tavenem.Wiki.Blazor.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var dataStore = new InMemoryDataStore();
builder.Services.AddSingleton<IDataStore>(dataStore);

builder.Services.AddWikiJsonContext();
builder.Services.AddWiki(
    typeof(WikiUserManager),
    typeof(WikiGroupManager),
    new WikiOptions
    {
        ContactPageTitle = null,
        ContentsPageTitle = null,
        CopyrightPageTitle = null,
        LinkTemplate = WikiBlazorServerOptions.DefaultLinkTemplate,
        MaxFileSize = 0,
        PolicyPageTitle = null,
    },
    new WikiBlazorServerOptions
    {
        LoginPath = "/",
    });

var app = builder.Build();

var serviceProvider = app.Services.CreateScope().ServiceProvider;
await Seed.AddDefaultWikiPagesAsync(
    serviceProvider.GetRequiredService<WikiOptions>(),
    serviceProvider.GetRequiredService<IDataStore>(),
    WikiUserManager.User.Id);

// Configure the HTTP request pipeline.
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapWiki();
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
