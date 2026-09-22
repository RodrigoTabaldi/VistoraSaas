using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vistora.Application.Idempotency;
using Vistora.Application.Persistence;
using Vistora.Application.UseCases;

namespace Vistora.Api.Endpoints;

public static class InspectionsEndpoints
{
    public static IEndpointRouteBuilder MapInspectionsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/inspections/{inspectionId:guid}/complete", CompleteInspection)
            .WithName("CompleteInspection")
            .WithTags("Inspections");

        return endpoints;
    }

    private static async Task<IResult> CompleteInspection(
        [FromRoute] Guid inspectionId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKeyHeader,
        [FromServices] IdempotencyGuard idempotencyGuard,
        [FromServices] CompleteInspectionUseCase useCase,
        [FromServices] ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (tenantContext.OrganizationId is not { } organizationId)
        {
            return Results.BadRequest(new { error = "Organization context is missing." });
        }

        var outcome = await idempotencyGuard.ExecuteAsync(
            organizationId,
            idempotencyKeyHeader,
            async () => await useCase.ExecuteAsync(inspectionId, idempotencyKeyHeader ?? string.Empty, cancellationToken),
            cancellationToken);

        if (outcome.Kind == IdempotencyResultKind.Conflict)
        {
            return Results.Conflict(new { error = "An operation with this Idempotency-Key is currently in progress." });
        }

        return outcome.Result switch
        {
            CompleteInspectionResult.Created created =>
                Results.Accepted($"/api/v1/inspections/{inspectionId}/report-jobs/{created.ReportJobId}", new { reportJobId = created.ReportJobId }),

            CompleteInspectionResult.AlreadyInProgress inProgress =>
                Results.Ok(new { reportJobId = inProgress.ExistingReportJobId, message = "Report generation job for this inspection is already in progress." }),

            CompleteInspectionResult.NotFound =>
                Results.NotFound(new { error = $"Inspection '{inspectionId}' was not found." }),

            CompleteInspectionResult.InvalidState =>
                Results.Conflict(new { error = "Inspection cannot be completed because its current status is not eligible." }),

            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
