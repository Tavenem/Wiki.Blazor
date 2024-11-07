using System.Security.Claims;

namespace Tavenem.Wiki.Blazor.Example.Services;

/// <summary>
/// An implementation of <see cref="IWikiUserManager"/> which always returns a static user.
/// </summary>
public class DefaultUserManager : IWikiUserManager
{
    private const string DefaultId = "c6798a76-7831-4675-959b-2951566ef068";

    public static WikiUser User { get; } = new()
    {
        Id = DefaultId,
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
        => new(string.Equals(userId, DefaultId) ? User : null);

    /// <summary>
    /// Finds and returns a user, if any, who has the specified user name.
    /// </summary>
    /// <param name="userName">The user name to search for.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the user
    /// matching the specified <paramref name="userName"/> if it exists.
    /// </returns>
    public ValueTask<IWikiUser?> FindByNameAsync(string? userName)
        => new(string.Equals(userName, User.DisplayName, StringComparison.OrdinalIgnoreCase) ? User : null);

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
        => new(principal?.Identity?.IsAuthenticated == true
        && principal.HasClaim(x => x.Type == ClaimTypes.NameIdentifier && x.Value == DefaultId)
        ? User
        : null);
}
