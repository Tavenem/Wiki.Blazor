using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace Tavenem.Wiki.Blazor.Client.Services;

/// <summary>
/// Controls the layout and rendering of wiki articles.
/// </summary>
public interface IArticleRenderManager
{
    /// <summary>
    /// Gets the type of a component which should be displayed after the content of the given wiki
    /// article (before the category list).
    /// </summary>
    /// <param name="page">The page for which to get a component type.</param>
    /// <returns>The type of a component.</returns>
    /// <remarks>
    /// The following parameters will be supplied to the component, if they exist:
    /// <list type="bullet">
    /// <listheader>
    /// <term>Name</term>
    /// <description>Value</description>
    /// </listheader>
    /// <item>
    /// <term>Article</term>
    /// <description>
    /// The currently displayed <see cref="Article"/> (may be <see langword="null"/>).
    /// </description>
    /// </item>
    /// <item>
    /// <term>CanEdit</term>
    /// <description>
    /// A boolean indicating whether the current user has permission to edit the displayed <see
    /// cref="Article"/>. Note that this may be <see langword="true"/> even if the article or the
    /// user are <see langword="null"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>User</term>
    /// <description>
    /// The current <see cref="IWikiUser"/> (may be <see langword="null"/>).
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type? GetArticleEndMatter(Page page);

    /// <summary>
    /// Gets the render mode of the component indicated by <see cref="GetArticleEndMatter"/>.
    /// </summary>
    /// <param name="page">The page for which to get a component's render mode.</param>
    /// <returns>
    /// The render mode of a component, or <see langword="null"/> for static rendering.
    /// </returns>
    IComponentRenderMode? GetArticleEndMatterRenderMode(Page page);

    /// <summary>
    /// Gets the type of a component which should be displayed before the content of the given wiki
    /// article (after the subtitle).
    /// </summary>
    /// <param name="page">The page for which to get a component type.</param>
    /// <returns>The type of a component.</returns>
    /// <remarks>
    /// The following parameters will be supplied to the component, if they exist:
    /// <list type="bullet">
    /// <listheader>
    /// <term>Name</term>
    /// <description>Value</description>
    /// </listheader>
    /// <item>
    /// <term>Article</term>
    /// <description>
    /// The currently displayed <see cref="Article"/> (may be <see langword="null"/>).
    /// </description>
    /// </item>
    /// <item>
    /// <term>CanEdit</term>
    /// <description>
    /// A boolean indicating whether the current user has permission to edit the displayed <see
    /// cref="Article"/>. Note that this may be <see langword="true"/> even if the article or the
    /// user are <see langword="null"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>User</term>
    /// <description>
    /// The current <see cref="IWikiUser"/> (may be <see langword="null"/>).
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type? GetArticleFrontMatter(Page page);

    /// <summary>
    /// Gets the render mode of the component indicated by <see cref="GetArticleFrontMatter"/>.
    /// </summary>
    /// <param name="page">The page for which to get a component's render mode.</param>
    /// <returns>
    /// The render mode of a component, or <see langword="null"/> for static rendering.
    /// </returns>
    IComponentRenderMode? GetArticleFrontMatterRenderMode(Page page);
}
