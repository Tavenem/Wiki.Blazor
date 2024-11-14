using Microsoft.AspNetCore.Authorization;

namespace Tavenem.Wiki.Blazor.Server.Authorization;

internal class WikiEditAuthorizationHandler : AuthorizationHandler<WikiEditRequirement, PageTitle>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WikiEditRequirement requirement,
        PageTitle resource)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
