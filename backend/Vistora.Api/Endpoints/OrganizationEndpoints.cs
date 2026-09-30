using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vistora.Application.Persistence;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var organization = endpoints.MapGroup("/api/v1/organization").RequireAuthorization();
        organization.MapGet("", GetAsync);
        organization.MapPatch("", UpdateAsync).RequireAuthorization(AccessPolicies.ManageOrganization);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(VistoraDbContext db, ITenantContext tenant, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId is not { } organizationId) return Results.Unauthorized();
        var organization = await db.Organizations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == organizationId, cancellationToken);
        return organization is null ? Results.NotFound() : Results.Ok(new
        {
            organization.Id, organization.Name, organization.Document, organization.OperationalEmail
        });
    }

    private static async Task<IResult> UpdateAsync(
        UpdateOrganizationRequest? request, VistoraDbContext db, ITenantContext tenant,
        HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId is not { } organizationId) return Results.Unauthorized();
        if (request is null || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200 ||
            (request.Document?.Trim().Length ?? 0) > 32 ||
            (request.OperationalEmail is { Length: > 320 } ||
             !string.IsNullOrWhiteSpace(request.OperationalEmail) && !new EmailAddressAttribute().IsValid(request.OperationalEmail.Trim())))
            return Results.BadRequest(new { error = "Revise o nome, documento e e-mail operacional informados." });

        var organization = await db.Organizations.SingleOrDefaultAsync(x => x.Id == organizationId, cancellationToken);
        if (organization is null) return Results.NotFound();
        organization.Name = request.Name.Trim();
        organization.Document = string.IsNullOrWhiteSpace(request.Document) ? null : request.Document.Trim();
        organization.OperationalEmail = string.IsNullOrWhiteSpace(request.OperationalEmail)
            ? null
            : request.OperationalEmail.Trim();
        var now = DateTimeOffset.UtcNow;
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            ActorUserId = Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null,
            EventType = "OrganizationSettingsUpdated", EntityType = "Organization",
            EntityId = organizationId.ToString(), OccurredAtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { organization.Id, organization.Name, organization.Document, organization.OperationalEmail });
    }
}

public sealed record UpdateOrganizationRequest(string Name, string? Document, string? OperationalEmail);
