using Tavenem.Wiki.Blazor.Client;

namespace Tavenem.Wiki.Blazor.Example;

public static class ExampleWikiOptions
{
    public static readonly WikiOptions Instance = new()
    {
        ContactPageTitle = null,
        ContentsPageTitle = null,
        CopyrightPageTitle = null,
        LinkTemplate = WikiBlazorClientOptions.DefaultLinkTemplate,
        MaxFileSize = 0,
        PolicyPageTitle = null,
    };
}
