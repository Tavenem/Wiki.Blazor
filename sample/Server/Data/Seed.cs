using Tavenem.DataStorage;

namespace Tavenem.Wiki.Blazor.Sample.Server.Data;

public static class Seed
{
    public static async Task AddDefaultWikiPagesAsync(
        WikiOptions wikiOptions,
        IDataStore dataStore,
        string adminId)
    {
        var welcomeReference = await PageReference
            .GetPageReferenceAsync(dataStore, "Welcome", wikiOptions.TransclusionNamespace)
            .ConfigureAwait(false);
        if (welcomeReference is null)
        {
            _ = await GetDefaultWelcomeAsync(wikiOptions, dataStore, adminId).ConfigureAwait(false);
        }

        var mainReference = await PageReference
            .GetPageReferenceAsync(dataStore, wikiOptions.MainPageTitle, wikiOptions.DefaultNamespace)
            .ConfigureAwait(false);
        if (mainReference is null)
        {
            _ = await GetDefaultMainAsync(wikiOptions, dataStore, adminId).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(wikiOptions.AboutPageTitle))
        {
            var aboutReference = await PageReference
                .GetPageReferenceAsync(dataStore, wikiOptions.AboutPageTitle, wikiOptions.SystemNamespace)
                .ConfigureAwait(false);
            if (aboutReference is null)
            {
                _ = await GetDefaultAboutAsync(wikiOptions, dataStore, adminId).ConfigureAwait(false);
            }
        }

        if (!string.IsNullOrEmpty(wikiOptions.HelpPageTitle))
        {
            var helpReference = await PageReference
                .GetPageReferenceAsync(dataStore, wikiOptions.HelpPageTitle, wikiOptions.SystemNamespace)
                .ConfigureAwait(false);
            if (helpReference is null)
            {
                _ = await GetDefaultHelpAsync(wikiOptions, dataStore, adminId).ConfigureAwait(false);
            }
        }

        var blazorReference = await PageReference
            .GetPageReferenceAsync(dataStore, "Blazor", wikiOptions.DefaultNamespace)
            .ConfigureAwait(false);
        if (blazorReference is null)
        {
            _ = await GetDefaultBlazorAsync(wikiOptions, dataStore, adminId).ConfigureAwait(false);
        }

        var category = await Category.GetCategoryAsync(wikiOptions, dataStore, "System pages");
        if (category is null)
        {
            throw new Exception("Failed to create category during article creation");
        }
        if (!category.MarkdownContent.StartsWith("These are system pages", StringComparison.Ordinal))
        {
            await SetDefaultCategoryAsync(wikiOptions, dataStore, category, adminId).ConfigureAwait(false);
        }
    }

