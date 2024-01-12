using System.Security.Claims;

namespace Tavenem.Wiki.Blazor.Example.Services;

/// <summary>
/// An implementation of <see cref="IWikiUserManager"/> which always returns a static user.
/// </summary>
public class DefaultUserManager : IWikiUserManager
{
    public static WikiUser User { get; } = new()
    {
        DisplayName = "User",
        IsWikiAdmin = true,
        UploadLimit = -1,
    };

    /// <summary>
    /// Finds and returns a user, if any, who has the specified <paramref name="userId"/>.
    /// </summary>
    /// <param name="userId">The user ID to search for.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the user
    /// matching the specified <paramref name="userId"/> if it exists.
    /// </returns>
    public ValueTask<IWikiUser?> FindByIdAsync(string? userId)
        => new(string.IsNullOrEmpty(userId) ? null : User);

    /// <summary>
    /// Finds and returns a user, if any, who has the specified user name.
    /// </summary>
    /// <param name="userName">The user name to search for.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the user
    /// matching the specified <paramref name="userName"/> if it exists.
    /// </returns>
    public ValueTask<IWikiUser?> FindByNameAsync(string? userName)
        => new(string.IsNullOrEmpty(userName) ? null : User);

    /// <summary>
    /// Returns the user corresponding to the IdentityOptions.ClaimsIdentity.UserIdClaimType
    /// claim in the <paramref name="principal"/> or <see langword="null"/>.
    /// </summary>
    /// <param name="principal">The principal which contains the user id claim.</param>
    /// <returns>
    /// The user corresponding to the IdentityOptions.ClaimsIdentity.UserIdClaimType claim in
    /// the <paramref name="principal"/> or <see langword="null"/>
    /// </returns>
    public ValueTask<IWikiUser?> GetUserAsync(ClaimsPrincipal? principal)
        => new(User);
}
