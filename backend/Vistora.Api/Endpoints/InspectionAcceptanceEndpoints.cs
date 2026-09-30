using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vistora.Application.Persistence;
using Vistora.Application.Storage;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Api.Endpoints;

public static class InspectionAcceptanceEndpoints
{
    private const string TermsVersion = "vistora-inspection-acceptance-1";

    public static IEndpointRouteBuilder MapInspectionAcceptanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var acceptance = endpoints.MapGroup("/api/v1/inspections/{inspectionId:guid}/acceptance")
            .RequireAuthorization();
        acceptance.MapPost("", CreateAsync).RequireAuthorization(AccessPolicies.EditInspection);
        acceptance.MapGet("", GetAsync);
        acceptance.MapGet("/signature", DownloadAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        Guid inspectionId, CreateInspectionAcceptanceRequest? request, VistoraDbContext db,
        ITenantContext tenant, HttpContext httpContext, IPrivateObjectStorage storage,
        ILogger logger, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId is not { } organizationId) return Results.Unauthorized();
        if (request is null || !request.AcceptedTerms || !SignaturePngValidator.TryDecode(request.SignatureDataUrl, out var png))
            return Results.BadRequest(new { error = "Confirme o aceite e envie uma assinatura PNG válida de até 256 KiB." });
        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Results.Unauthorized();

        var signer = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(
            account => account.Id == actorId && account.OrganizationId == organizationId, cancellationToken);
        if (signer is null) return Results.Unauthorized();
        var inspection = await db.Inspections.SingleOrDefaultAsync(x => x.Id == inspectionId, cancellationToken);
        if (inspection is null) return Results.NotFound();
        if (inspection.Status != InspectionStatus.Completed ||
            !await db.Reports.AnyAsync(x => x.InspectionId == inspectionId, cancellationToken))
            return Results.Conflict(new { error = "vistoria precisa estar concluída e ter relatório antes do aceite" });
        if (await db.InspectionAcceptances.AnyAsync(x => x.InspectionId == inspectionId, cancellationToken))
            return Results.Conflict(new { error = "o aceite desta vistoria já foi registrado", code = "acceptance_exists" });

        var now = DateTimeOffset.UtcNow;
        var acceptance = new InspectionAcceptance
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, InspectionId = inspectionId,
            ActorUserId = actorId, SignerName = signer.Name, SignerEmail = signer.Email,
            SignatureObjectKey = $"{organizationId:N}/inspections/{inspectionId:N}/acceptance/{Guid.NewGuid():N}.png",
            SignatureSha256 = Convert.ToHexString(SHA256.HashData(png)).ToLowerInvariant(),
            TermsVersion = TermsVersion, AcceptedAtUtc = now
        };
        await using var stream = new MemoryStream(png, writable: false);
        await storage.UploadAsync(new StorageUpload(acceptance.SignatureObjectKey, stream, "image/png", png.Length), cancellationToken);
        db.InspectionAcceptances.Add(acceptance);
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, ActorUserId = actorId,
            EventType = "InspectionAccepted", EntityType = "Inspection", EntityId = inspectionId.ToString(),
            OccurredAtUtc = now
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            try
            {
                await storage.DeleteAsync(acceptance.SignatureObjectKey, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                logger.LogError(cleanupException, "Could not remove orphaned signature {ObjectKey} after database failure", acceptance.SignatureObjectKey);
            }
            if (exception is DbUpdateException)
                return Results.Conflict(new { error = "o aceite desta vistoria foi registrado por outra solicitação", code = "acceptance_exists" });
            throw;
        }

        return Results.Created($"/api/v1/inspections/{inspectionId}/acceptance", new
        {
            acceptance.Id, acceptance.SignerName, acceptance.SignerEmail, acceptance.AcceptedAtUtc,
            acceptance.TermsVersion, SignatureUrl = $"/api/v1/inspections/{inspectionId}/acceptance/signature"
        });
    }

    private static async Task<IResult> GetAsync(Guid inspectionId, VistoraDbContext db, CancellationToken cancellationToken)
    {
        var acceptance = await db.InspectionAcceptances.AsNoTracking()
            .SingleOrDefaultAsync(x => x.InspectionId == inspectionId, cancellationToken);
        return acceptance is null
            ? Results.NotFound()
            : Results.Ok(new
            {
                acceptance.Id, acceptance.SignerName, acceptance.SignerEmail,
                acceptance.AcceptedAtUtc, acceptance.TermsVersion,
                SignatureUrl = $"/api/v1/inspections/{inspectionId}/acceptance/signature"
            });
    }

    private static async Task<IResult> DownloadAsync(
        Guid inspectionId, VistoraDbContext db, IPrivateObjectStorage storage, CancellationToken cancellationToken)
    {
        var acceptance = await db.InspectionAcceptances.AsNoTracking()
            .SingleOrDefaultAsync(x => x.InspectionId == inspectionId, cancellationToken);
        if (acceptance is null) return Results.NotFound();
        var signature = await storage.DownloadAsync(acceptance.SignatureObjectKey, 256 * 1024, cancellationToken);
        if (!SHA256.HashData(signature).AsSpan().SequenceEqual(Convert.FromHexString(acceptance.SignatureSha256)))
            return Results.Problem("Signature integrity check failed.", statusCode: StatusCodes.Status500InternalServerError);
        return Results.File(signature, "image/png", $"aceite-vistoria-{inspectionId}.png");
    }
}

public sealed record CreateInspectionAcceptanceRequest(string SignatureDataUrl, bool AcceptedTerms);