    private static Task<Article> GetDefaultAboutAsync(
        WikiOptions wikiOptions,
        IDataStore dataStore,
        string adminId) => Article.NewAsync(
            wikiOptions,
            dataStore,
            wikiOptions.AboutPageTitle ?? "About",
            adminId,
@$"{{{{Welcome}}}}

The [Tavenem.Wiki](https://github.com/Tavenem/Wiki) package is a [.NET](https://dotnet.microsoft.com) [[w:Wiki||]] library.

Unlike many wiki implementations, the main package (`Tavenem.Wiki`) is implementation-agnostic. It provides a set of core features which can be used to build a web-based wiki, a desktop application, a distributed cloud app with native clients, or any other architecture desired.

The ""reference"" implementation included out-of-the-box ([Tavenem.Wiki.Mvc](https://github.com/Tavenem/Wiki.Mvc)) is a [Razor class library](https://docs.microsoft.com/en-us/aspnet/core/razor-pages/ui-class) which can be included in an [ASP.NET Core MVC](https://docs.microsoft.com/en-us/aspnet/core/mvc/overview) project to turn it into a wiki.

See the [[System:Help|]] page for usage information.

[[{wikiOptions.CategoryNamespace}:System pages]]",
            wikiOptions.SystemNamespace,
            adminId,
            new[] { adminId });

    private static Task<Article> GetDefaultHelpAsync(
        WikiOptions wikiOptions,
        IDataStore dataStore,
        string adminId) => Article.NewAsync(
            wikiOptions,
            dataStore,
            wikiOptions.HelpPageTitle ?? "Help",
            adminId,
@"{{Welcome}}

This page includes various information which will help you to get a [Tavenem.Wiki](https://github.com/Tavenem/Wiki) instance up and running.

For information about the `Tavenem.Wiki` project, see the [[System:About|]] page.

# Markup
The Tavenem Wiki syntax is a custom flavor of markdown. It implements all the features of [CommonMark](http://commonmark.org), as well as many others. The implementation uses [Markdig](https://github.com/lunet-io/markdig), and details of most extensions to standard CommonMark can be found on [its GitHub page](https://github.com/lunet-io/markdig).

# Blazor
The `Tavenem.Wiki.Blazer` package contains a sample/default implementation of `Tavenem.Wiki` for use with a [Blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor) site. This implementation can be used as-is, or you can use the source as the starting point to build your own implementation. See [[Blazor|the Blazor page]] for more information.

[[" + wikiOptions.CategoryNamespace + @":System pages]]
[[" + wikiOptions.CategoryNamespace + ":Help pages]]",
            wikiOptions.SystemNamespace,
            adminId,
            new[] { adminId });

    private static Task<Article> GetDefaultMainAsync(
        WikiOptions wikiOptions,
        IDataStore dataStore,
        string adminId) => Article.NewAsync(
            wikiOptions,
            dataStore,
            wikiOptions.MainPageTitle,
            adminId,
@$"{{{{Welcome}}}}

See the [[System:About|]] page or the [[System:Help|]] page for more information.

[[{wikiOptions.CategoryNamespace}:System pages]]",
            wikiOptions.DefaultNamespace,
            adminId,
            new[] { adminId });

    private static Task<Article> GetDefaultBlazorAsync(
        WikiOptions wikiOptions,
        IDataStore dataStore,
        string adminId) => Article.NewAsync(
            wikiOptions,
            dataStore,
            "Blazor",
            adminId,
@"{{Welcome}}

The [Tavenem.Wiki.Blazor](https://github.com/Tavenem/Wiki.Blazor) package contains a sample/default implementation of [Tavenem.Wiki](https://github.com/Tavenem/Wiki) for use with a [Blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor) site. Note that this isn't a complete website, but rather a [Razor class library](https://docs.microsoft.com/en-us/aspnet/core/razor-pages/ui-class) which can be included in a Blazor project to enable wiki functionality.

[[" + wikiOptions.CategoryNamespace + ":Help pages]]",
            wikiOptions.DefaultNamespace,
            adminId,
            new[] { adminId });

    private static Task<Article> GetDefaultWelcomeAsync(
        WikiOptions wikiOptions,
        IDataStore dataStore,
        string adminId) => Article.NewAsync(
            wikiOptions,
            dataStore,
            "Welcome",
            adminId,
@$"Welcome to the [Tavenem.Wiki](https://github.com/Tavenem/Wiki) sample.

{{{{ifnottemplate|[[{wikiOptions.CategoryNamespace}:System pages]]}}}}",
            wikiOptions.TransclusionNamespace,
            adminId,
            new[] { adminId });

    private static Task SetDefaultCategoryAsync(
        WikiOptions wikiOptions,
        IDataStore dataStore,
        Category category,
        string adminId) => category.ReviseAsync(
            wikiOptions,
            dataStore,
            adminId,
            markdown: "These are system pages in the [Tavenem.Wiki](https://github.com/Tavenem/Wiki) sample [[w:Wiki||]].",
            revisionComment: "Provide a description",
            allowedEditors: new[] { adminId });
}
