using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Tavenem.Wiki.Blazor.Example.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(new(new ClaimsIdentity(
        [
            new Claim("sub", DefaultUserManager.User.Id),
            new Claim("preferred_username", DefaultUserManager.User.DisplayName!),
        ]))));
}
