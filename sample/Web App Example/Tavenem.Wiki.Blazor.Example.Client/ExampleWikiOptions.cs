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
        ArticleFrontMatter = page => string.IsNullOrEmpty(page.Title.Namespace)
            && string.IsNullOrEmpty(page.Title.Title)
            ? typeof(MainFrontMatter)
            : null,
        ArticleFrontMatterRenderMode = page => string.IsNullOrEmpty(page.Title.Namespace)
            && string.IsNullOrEmpty(page.Title.Title)
            ? RenderMode.InteractiveWebAssembly
            : null,
        CanEditOffline = (_, _, _) => ValueTask.FromResult(true),
        ContactPageTitle = null,
        ContentsPageTitle = null,
        CopyrightPageTitle = null,
        LoginPath = "/",
        MaxFileSize = 0,
        PolicyPageTitle = null,
        WikiServerApiRoute = WikiBlazorOptions.DefaultWikiServerApiRoute,
    };
}
