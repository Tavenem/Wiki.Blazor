using Microsoft.AspNetCore.Authorization;

namespace Tavenem.Wiki.Blazor.Server.Authorization;

/// <summary>
/// An <see cref="IAuthorizationRequirement"/> for accessing the wiki.
/// </summary>
public class WikiDefaultRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// A static default instance of the requirement.
    /// </summary>
    public static readonly WikiDefaultRequirement Instance = new();
}
