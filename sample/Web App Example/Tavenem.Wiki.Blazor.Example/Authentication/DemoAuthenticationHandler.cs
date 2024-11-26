using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Tavenem.Wiki.Blazor.Example.Services;

namespace Tavenem.Wiki.Blazor.Example.Authentication;

public class DemoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "DemoAuth";

    /// <summary>
    /// Initializes a new instance of <see cref="AuthenticationHandler{TOptions}"/>.
    /// </summary>
    /// <param name="options">The monitor for the options instance.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    public DemoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("sub", DefaultUserManager.User.Id),
                    new Claim("preferred_username", DefaultUserManager.User.DisplayName!),
                ],
                AuthenticationScheme)),
            new AuthenticationProperties(),
            AuthenticationScheme!)));
}
