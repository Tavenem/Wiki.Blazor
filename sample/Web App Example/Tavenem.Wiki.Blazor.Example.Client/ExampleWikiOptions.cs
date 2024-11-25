using Microsoft.AspNetCore.Components.Web;
using Tavenem.Wiki.Blazor.Client;
using Tavenem.Wiki.Blazor.Example.Client;

namespace Tavenem.Wiki.Blazor.Example;

public static class ExampleWikiOptions
{
    public static readonly WikiBlazorOptions Instance = new()
    {
        AppBar = typeof(TopAppBar),
        AppBarRenderMode = RenderMode.InteractiveWebAssembly,
        ContactPageTitle = null,
        ContentsPageTitle = null,
        CopyrightPageTitle = null,
        LoginPath = "/",
        MaxFileSize = 0,
        PolicyPageTitle = null,
        WikiServerApiRoute = WikiBlazorOptions.DefaultWikiServerApiRoute,
    };
}
