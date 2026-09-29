using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Vistora.Application.Messaging;
using Vistora.Application.Persistence;
using Vistora.Application.Storage;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Api;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapVistoraApi(this IEndpointRouteBuilder endpoints, bool requireAuthorization = false)
    {
        var api = endpoints.MapGroup("/api/v1");
        if (requireAuthorization) api.RequireAuthorization();
        api.MapGet("/me", (HttpContext httpContext, ITenantContext tenant) =>
            Results.Ok(new
            {
                UserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub"),
                Name = httpContext.User.FindFirstValue(ClaimTypes.Name),
                Email = httpContext.User.FindFirstValue(ClaimTypes.Email),
                Role = httpContext.User.FindFirstValue(ClaimTypes.Role),
                OrganizationId = tenant.OrganizationId
            }));

        var properties = api.MapGroup("/properties");
        properties.MapGet("", ListPropertiesAsync);
        properties.MapPost("", CreatePropertyAsync).RequireAuthorization(AccessPolicies.ManageOrganization);
        properties.MapGet("/{propertyId:guid}/units", ListUnitsAsync);
        properties.MapPost("/{propertyId:guid}/units", CreateUnitAsync).RequireAuthorization(AccessPolicies.ManageOrganization);

        var inspections = api.MapGroup("/inspections");
        inspections.MapGet("", ListInspectionsAsync);
        inspections.MapPost("", CreateInspectionAsync).RequireAuthorization(AccessPolicies.EditInspection);
        inspections.MapGet("/{inspectionId:guid}", GetInspectionAsync);
        inspections.MapGet("/{inspectionId:guid}/comparison", CompareInspectionAsync);
        inspections.MapPatch("/{inspectionId:guid}/status", UpdateInspectionStatusAsync).RequireAuthorization(AccessPolicies.ManageOrganization);
        inspections.MapPost("/{inspectionId:guid}/rooms", CreateRoomAsync).RequireAuthorization(AccessPolicies.EditInspection);
        inspections.MapPost("/{inspectionId:guid}/reports", UploadReportAsync).RequireAuthorization(AccessPolicies.ManageOrganization);
        inspections.MapGet("/{inspectionId:guid}/reports", ListReportsAsync);

        var rooms = api.MapGroup("/rooms");
        rooms.MapPost("/{roomId:guid}/items", CreateItemAsync).RequireAuthorization(AccessPolicies.EditInspection);

        var items = api.MapGroup("/items");
        items.MapPut("/{itemId:guid}", UpdateItemAsync).RequireAuthorization(AccessPolicies.EditInspection);
        items.MapPost("/{itemId:guid}/evidence", UploadEvidenceAsync).RequireAuthorization(AccessPolicies.EditInspection);

        api.MapGet("/evidence/{evidenceId:guid}/download", DownloadEvidenceAsync);
        api.MapGet("/evidence/{evidenceId:guid}/content", ReadEvidenceAsync);
        api.MapGet("/reports/{reportId:guid}/download", DownloadReportAsync);
        api.MapGet("/reports/{reportId:guid}/content", ReadReportAsync);
        return endpoints;
    }

    private static async Task<IResult> ListPropertiesAsync(VistoraDbContext db, CancellationToken cancellationToken)
    {
        var properties = await db.Properties.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new PropertyResponse(x.Id, x.Name, x.Address, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return Results.Ok(properties);
    }

    private static async Task<IResult> CreatePropertyAsync(
        PropertyRequest request,
        VistoraDbContext db,
        ITenantContext tenant,
        CancellationToken cancellationToken)
    {
        if (!HasTenant(tenant, out var organizationId)) return TenantRequired();
        if (!ValidText(request.Name, 200) || !ValidText(request.Address, 1000))
            return Validation("name and address are required and must respect their size limits");

        var property = new Property
        {
            Id = Guid.NewGuid(), Name = request.Name.Trim(), Address = request.Address.Trim(),
            OrganizationId = organizationId, CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.Properties.Add(property);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/properties/{property.Id}",
            new PropertyResponse(property.Id, property.Name, property.Address, property.CreatedAtUtc));
    }

    private static async Task<IResult> ListUnitsAsync(Guid propertyId, VistoraDbContext db, CancellationToken cancellationToken)
    {
        var units = await db.Units.AsNoTracking().Where(x => x.PropertyId == propertyId).OrderBy(x => x.Identifier)
            .Select(x => new UnitResponse(x.Id, x.PropertyId, x.Identifier)).ToListAsync(cancellationToken);
        return Results.Ok(units);
    }

    private static async Task<IResult> CreateUnitAsync(
        Guid propertyId, UnitRequest request, VistoraDbContext db, ITenantContext tenant, CancellationToken cancellationToken)
    {
        if (!HasTenant(tenant, out var organizationId)) return TenantRequired();
        if (!ValidText(request.Identifier, 100)) return Validation("identifier is required and must have at most 100 characters");
        if (!await db.Properties.AnyAsync(x => x.Id == propertyId, cancellationToken)) return Results.NotFound();

        var unit = new Unit { Id = Guid.NewGuid(), PropertyId = propertyId, Identifier = request.Identifier.Trim(), OrganizationId = organizationId };
        db.Units.Add(unit);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/properties/{propertyId}/units/{unit.Id}", new UnitResponse(unit.Id, unit.PropertyId, unit.Identifier));
    }

    private static async Task<IResult> ListInspectionsAsync(VistoraDbContext db, CancellationToken cancellationToken)
    {
        var inspections = await db.Inspections.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new InspectionResponse(x.Id, x.UnitId, x.Type, x.Status, x.CreatedAtUtc, x.CompletedAtUtc, x.RowVersion))
            .ToListAsync(cancellationToken);
        return Results.Ok(inspections);
    }

    private static async Task<IResult> CreateInspectionAsync(
        InspectionRequest request, VistoraDbContext db, ITenantContext tenant, IMessageBus bus, CancellationToken cancellationToken)
    {
        if (!HasTenant(tenant, out var organizationId)) return TenantRequired();
        if (!Enum.IsDefined(request.Type)) return Validation("type must be MoveIn or MoveOut");
        if (!await db.Units.AnyAsync(x => x.Id == request.UnitId, cancellationToken)) return Results.NotFound();
        if (request.ChecklistTemplateId.HasValue && !await db.ChecklistTemplates.AnyAsync(x => x.Id == request.ChecklistTemplateId, cancellationToken)) return Results.NotFound();

        var relatedInspectionId = request.Type == InspectionType.MoveOut
            ? await db.Inspections
                .Where(x => x.UnitId == request.UnitId && x.Type == InspectionType.MoveIn && x.Status == InspectionStatus.Approved)
                .OrderByDescending(x => x.CompletedAtUtc)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var inspection = new Inspection
        {
            Id = Guid.NewGuid(), UnitId = request.UnitId, ChecklistTemplateId = request.ChecklistTemplateId,
            RelatedInspectionId = relatedInspectionId,
            Type = request.Type, Status = InspectionStatus.Draft, OrganizationId = organizationId, CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.Inspections.Add(inspection);
        await db.SaveChangesAsync(cancellationToken);
        await PublishAsync(bus, "inspection.created", inspection.Id, organizationId, cancellationToken);
        return Results.Created($"/api/v1/inspections/{inspection.Id}",
            new InspectionResponse(inspection.Id, inspection.UnitId, inspection.Type, inspection.Status, inspection.CreatedAtUtc, null, inspection.RowVersion));
    }

    private static async Task<IResult> GetInspectionAsync(Guid inspectionId, VistoraDbContext db, CancellationToken cancellationToken)
    {
        var inspection = await db.Inspections.AsNoTracking().Include(x => x.Rooms).ThenInclude(x => x.Items).ThenInclude(x => x.Evidence)
            .SingleOrDefaultAsync(x => x.Id == inspectionId, cancellationToken);
        if (inspection is null) return Results.NotFound();

        return Results.Ok(new
        {
            inspection.Id, inspection.UnitId, inspection.Type, inspection.Status, inspection.CreatedAtUtc, inspection.CompletedAtUtc,
            inspection.RelatedInspectionId,
            inspection.RowVersion,
            Rooms = inspection.Rooms.OrderBy(x => x.Position).Select(room => new
            {
                room.Id, room.Name, room.Position, room.RowVersion,
                Items = room.Items.OrderBy(x => x.Position).Select(item => new
                {
                    item.Id, item.Description, item.Response, item.Notes, item.Position, item.RowVersion,
                    Evidence = item.Evidence.Select(e => new { e.Id, e.FileName, e.ContentType, e.SizeBytes, e.CreatedAtUtc })
                })
            })
        });
    }

    private static async Task<IResult> UpdateInspectionStatusAsync(
        Guid inspectionId,
        StatusRequest request,
        VistoraDbContext db,
        ITenantContext tenant,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        if (!HasTenant(tenant, out var organizationId)) return TenantRequired();
        if (request.Status != InspectionStatus.Approved || request.RowVersion == 0)
            return Validation("only approval with a valid rowVersion is supported; use /complete to finish an inspection");

        var inspection = await db.Inspections.SingleOrDefaultAsync(x => x.Id == inspectionId, cancellationToken);
        if (inspection is null) return Results.NotFound();
        var report = await db.Reports.Where(x => x.InspectionId == inspectionId)
            .OrderByDescending(x => x.Version).FirstOrDefaultAsync(cancellationToken);
        if (!InspectionWorkflow.CanApprove(inspection.Status, report is not null))
            return Results.Conflict(new { error = "inspection must be completed and have a report before approval" });

        db.Entry(inspection).Property(x => x.RowVersion).OriginalValue = request.RowVersion;
        inspection.Status = InspectionStatus.Approved;
        report!.ApprovedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            EventType = "ReportApproved", EntityType = "Report", EntityId = report.Id.ToString(),
            OccurredAtUtc = report.ApprovedAtUtc.Value
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await PublishAsync(bus, $"inspection.{request.Status.ToString().ToLowerInvariant()}", inspection.Id, organizationId, cancellationToken);
            return Results.Ok(new { inspection.Id, inspection.Status, inspection.CompletedAtUtc, inspection.RowVersion });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { error = "inspection was modified by another request", code = "concurrency_conflict" });
        }
    }

    private static async Task<IResult> CreateRoomAsync(Guid inspectionId, RoomRequest request, VistoraDbContext db, ITenantContext tenant, CancellationToken cancellationToken)
    {
        if (!HasTenant(tenant, out var organizationId)) return TenantRequired();
        if (!ValidText(request.Name, 200) || request.Position < 0) return Validation("name and a non-negative position are required");
        var inspection = await db.Inspections.SingleOrDefaultAsync(x => x.Id == inspectionId, cancellationToken);
        if (inspection is null) return Results.NotFound();
        if (!InspectionWorkflow.CanEdit(inspection.Status)) return Results.Conflict(new { error = "completed inspections cannot be edited" });
        var room = new InspectionRoom { Id = Guid.NewGuid(), InspectionId = inspectionId, Name = request.Name.Trim(), Position = request.Position, OrganizationId = organizationId };
        db.InspectionRooms.Add(room);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/inspections/{inspectionId}/rooms/{room.Id}", new { room.Id, room.Name, room.Position, room.RowVersion });
    }

    private static async Task<IResult> CreateItemAsync(Guid roomId, ItemRequest request, VistoraDbContext db, ITenantContext tenant, CancellationToken cancellationToken)
    {
        if (!HasTenant(tenant, out var organizationId)) return TenantRequired();
        if (!ValidText(request.Description, 1000) || request.Position < 0) return Validation("description and a non-negative position are required");
        var room = await db.InspectionRooms.Include(x => x.Inspection).SingleOrDefaultAsync(x => x.Id == roomId, cancellationToken);
        if (room is null) return Results.NotFound();
        if (!InspectionWorkflow.CanEdit(room.Inspection!.Status)) return Results.Conflict(new { error = "completed inspections cannot be edited" });
        var item = new InspectionItem { Id = Guid.NewGuid(), InspectionRoomId = roomId, Description = request.Description.Trim(), Position = request.Position, OrganizationId = organizationId };
        db.InspectionItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/rooms/{roomId}/items/{item.Id}", new { item.Id, item.Description, item.Position, item.RowVersion });
    }

    private static async Task<IResult> UpdateItemAsync(Guid itemId, UpdateItemRequest request, VistoraDbContext db, CancellationToken cancellationToken)
    {
        if (request.Response?.Length > 2000 || request.Notes?.Length > 4000 || request.RowVersion == 0) return Validation("response, notes or rowVersion are invalid");
        var item = await db.InspectionItems.Include(x => x.Room).ThenInclude(x => x!.Inspection)
            .SingleOrDefaultAsync(x => x.Id == itemId, cancellationToken);
        if (item is null) return Results.NotFound();
        if (!InspectionWorkflow.CanEdit(item.Room!.Inspection!.Status)) return Results.Conflict(new { error = "completed inspections cannot be edited" });
        db.Entry(item).Property(x => x.RowVersion).OriginalValue = request.RowVersion;
        item.Response = request.Response;
        item.Notes = request.Notes;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { item.Id, item.Response, item.Notes, item.RowVersion });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { error = "item was modified by another request", code = "concurrency_conflict" });
        }
    }

    private static async Task<IResult> UploadEvidenceAsync(Guid itemId, HttpRequest request, VistoraDbContext db, ITenantContext tenant, IPrivateObjectStorage storage, CancellationToken cancellationToken)
    {
        if (!HasTenant(tenant, out var organizationId)) return TenantRequired();
        var file = request.Form.Files.GetFile("file");
        if (file is null || file.Length == 0 || file.Length > 25 * 1024 * 1024) return Validation("file is required and must be at most 25 MiB");
        if (file.ContentType is not ("image/jpeg" or "image/png" or "image/webp")) return Validation("only JPEG, PNG and WebP images are allowed");
        await using (var signatureStream = file.OpenReadStream())
        {
            if (!await UploadSignatureValidator.MatchesAsync(signatureStream, file.ContentType, cancellationToken))
                return Validation("file content does not match its image type");
        }
        var item = await db.InspectionItems.Include(x => x.Room).ThenInclude(x => x!.Inspection)
            .SingleOrDefaultAsync(x => x.Id == itemId, cancellationToken);
        if (item is null) return Results.NotFound();
        if (!InspectionWorkflow.CanEdit(item.Room!.Inspection!.Status)) return Results.Conflict(new { error = "completed inspections cannot be edited" });

        var safeName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (!ValidText(safeName, 512)) return Validation("file name is required and must have at most 512 characters");
        var objectKey = $"{organizationId:N}/inspections/items/{itemId:N}/{Guid.NewGuid():N}-{safeName}";
        string sha256;
        await using (var hashStream = file.OpenReadStream())
        {
            sha256 = await ComputeHashAsync(hashStream, cancellationToken);
        }
        await using var stream = file.OpenReadStream();
        await storage.UploadAsync(new StorageUpload(objectKey, stream, file.ContentType, file.Length), cancellationToken);
        var evidence = new Evidence { Id = Guid.NewGuid(), InspectionItemId = itemId, ObjectKey = objectKey, FileName = safeName, ContentType = file.ContentType, SizeBytes = file.Length, Sha256 = sha256, CreatedAtUtc = DateTimeOffset.UtcNow, OrganizationId = organizationId };
        db.Evidence.Add(evidence);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch { await storage.DeleteAsync(objectKey, CancellationToken.None); throw; }
        return Results.Created($"/api/v1/evidence/{evidence.Id}/download", new { evidence.Id, evidence.FileName, evidence.ContentType, evidence.SizeBytes });
    }

    private static async Task<IResult> DownloadEvidenceAsync(Guid evidenceId, VistoraDbContext db, IPrivateObjectStorage storage, CancellationToken cancellationToken)
    {
        var evidence = await db.Evidence.AsNoTracking().SingleOrDefaultAsync(x => x.Id == evidenceId, cancellationToken);
        return evidence is null ? Results.NotFound() : Results.Ok(new { Url = $"/api/v1/evidence/{evidence.Id}/content", evidence.FileName, evidence.ContentType });
    }

    private static async Task<IResult> CompareInspectionAsync(Guid inspectionId, VistoraDbContext db, CancellationToken cancellationToken)
    {
        var moveOut = await db.Inspections.AsNoTracking().Include(x => x.Rooms).ThenInclude(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == inspectionId, cancellationToken);
        if (moveOut is null) return Results.NotFound();
        if (moveOut.Type != InspectionType.MoveOut)
            return Validation("comparison is available only for exit inspections");

        var moveInId = moveOut.RelatedInspectionId ?? await db.Inspections
            .Where(x => x.UnitId == moveOut.UnitId && x.Type == InspectionType.MoveIn &&
                x.Status == InspectionStatus.Approved && x.CompletedAtUtc <= moveOut.CreatedAtUtc)
            .OrderByDescending(x => x.CompletedAtUtc)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (moveInId is null)
            return Results.NotFound(new { error = "no approved entry inspection exists for this unit" });

        var moveIn = await db.Inspections.AsNoTracking().Include(x => x.Rooms).ThenInclude(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == moveInId, cancellationToken);
        if (moveIn is null) return Results.NotFound();

        return Results.Ok(new
        {
            moveInInspectionId = moveIn.Id,
            moveOutInspectionId = moveOut.Id,
            differences = InspectionComparison.Compare(moveIn, moveOut)
        });
    }

    private static async Task<IResult> ReadEvidenceAsync(Guid evidenceId, VistoraDbContext db, IPrivateObjectStorage storage, CancellationToken cancellationToken)
    {
        var evidence = await db.Evidence.AsNoTracking().SingleOrDefaultAsync(x => x.Id == evidenceId, cancellationToken);
        if (evidence is null) return Results.NotFound();
        var content = await storage.DownloadAsync(evidence.ObjectKey, 25 * 1024 * 1024, cancellationToken);
        if (!SHA256.HashData(content).AsSpan().SequenceEqual(Convert.FromHexString(evidence.Sha256)))
            return Results.Problem("Evidence integrity check failed.", statusCode: StatusCodes.Status500InternalServerError);
        return Results.File(content, evidence.ContentType, evidence.FileName);
    }

    private static async Task<IResult> UploadReportAsync(Guid inspectionId, HttpRequest request, VistoraDbContext db, ITenantContext tenant, IPrivateObjectStorage storage, CancellationToken cancellationToken)
    {
        if (!HasTenant(tenant, out var organizationId)) return TenantRequired();
        var file = request.Form.Files.GetFile("file");
        if (file is null || file.Length == 0 || file.Length > 50 * 1024 * 1024) return Validation("file is required and must be at most 50 MiB");
        if (file.ContentType != "application/pdf") return Validation("only PDF reports are allowed");
        await using (var signatureStream = file.OpenReadStream())
        {
            if (!await UploadSignatureValidator.MatchesAsync(signatureStream, file.ContentType, cancellationToken))
                return Validation("file content does not match PDF format");
        }
        var inspection = await db.Inspections.SingleOrDefaultAsync(x => x.Id == inspectionId, cancellationToken);
        if (inspection is null) return Results.NotFound();
        if (inspection.Status != InspectionStatus.Completed) return Results.Conflict(new { error = "only completed, unapproved inspections accept reports" });
        var version = (await db.Reports.Where(x => x.InspectionId == inspectionId).MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0) + 1;
        var safeName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (!ValidText(safeName, 512)) return Validation("file name is required and must have at most 512 characters");
        var objectKey = $"{organizationId:N}/inspections/{inspectionId:N}/reports/{version}-{Guid.NewGuid():N}-{safeName}";
        string sha256;
        await using (var hashStream = file.OpenReadStream())
        {
            sha256 = await ComputeHashAsync(hashStream, cancellationToken);
        }
        await using var stream = file.OpenReadStream();
        await storage.UploadAsync(new StorageUpload(objectKey, stream, file.ContentType, file.Length), cancellationToken);
        var report = new Report { Id = Guid.NewGuid(), InspectionId = inspectionId, Version = version, ObjectKey = objectKey, Sha256 = sha256, CreatedAtUtc = DateTimeOffset.UtcNow, OrganizationId = organizationId };
        db.Reports.Add(report);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch { await storage.DeleteAsync(objectKey, CancellationToken.None); throw; }
        return Results.Created($"/api/v1/reports/{report.Id}/download", new { report.Id, report.Version, report.CreatedAtUtc });
    }

    private static async Task<IResult> DownloadReportAsync(Guid reportId, VistoraDbContext db, IPrivateObjectStorage storage, CancellationToken cancellationToken)
    {
        var report = await db.Reports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == reportId, cancellationToken);
        return report is null
            ? Results.NotFound()
            : Results.Ok(new { Url = $"/api/v1/reports/{report.Id}/content", report.Version, report.CreatedAtUtc });
    }

    private static async Task<IResult> ReadReportAsync(Guid reportId, VistoraDbContext db, IPrivateObjectStorage storage, CancellationToken cancellationToken)
    {
        var report = await db.Reports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == reportId, cancellationToken);
        if (report is null) return Results.NotFound();
        var content = await storage.DownloadAsync(report.ObjectKey, 100 * 1024 * 1024, cancellationToken);
        if (!SHA256.HashData(content).AsSpan().SequenceEqual(Convert.FromHexString(report.Sha256)))
            return Results.Problem("Report integrity check failed.", statusCode: StatusCodes.Status500InternalServerError);
        return Results.File(content, "application/pdf", $"vistoria-{report.InspectionId}-v{report.Version}.pdf");
    }

    private static async Task<IResult> ListReportsAsync(Guid inspectionId, VistoraDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Inspections.AnyAsync(x => x.Id == inspectionId, cancellationToken)) return Results.NotFound();
        var reports = await db.Reports.AsNoTracking()
            .Where(x => x.InspectionId == inspectionId)
            .OrderByDescending(x => x.Version)
            .Select(x => new { x.Id, x.InspectionId, x.Version, x.CreatedAtUtc, x.ApprovedAtUtc })
            .ToListAsync(cancellationToken);
        return Results.Ok(reports);
    }

    private static bool HasTenant(ITenantContext tenant, out Guid organizationId)
    { organizationId = tenant.OrganizationId ?? Guid.Empty; return organizationId != Guid.Empty; }

    private static IResult TenantRequired() => Results.Problem("A valid organization tenant is required.", statusCode: StatusCodes.Status401Unauthorized);
    private static IResult Validation(string message) => Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [message] });
    private static bool ValidText(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max;
    private static Task PublishAsync(IMessageBus bus, string type, Guid entityId, Guid organizationId, CancellationToken cancellationToken) =>
        bus.PublishAsync(new MessageEnvelope(Guid.NewGuid(), type, $"{{\"organizationId\":\"{organizationId}\",\"entityId\":\"{entityId}\"}}", DateTimeOffset.UtcNow), cancellationToken);
    private static async Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken)
    { if (stream.CanSeek) stream.Position = 0; return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant(); }

    public sealed record PropertyRequest(string Name, string Address);
    public sealed record UnitRequest(string Identifier);
    public sealed record InspectionRequest(Guid UnitId, InspectionType Type, Guid? ChecklistTemplateId);
    public sealed record RoomRequest(string Name, int Position);
    public sealed record ItemRequest(string Description, int Position);
    public sealed record UpdateItemRequest(string? Response, string? Notes, uint RowVersion);
    public sealed record StatusRequest(InspectionStatus Status, uint RowVersion);
    private sealed record PropertyResponse(Guid Id, string Name, string Address, DateTimeOffset CreatedAtUtc);
    private sealed record UnitResponse(Guid Id, Guid PropertyId, string Identifier);
    private sealed record InspectionResponse(Guid Id, Guid UnitId, InspectionType Type, InspectionStatus Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? CompletedAtUtc, uint RowVersion);
}
