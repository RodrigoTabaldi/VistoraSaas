using System.Security.Claims;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Api;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IWebHostEnvironment environment,
    ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var claim = context.User.FindFirstValue("organization_id")
            ?? context.User.FindFirstValue("tenant_id")
            ?? context.User.FindFirstValue("org_id");

        if (Guid.TryParse(claim, out var organizationId))
        {
            tenantContext.SetOrganization(organizationId);
        }
        else if (environment.IsDevelopment()
                 && Guid.TryParse(context.Request.Headers["X-Organization-Id"], out organizationId))
        {
            tenantContext.SetOrganization(organizationId);
            logger.LogWarning("Using X-Organization-Id development tenant fallback for {Path}.", context.Request.Path);
        }

        await next(context);
    }
}
