using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using System.Threading.Tasks;

namespace Messaging
{
    /// <summary>
    /// Authorization requirement satisfied by EITHER an authenticated ops.acceleratenetworks.com Identity user
    /// OR a valid FusionPBX API key presented as a bearer token. This lets FusionPBX users reach the same
    /// endpoints as operators; per-number ownership is then enforced inside each endpoint.
    /// </summary>
    public sealed class ApiAccessRequirement : IAuthorizationRequirement { }

    public sealed class ApiAccessHandler(IHttpContextAccessor httpContextAccessor, AppSettings appSettings)
        : AuthorizationHandler<ApiAccessRequirement>
    {
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ApiAccessRequirement requirement)
        {
            // Existing behavior: an authenticated Identity user (operator) is always allowed.
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                context.Succeed(requirement);
                return;
            }

            // New behavior: a valid, enabled FusionPBX API key in the bearer token is allowed.
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                var user = await FusionPBXAuth.AuthenticateAsync(httpContext, appSettings.ConnectionStrings.FusionPBXConnectionString);
                if (user.IsAuthenticated())
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
