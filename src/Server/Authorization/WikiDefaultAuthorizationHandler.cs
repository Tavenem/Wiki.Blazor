using Microsoft.AspNetCore.Authorization;

namespace Tavenem.Wiki.Blazor.Server.Authorization;

internal class WikiDefaultAuthorizationHandler : AuthorizationHandler<WikiDefaultRequirement, PageTitle>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WikiDefaultRequirement requirement,
        PageTitle resource)
    {
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
