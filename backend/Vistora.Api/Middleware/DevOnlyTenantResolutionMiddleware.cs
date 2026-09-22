using Microsoft.AspNetCore.Http;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Api.Middleware;

/// <summary>
/// TEMPORARY / DEV-ONLY. Resolves the current tenant from a plain HTTP header
/// ("X-Organization-Id") with NO authentication or authorization behind it. This exists only to
/// unblock end-to-end testing of endpoints before real authentication (JWT/OIDC) is implemented.
/// 
/// SECURITY WARNING: this trusts a client-supplied header at face value. Anyone can claim to be
/// any organization. This MUST be replaced with real authentication (resolving the organization
/// from a validated JWT claim, not a raw header) before this API is exposed outside a trusted
/// development environment. Do not build any other security assumption on top of this.
/// </summary>
public sealed class DevOnlyTenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Organization-Id", out var headerValues) ||
            !Guid.TryParse(headerValues.ToString(), out var organizationId) ||
            organizationId == Guid.Empty)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Header 'X-Organization-Id' is required and must be a valid non-empty Guid (Dev-Only mode)."
            });
            return;
        }

        tenantContext.SetOrganization(organizationId);
        await next(context);
    }
}
