using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Wiki.Blazor.Client.Services;

namespace Tavenem.Wiki.Blazor.Example.Client.Services;

public class CustomArticleRenderManager : IArticleRenderManager
{
    /// <inheritdoc />
    [return: DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))]
    public Type? GetArticleEndMatter(Page page) => null;

    /// <inheritdoc />
    public IComponentRenderMode? GetArticleEndMatterRenderMode(Page page) => null;

    /// <inheritdoc />
    [return: DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))]
    public Type? GetArticleFrontMatter(Page page) => string.IsNullOrEmpty(page.Title.Namespace)
        && string.IsNullOrEmpty(page.Title.Title)
        ? typeof(MainFrontMatter)
        : null;

    /// <inheritdoc />
    public IComponentRenderMode? GetArticleFrontMatterRenderMode(Page page) => string.IsNullOrEmpty(page.Title.Namespace)
        && string.IsNullOrEmpty(page.Title.Title)
        ? RenderMode.InteractiveWebAssembly
        : null;
}
