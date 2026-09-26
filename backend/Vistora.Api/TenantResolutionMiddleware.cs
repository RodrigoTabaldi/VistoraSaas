using System.Security.Claims;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Api;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var claim = context.User.FindFirstValue("organization_id")
            ?? context.User.FindFirstValue("tenant_id")
            ?? context.User.FindFirstValue("org_id");

        if (context.User.Identity?.IsAuthenticated == true && Guid.TryParse(claim, out var organizationId))
        {
            tenantContext.SetOrganization(organizationId);
        }
        await next(context);
    }
}
