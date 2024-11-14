using Microsoft.AspNetCore.Authorization;

namespace Tavenem.Wiki.Blazor.Server.Authorization;

/// <summary>
/// An <see cref="IAuthorizationRequirement"/> for editing the wiki.
/// </summary>
public class WikiEditRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// A static default instance of the requirement.
    /// </summary>
    public static readonly WikiEditRequirement Instance = new();
}
