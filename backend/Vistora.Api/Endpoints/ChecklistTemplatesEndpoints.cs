using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Vistora.Application.Idempotency;
using Vistora.Application.Persistence;
using Vistora.Application.UseCases;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Api.Endpoints;

public static class ChecklistTemplatesEndpoints
{
    public static IEndpointRouteBuilder MapChecklistTemplatesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/checklist-templates", async (VistoraDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await db.ChecklistTemplates.AsNoTracking()
                .Where(template => template.IsActive)
                .OrderBy(template => template.Name)
                .Select(template => new { template.Id, template.Name })
                .ToListAsync(cancellationToken)))
            .RequireAuthorization()
            .WithTags("ChecklistTemplates");

        endpoints.MapGet("/api/v1/checklist-templates/manage", ListTemplatesForManagement)
            .RequireAuthorization(AccessPolicies.ManageOrganization)
            .WithTags("ChecklistTemplates");

        endpoints.MapPost("/api/v1/checklist-templates", CreateChecklistTemplate)
            .RequireAuthorization(AccessPolicies.ManageOrganization)
            .WithName("CreateChecklistTemplate")
            .WithTags("ChecklistTemplates");

        endpoints.MapPut("/api/v1/checklist-templates/{templateId:guid}", UpdateChecklistTemplate)
            .RequireAuthorization(AccessPolicies.ManageOrganization)
            .WithTags("ChecklistTemplates");

        endpoints.MapPost("/api/v1/inspections/from-template", CreateInspection)
            .RequireAuthorization(AccessPolicies.EditInspection)
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
        if (!ChecklistTemplateRules.TryValidate(request.Name, roomsInput, out var validationError))
            return Results.BadRequest(new { error = validationError });

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

    private static async Task<IResult> ListTemplatesForManagement(VistoraDbContext db, CancellationToken cancellationToken)
    {
        var templates = await db.ChecklistTemplates.AsNoTracking()
            .Include(template => template.Rooms).ThenInclude(room => room.Items)
            .OrderBy(template => template.Name)
            .Select(template => new
            {
                template.Id, template.Name, template.IsActive, template.RowVersion,
                Rooms = template.Rooms.OrderBy(room => room.Position).Select(room => new
                {
                    room.Name, room.Position,
                    Items = room.Items.OrderBy(item => item.Position).Select(item => new { item.Description, item.Position })
                })
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(templates);
    }

    private static async Task<IResult> UpdateChecklistTemplate(
        Guid templateId, UpdateChecklistTemplateRequest request, VistoraDbContext db, CancellationToken cancellationToken)
    {
        if (request is null || request.RowVersion == 0)
            return Results.BadRequest(new { error = "A valid rowVersion is required." });

        var rooms = request.Rooms?.Select(room => new CreateChecklistTemplateRoomInput(
            room.Name, room.Position,
            room.Items?.Select(item => new CreateChecklistTemplateItemInput(item.Description, item.Position)).ToList()
                ?? (IReadOnlyList<CreateChecklistTemplateItemInput>)Array.Empty<CreateChecklistTemplateItemInput>())).ToList();
        if (!ChecklistTemplateRules.TryValidate(request.Name, rooms, out var validationError))
            return Results.BadRequest(new { error = validationError });

        var template = await db.ChecklistTemplates.Include(x => x.Rooms).ThenInclude(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == templateId, cancellationToken);
        if (template is null) return Results.NotFound();

        db.Entry(template).Property(x => x.RowVersion).OriginalValue = request.RowVersion;
        template.Name = request.Name.Trim();
        template.IsActive = request.IsActive;
        db.Entry(template).Property(x => x.Name).IsModified = true;

        try
        {
            // Template edits replace only the reusable definition; inspections retain copied rooms and items.
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            db.ChecklistTemplateRooms.RemoveRange(template.Rooms);
            template.Rooms.Clear();
            await db.SaveChangesAsync(cancellationToken);
            foreach (var roomInput in rooms!)
            {
                var room = new ChecklistTemplateRoom
                {
                    Id = Guid.NewGuid(), OrganizationId = template.OrganizationId,
                    ChecklistTemplateId = template.Id, Name = roomInput.Name.Trim(), Position = roomInput.Position
                };
                foreach (var itemInput in roomInput.Items)
                    room.Items.Add(new ChecklistTemplateItem
                    {
                        Id = Guid.NewGuid(), OrganizationId = template.OrganizationId,
                        ChecklistTemplateRoomId = room.Id, Description = itemInput.Description.Trim(), Position = itemInput.Position
                    });
                template.Rooms.Add(room);
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { template.Id, template.Name, template.IsActive, template.RowVersion });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { error = "template was modified by another request", code = "concurrency_conflict" });
        }
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
        if (!InspectionScheduleRules.IsValid(request.ScheduledAtUtc, DateTimeOffset.UtcNow))
            return Results.BadRequest(new { error = "ScheduledAtUtc must be in the future or null." });

        if (tenantContext.OrganizationId is not { } organizationId)
        {
            return Results.BadRequest(new { error = "Organization context is missing." });
        }

        var outcome = await idempotencyGuard.ExecuteAsync(
            organizationId,
            idempotencyKey,
            async () => await useCase.ExecuteAsync(request.UnitId, request.ChecklistTemplateId, inspectionType, cancellationToken, request.ScheduledAtUtc),
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

public sealed record UpdateChecklistTemplateRequest(
    string Name,
    bool IsActive,
    uint RowVersion,
    List<CreateChecklistTemplateRoomRequest>? Rooms);

public sealed record CreateInspectionRequest(
    Guid UnitId,
    Guid ChecklistTemplateId,
    string Type,
    DateTimeOffset? ScheduledAtUtc = null);
