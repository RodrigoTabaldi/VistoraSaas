using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vistora.Application.Idempotency;
using Vistora.Application.Persistence;
using Vistora.Application.UseCases;
using Vistora.Domain;

namespace Vistora.Api.Endpoints;

public static class ChecklistTemplatesEndpoints
{
    public static IEndpointRouteBuilder MapChecklistTemplatesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/checklist-templates", CreateChecklistTemplate)
            .RequireAuthorization()
            .WithName("CreateChecklistTemplate")
            .WithTags("ChecklistTemplates");

        endpoints.MapPost("/api/v1/inspections", CreateInspection)
            .RequireAuthorization()
            .WithName("CreateInspection")
            .WithTags("Inspections");

        return endpoints;
    }

    private static async Task<IResult> CreateChecklistTemplate(
        [FromBody] CreateChecklistTemplateRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] IdempotencyGuard idempotencyGuard,
        [FromServices] CreateChecklistTemplateUseCase useCase,
        [FromServices] ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Results.BadRequest(new { error = "Header 'Idempotency-Key' is required." });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Template name is required." });
        }

        if (tenantContext.OrganizationId is not { } organizationId)
        {
            return Results.BadRequest(new { error = "Organization context is missing." });
        }

        var roomsInput = request.Rooms?
            .Select(r => new CreateChecklistTemplateRoomInput(
                r.Name,
                r.Position,
                r.Items?.Select(i => new CreateChecklistTemplateItemInput(i.Description, i.Position)).ToList()
                    ?? (IReadOnlyList<CreateChecklistTemplateItemInput>)Array.Empty<CreateChecklistTemplateItemInput>()))
            .ToList() ?? new List<CreateChecklistTemplateRoomInput>();

        var outcome = await idempotencyGuard.ExecuteAsync(
            organizationId,
            idempotencyKey,
            async () => await useCase.ExecuteAsync(request.Name, roomsInput, cancellationToken),
            cancellationToken);

        if (outcome.Kind == IdempotencyResultKind.Conflict)
        {
            return Results.Conflict(new { error = "An operation with this Idempotency-Key is currently in progress." });
        }

        var id = outcome.Result;
        return Results.Created($"/api/v1/checklist-templates/{id}", new { checklistTemplateId = id });
    }

    private static async Task<IResult> CreateInspection(
        [FromBody] CreateInspectionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] IdempotencyGuard idempotencyGuard,
        [FromServices] CreateInspectionFromTemplateUseCase useCase,
        [FromServices] ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Results.BadRequest(new { error = "Header 'Idempotency-Key' is required." });
        }

        if (request is null || request.UnitId == Guid.Empty || request.ChecklistTemplateId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "UnitId and ChecklistTemplateId are required." });
        }

        if (!Enum.TryParse<InspectionType>(request.Type, true, out var inspectionType))
        {
            return Results.BadRequest(new { error = $"Invalid inspection type '{request.Type}'. Allowed values: MoveIn, MoveOut." });
        }

        if (tenantContext.OrganizationId is not { } organizationId)
        {
            return Results.BadRequest(new { error = "Organization context is missing." });
        }

        var outcome = await idempotencyGuard.ExecuteAsync(
            organizationId,
            idempotencyKey,
            async () => await useCase.ExecuteAsync(request.UnitId, request.ChecklistTemplateId, inspectionType, cancellationToken),
            cancellationToken);

        if (outcome.Kind == IdempotencyResultKind.Conflict)
        {
            return Results.Conflict(new { error = "An operation with this Idempotency-Key is currently in progress." });
        }

        return outcome.Result switch
        {
            CreateInspectionResult.Created created =>
                Results.Created($"/api/v1/inspections/{created.InspectionId}", new { inspectionId = created.InspectionId }),

            CreateInspectionResult.TemplateNotFound =>
                Results.NotFound(new { error = $"ChecklistTemplate '{request.ChecklistTemplateId}' was not found or is inactive." }),

            CreateInspectionResult.UnitNotFound =>
                Results.NotFound(new { error = $"Unit '{request.UnitId}' was not found." }),

            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}

public sealed record CreateChecklistTemplateRequest(
    string Name,
    List<CreateChecklistTemplateRoomRequest>? Rooms);

public sealed record CreateChecklistTemplateRoomRequest(
    string Name,
    int Position,
    List<CreateChecklistTemplateItemRequest>? Items);

public sealed record CreateChecklistTemplateItemRequest(
    string Description,
    int Position);

public sealed record CreateInspectionRequest(
    Guid UnitId,
    Guid ChecklistTemplateId,
    string Type);
