![build](https://img.shields.io/github/actions/workflow/status/Tavenem/Wiki.Blazor/publish.yml?branch=main) [![NuGet downloads](https://img.shields.io/nuget/dt/Tavenem.Wiki.Blazor)](https://www.nuget.org/packages/Tavenem.Wiki.Blazor/)

Tavenem.Wiki.Blazor
==

This is an implementation of [Tavenem.Wiki](https://github.com/Tavenem/Wiki) for [Blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor). It is comprised of a pair of [Razor class libraries](https://docs.microsoft.com/en-us/aspnet/core/razor-pages/ui-class): a **Client** library which can be included in a Blazor client app, and a **Server** library which can be included in an [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core) host project. Working together, this will function as a complete wiki.

It is also possible to use only the client library, and provide your own implementation for the server library. Its source code could easily be adapted to integrate more closely with your main server project, or reimagined as a cloud-native set of functions and APIs, or replaced by any number of other implementations.

It is also possible to use the client library "offline" by providing direct access to wiki data, without the need for any back-end at all. Data might be persisted locally on the client in browser storage, or the local filesystem.

## Installation

Tavenem.Wiki.Blazor is available as a pair of [NuGet](https://www.nuget.org/packages/Tavenem.Wiki.Blazor.Client/) [packages](https://www.nuget.org/packages/Tavenem.Wiki.Blazor.Server/): one for [the client library](https://www.nuget.org/packages/Tavenem.Wiki.Blazor.Client/), and one for [the server library](https://www.nuget.org/packages/Tavenem.Wiki.Blazor.Server/).

The client package should be installed in your Blazor client app, and the server library can optionally be installed in your host app.

## Configuration

In order to use Tavenem.Wiki.Blazor, the following steps should be taken:

### The Client App

1. Call one of the overloads of `AddWikiClient` on an `IServiceCollection` instance in your `Program.cs` file.

    For example:
    ```csharp
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.Services.AddWikiClient();
    ```

   `AddWikiClient` has two optional parameters.

   The first parameter is either an instance of `WikiOptions` or a function which provides one. This interface allows you to configure the wiki's core features. See the README for [Tavenem.Wiki](https://github.com/Tavenem/Wiki) for more information.
   
   The second parameter is either an instance of `WikiBlazorClientOptions` or a function which provides one.
   This interface allows you to configure the wiki's Blazor implementation features, and includes the following properties:
   - `AppBar`: The type of an optional component (typically containing an [AppBar](https://tavenem.com/Blazor.Framework/components/appbar) from the [Tavenem Blazor Framework](https://tavenem.com/Blazor.Framework/)) which will appear at the top of wiki pages.
   - `AppBarRenderMode`: The [render mode](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes) to use for the `AppBar` component, or `null` to use static rendering.
   
     The type must implement `IComponent`.
   - `ArticleFrontMatter` and `ArticleEndMatter`: these can be set to functions which accept an `Article` parameter and should return type of a component which should be displayed before or after the content of the given wiki article (before the category list), or null if no additional component should be displayed.
   - `ArticleFrontMatterRenderMode` and `ArticleEndMatterRenderMode`: The [render mode](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes) to use for the `ArticleFrontMatter` and `ArticleEndMatter` components, or `null` to use static rendering.
   - `CanEditOffline`: Can be set to a function which determines whether content may be edited locally.

     If this function is not defined, no content may be edited locally (i.e. local content may only be viewed).
   - `CompactLayout`: The type of layout used when requesting a compact version of a wiki page. Wiki pages will be nested within this layout.
   
     If omitted, a default layout will be used.
   - `CompactRouteHostPart`: The host part which will be recognized as indicating a request for the compact version of the wiki.

     If left empty the compact view cannot be reached at a particular host path.
   - `CompactRouteHostPosition`: The position (zero-based) within the parts of the host string which will be examined to determine a request for the compact version of the wiki.

     If left null position zero will be assumed.

     Only used when `CompactRouteHostPart` is non-empty.
   - `CompactRoutePort`: The port which will be recognized as indicating a request for the compact version of the wiki.

     If left null the compact view cannot be reached at a particular port.
   - `DataStore`: An optional data store which the client can access directly (i.e. without reaching the server).

     If the `WikiServerApiRoute` has also been defined, the client will try to reach the server first for all wiki operations. If the server cannot be reached or the requested content is unavailable at the server, the client will fall back to the local data store.

     If both the server and the local data store are unavailable, the wiki will remain operational, but will show no content and will not allow any content to be added.

     No automatic synchronization occurs from the local data store to the server (for instance when an offline client reestablishes network connectivity). If your app model requires synchronization of offline content to a server, that logic must be implemented separately.
   - `IsOfflineDomain`: A function which determines whether the given domain should always be retrieved from the local `DataStore`, and never from the `WikiServerApiRoute`.
   - `LoginPath`: The relative path to the site's login page.
     
     For security reasons, only a local path is permitted. If your authentication mechanisms are handled externally, this should point to a local page which redirects to that source (either automatically or via interaction).
   
     A query parameter with the name "returnUrl" whose value is set to the page which initiated the logic request will be appended to this URL (if provided). Your login page may ignore this parameter, but to improve user experience it should redirect the user back to this URL after performing a successful login. Be sure to validate that the value of the parameter is from a legitimate source to avoid exploits.
   
     If this option is omitted, a generic "not signed in" message will be displayed whenever a user who is not logged in attempts any action which requires an account.
   - `MainLayout`: The type of the main layout for the wiki. Wiki pages will be nested within this layout.
   
     If omitted, a default layout will be used.
   - `TenorAPIKey`: The API key to be used for [Tenor](https://tenor.com) GIF integration. If omitted, discussion pages will not have built-in GIF functionality.
   - `WikiServerApiRoute`: The relative URL of the wiki's server API.

     
     This is initialized to <see langword="null"/> by default, `WikiBlazorClientOptions.DefaultWikiServerApiRoute` may be assigned to use the default value for a hosting server app with default values.
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

### The Server App

1. (Optional) Call `AddWikiJsonContext` on an `IServiceCollection` instance in your `Program.cs` file. This configures the `JsonOptions` for MVC with the `WikiBlazorJsonSerializerContext` and `WikiJsonSerializerContext` source gererated serializer contexts.

   This is an optional step. If you wish to provide your own JSON serializer context, or use reflection-based JSON serialization, or use an alternative JSON serializer, you should skip this step.

   If you will provide your own context, it is recommended that you combine it with `WikiBlazorJsonSerializerContext` and `WikiJsonSerializerContext` via `JsonTypeInfoResolver.Combine` to ensure that all necessary wiki types are included.

   For example:
   ```csharp
    var resolver = JsonTypeInfoResolver.Combine(
        YourCustomContext.Default,
        WikiBlazorJsonSerializerContext.Default,
        WikiJsonSerializerContext.Default,
        new DefaultJsonTypeInfoResolver());
    services.Configure<JsonOptions>(options =>
        options.JsonSerializerOptions.TypeInfoResolver = resolver);
   ```
1. Call one of the overloads of `AddWikiServer` on an `IServiceCollection` instance in your `Program.cs` file. `AddWikiServer` has two required parameters and three optional parameters.
   
   The first parameter is either an instance of `IWikiUserManager`, or the type of an implementation of that interface which is available via dependency injection, or a function which provides one. This interface allows the wiki to get information about users. Typically this will be a wrapper around your actual user persistence mechanism (e.g. [ASP.NET Core Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)).

   The second parameter is either an instance of `IWikiGroupManager`, or the type of an implementation of that interface which is available via dependency injection, or a function which provides one. This interface allows the wiki to get information about user groups. Typically this will be a wrapper around your actual user group persistence mechanism.

   The next parameter is either an instance of `WikiOptions` or a function which provides one. This interface allows you to configure the wiki's core features. See the README for [Tavenem.Wiki](https://github.com/Tavenem/Wiki) for more information.
   
   The next parameter is either an instance of `IWikiBlazorServerOptions` or a function which provides one. This interface allows you to configure the wiki's Blazor implementation features, and includes the following properties:
   - `DomainArchivePermission`: The minimum permission the user must have in order to create an archive of a domain.
   
     This property does not apply when creating an archive for content without a domain, or for the entire wiki.
     
     Since it would be prohibitive to check individual pages' permission, archiving only requires that a user has this level of permission (defaults to Read) for the target domain. This could represent a potential security breach, if individual pages within the domain are further restricted. It is strongly recommended that the ability to create archives is restricted in your client code in a manner specific to your implementation's use of domains, which guarantees that only those with the correct permissions can create archives.
   - `LoginPath`: The relative path to the site's login page.
     
     For security reasons, only a local path is permitted. If your authentication mechanisms are handled externally, this should point to a local page which redirects to that source (either automatically or via interaction).
   
     A query parameter with the name "returnUrl" whose value is set to the page which initiated the logic request will be appended to this URL (if provided). Your login page may ignore this parameter, but to improve user experience it should redirect the user back to this URL after performing a successful login. Be sure to validate that the value of the parameter is from a legitimate source to avoid exploits.
   
     If this option is omitted, a generic "not signed in" message will be displayed whenever a user who is not logged in attempts any action which requires an account.
   - `WikiServerApiRoute`: The relative URL of the wiki's server API.
   
     If omitted, the path "/wikiapi" will be used.
   
   The next parameter is either an instance of `IFileManager`, or the type of an implementation of that interface which is available via dependency injection, or a function which provides one. If omitted, an instance of `LocalFileManager` will be used, which stores files in a subfolder of wwwroot. Note that you can disable file uploads entirely in `WikiOptions`.

   For example:
   ```csharp
   var builder = WebAssemblyHostBuilder.CreateDefault(args);
   var app = builder.Build();
   app.MapWiki();
   ```
   This call should normally precede any other mapped endpoints.
1. It is possible to implement an authorization policy with the name "WikiPolicy" which will be applied to the built-in wiki controller. Note, however, that the `AllowAnonymous` attribute is applied to this controller. This means that while authentication will be performed according to the rules defined in the policy, access to the actions of the controller will not be denied on the basis of authorization.

## Roadmap

Tavenem.Wiki.Blazor is currently in a **prerelease** state. Development is ongoing, and breaking changes are possible before the first production release.

No release date is currently set for v1.0 of Tavenem.Wiki.Blazor.

## Contributing

Contributions are always welcome. Please carefully read the [contributing](docs/CONTRIBUTING.md) document to learn more before submitting issues or pull requests.

## Code of conduct

Please read the [code of conduct](docs/CODE_OF_CONDUCT.md) before engaging with our community, including but not limited to submitting or replying to an issue or pull request.