![build](https://img.shields.io/github/workflow/status/Tavenem/Wiki.Blazor/publish/main) [![NuGet downloads](https://img.shields.io/nuget/dt/Tavenem.Wiki.Blazor)](https://www.nuget.org/packages/Tavenem.Wiki.Blazor/)

Tavenem.Wiki.Blazor
==

This is the "reference" implementation of [Tavenem.Wiki](https://github.com/Tavenem/Wiki) for
[Blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor). It is comprised of a pair of [Razor
class libraries](https://docs.microsoft.com/en-us/aspnet/core/razor-pages/ui-class): a **Client**
library which can be included in a Blazor client app, and a **Server** library which can be included in an [ASP.NET
Core](https://docs.microsoft.com/en-us/aspnet/core) host project. Working together, this [hosted Blazor project](https://docs.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly#hosted-deployment-with-aspnet-core) will function as a complete wiki.

It is also possible to use only one of the libraries, and provide your own implementation for the other. The server library, in particular, is very bare-bones. Its source code could easily be adapted to integrate more closely with your main server project, or reimagined as a cloud-native set of functions and APIs, or replaced by any number of other implementations.

## Installation

Tavenem.Wiki.Blazor is available as a [NuGet package](https://www.nuget.org/packages/Tavenem.Wiki.Blazor/).

## Configuration

In order to use Tavenem.Wiki.Blazor, the following steps should be taken:

### The Client App

1. Call one of the overloads of `AddTavenemWikiClient` on an `IServiceCollection` instance in your `Program.cs` file.

    For example:
    ```csharp
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.Services.AddTavenemWikiClient();
    ```

   `AddTavenemWikiClient` has two optional parameters.

   The first parameter is either an instance of `WikiOptions` or a function which provides one. This
   interface allows you to configure the wiki's core features. See the README for
   [Tavenem.Wiki](https://github.com/Tavenem/Wiki) for more information.
   
   The second parameter is either an instance of `IWikiBlazorClientOptions` or a function which provides one.
   This interface allows you to configure the wiki's Blazor implementation features, and includes the
   following properties:
   - `AppBar`: The type of an optional component (typically containing an [AppBar](https://tavenem.com/Blazor.Framework/components/appbar) from the [Tavenem Blazor Framework](https://tavenem.com/Blazor.Framework/)) which will appear at the top of wiki pages.
   
     The type must implement `IComponent`.
   - `CompactLayout`: The type of layout used when requesting a compact version of a wiki page. Wiki pages will be nested within this layout.
   
     If omitted, a default layout will be used.
   - `CompactRouteHostPart`: The host part which will be recognized as indicating a request for the
     compact version of the wiki.

     If left empty the compact view cannot be reached at a particular host path.
   - `CompactRouteHostPosition`: The position (zero-based) within the parts of the host string which
     will be examined to determine a request for the compact version of the wiki.

     If left null position zero will be assumed.

     Only used when `CompactRouteHostPart` is non-empty.
   - `CompactRoutePort`: The port which will be recognized as indicating a request for the compact
     version of the wiki.

     If left null the compact view cannot be reached at a particular port.
   - `LoginPath`: The relative path to the site's login page.
     
     For security reasons, only a local path is permitted. If your authentication mechanisms are
     handled externally, this should point to a local page which redirects to that source (either
     automatically or via interaction).
   
     A query parameter with the name "returnUrl" whose value is set to the page which initiated the
     logic request will be appended to this URL (if provided). Your login page may ignore this
     parameter, but to improve user experience it should redirect the user back to this URL after
     performing a successful login. Be sure to validate that the value of the parameter is from a
     legitimate source to avoid exploits.
   
     If this option is omitted, a generic "not signed in" message will be displayed whenever a user
     who is not logged in attempts any action which requires an account.
   - `MainLayout`: The type of the main layout for the wiki. Wiki pages will be nested within this layout.
   
     If omitted, a default layout will be used.
   - `TalkHubRoute`: The relative path to the
     [SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction)
     [Hub](https://docs.microsoft.com/en-us/aspnet/core/signalr/hubs) used for discussion pages. If
     omitted, the path "/wikiTalkHub" will be used.
   - `TenorAPIKey`: The API key to be used for [Tenor](https://tenor.com) GIF integration. If
     omitted, discussion pages will not have built-in GIF functionality.
   - `WikiServerApiRoute`: The relative URL of the wiki's server API.
   
     If omitted, the path "/wikiapi" will be used.

   In addition there are two interface methods which can be overridden: `GetArticleFrontMatter` and
   `GetArticleEndMatter` these accept an `Article` parameter and should return type of a component
   which should be displayed after the content of the given wiki article (before the category list),
   or null if no additional component should be displayed.
   
   If you provide an instance of the `WikiBlazorClientOptions` class, these methods can be provided
   by overriding the `ArticleFrontMatter` and `ArticleEndMatter` properties, which accept functions.
1. Add a page with the following content to your client:
   ```csharp
   @page "/wiki/{*route}"
   <Wiki />

   @code {
       [Parameter] public string? Route { get; set; }
   }
   ```
   Replace "wiki" in the page route with your preferred wiki route prefix (which should match what your configure in your `WikiOptions` instance).

   This page will handle requests for wiki pages.
1. (Optional) In your main `App.razor` component, place a `Wiki` component in the `NotFound` content slot of your `Router` component. This will allow the wiki to handle requests for unrecognized routes (i.e. users who do not add your wiki prefix to a typed URL will still get to the expected page). Routes which do not match wiki content will display an "article not found" wiki page.

   If you prefer not to handle unrecognized routes as requests for wiki pages, this step can be skipped.
1. In your `index.html` page, add the following content to the `head` section:
   ```html
   <link href="https://fonts.googleapis.com/css2?family=Encode+Sans+SC:wdth,wght@75,100..900&family=Recursive:slnt,wght,CASL,MONO@-15..0,300..1000,0..1,0..1&display=swap" rel="stylesheet">
   <link href="https://fonts.googleapis.com/icon?family=Material+Icons|Material+Icons+Outlined" rel="stylesheet">
   <link href="_content/Tavenem.Blazor.Framework/framework.css" rel="stylesheet">
   <link href="_content/Tavenem.Wiki.Blazor.Client/wiki.css" rel="stylesheet">
   <link href="_content/Tavenem.Wiki.Blazor.Client/Tavenem.Wiki.Blazor.Client.bundle.scp.css" rel="stylesheet">
   ```
   This adds `Tavenem.Wiki.Blazor` styles, and dependency styles for the [Tavenem Blazor
   Framework](https://tavenem.com/Blazor.Framework/). The font choices can be adapted to suit your
   needs. See the Tavenem Blazor Framework documentation for details.

### The Server App

1. (Optional) Call `AddWikiJsonContext` on an `IServiceCollection` instance in your `Program.cs`
   file. This configures the `JsonOptions` for MVC with the `WikiBlazorJsonSerializerContext` source
   gererated serializer context. It also adds
   [SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction), configures the JSON
   protocol to use the same `WikiBlazorJsonSerializerContext`, and adds response compression.

   This is an optional step. If you wish to provide your own JSON serializer context (or not), you should call `AddSignalR` directly, and optionally call `AddResponseCompression`.
1. Call one of the overloads of `AddWiki` on an `IServiceCollection` instance in your `Program.cs`
   file. `AddWiki` has two required parameters and four optional parameters.
   
   The first parameter is either an instance of `IWikiUserManager`, or the type of an implementation
   of that interface which is available via dependency injection, or a function which provides one.
   This interface allows the wiki to get information about users. Typically this will be a wrapper
   around your actual user persistence mechanism (e.g. [ASP.NET Core
   Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)).

   The second parameter is either an instance of `IWikiGroupManager`, or the type of an
   implementation of that interface which is available via dependency injection, or a function which
   provides one. This interface allows the wiki to get information about user groups. Typically this
   will be a wrapper around your actual user group persistence mechanism.

   The next parameter is either an instance of `WikiOptions` or a function which provides one.
   This interface allows you to configure the wiki's core features. See the README for
   [Tavenem.Wiki](https://github.com/Tavenem/Wiki) for more information.
   
   The next parameter is either an instance of `IWikiBlazorServerOptions` or a function which
   provides one. This interface allows you to configure the wiki's Blazor implementation features,
   and includes the following properties:
   - `LoginPath`: The relative path to the site's login page.
     
     For security reasons, only a local path is permitted. If your authentication mechanisms are
     handled externally, this should point to a local page which redirects to that source (either
     automatically or via interaction).
   
     A query parameter with the name "returnUrl" whose value is set to the page which initiated the
     logic request will be appended to this URL (if provided). Your login page may ignore this
     parameter, but to improve user experience it should redirect the user back to this URL after
     performing a successful login. Be sure to validate that the value of the parameter is from a
     legitimate source to avoid exploits.
   
     If this option is omitted, a generic "not signed in" message will be displayed whenever a user
     who is not logged in attempts any action which requires an account.
   - `TalkHubRoute`: The relative path to the
     [SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction)
     [Hub](https://docs.microsoft.com/en-us/aspnet/core/signalr/hubs) used for discussion pages. If
     omitted, the path "/wikiTalkHub" will be used.
   - `WikiServerApiRoute`: The relative URL of the wiki's server API.
   
     If omitted, the path "/wikiapi" will be used.
   
   The next parameter is either an instance of `IFileManager`, or the type of an implementation of
   that interface which is available via dependency injection, or a function which provides one. If
   omitted, an instance of `LocalFileManager` will be used, which stores files in a subfolder of
   wwwroot.
   
   The next parameter is either an instance of `ISearchClient`, or the type of an implementation
   of that interface which is available via dependency injection, or a function which provides one.
   If omitted, an instance of `DefaultSearchClient` will be used.
     
   Note: the `DefaultSearchClient` is not recommended for production use. It is provided only to
   ensure that basic search functionality operates when an implementation of `ISearchClient` is not
   available (e.g. during debugging if the production client cannot be used during development).
1. Call `UseResponseCompression` on an `IApplicationBuilder` instance in your `Program.cs` file. This should normally only be done if you called `AddWikiJsonContext` or if you called `AddResponseCompression` directly.
1. Call `MapWiki` on an `IEndpointRouteBuilder` instance in your `Program.cs` file.

   For example:
   ```csharp
   var builder = WebAssemblyHostBuilder.CreateDefault(args);
   var app = builder.Build();
   app.MapWiki();
   ```
   This call should normally precede any other mapped endpoints.

## Roadmap

Tavenem.Wiki.Blazor is currently in a **prerelease** state. Development is ongoing, and breaking
changes are possible before the first production release.

No release date is currently set for v1.0 of Tavenem.Wiki.Blazor. The project is currently in a "wait
and see" phase while [Tavenem.DataStore](https://github.com/Tavenem/DataStore) (a dependency of
Tavenem.Wiki.Blazor) is in prerelease. When that project has a stable release, a production release of
Tavenem.Wiki.Blazor will follow.

## Contributing

Contributions are always welcome. Please carefully read the [contributing](docs/CONTRIBUTING.md) document to learn more before submitting issues or pull requests.

## Code of conduct

Please read the [code of conduct](docs/CODE_OF_CONDUCT.md) before engaging with our community, including but not limited to submitting or replying to an issue or pull request.