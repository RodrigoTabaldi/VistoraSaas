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
                OrganizationId = tenant.OrganizationId
            }));

        var properties = api.MapGroup("/properties");
        properties.MapGet("", ListPropertiesAsync);
        properties.MapPost("", CreatePropertyAsync);
        properties.MapGet("/{propertyId:guid}/units", ListUnitsAsync);
        properties.MapPost("/{propertyId:guid}/units", CreateUnitAsync);

        var inspections = api.MapGroup("/inspections");
        inspections.MapGet("", ListInspectionsAsync);
        inspections.MapPost("", CreateInspectionAsync);
        inspections.MapGet("/{inspectionId:guid}", GetInspectionAsync);
        inspections.MapPatch("/{inspectionId:guid}/status", UpdateInspectionStatusAsync);
        inspections.MapPost("/{inspectionId:guid}/rooms", CreateRoomAsync);
        inspections.MapPost("/{inspectionId:guid}/reports", UploadReportAsync);

        var rooms = api.MapGroup("/rooms");
        rooms.MapPost("/{roomId:guid}/items", CreateItemAsync);

        var items = api.MapGroup("/items");
        items.MapPut("/{itemId:guid}", UpdateItemAsync);
        items.MapPost("/{itemId:guid}/evidence", UploadEvidenceAsync);

        api.MapGet("/evidence/{evidenceId:guid}/download", DownloadEvidenceAsync);
        api.MapGet("/reports/{reportId:guid}/download", DownloadReportAsync);
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

        var inspection = new Inspection
        {
            Id = Guid.NewGuid(), UnitId = request.UnitId, ChecklistTemplateId = request.ChecklistTemplateId,
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
        if (!Enum.IsDefined(request.Status) || request.RowVersion == 0) return Validation("status or rowVersion is invalid");

        var inspection = await db.Inspections.SingleOrDefaultAsync(x => x.Id == inspectionId, cancellationToken);
        if (inspection is null) return Results.NotFound();
        if (!IsValidTransition(inspection.Status, request.Status)) return Validation("invalid inspection status transition");

        db.Entry(inspection).Property(x => x.RowVersion).OriginalValue = request.RowVersion;
        inspection.Status = request.Status;
        inspection.CompletedAtUtc = request.Status == InspectionStatus.Completed ? DateTimeOffset.UtcNow : inspection.CompletedAtUtc;
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
        if (!await db.Inspections.AnyAsync(x => x.Id == inspectionId, cancellationToken)) return Results.NotFound();
        var room = new InspectionRoom { Id = Guid.NewGuid(), InspectionId = inspectionId, Name = request.Name.Trim(), Position = request.Position, OrganizationId = organizationId };
        db.InspectionRooms.Add(room);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/inspections/{inspectionId}/rooms/{room.Id}", new { room.Id, room.Name, room.Position, room.RowVersion });
    }

    private static async Task<IResult> CreateItemAsync(Guid roomId, ItemRequest request, VistoraDbContext db, ITenantContext tenant, CancellationToken cancellationToken)
    {
        if (!HasTenant(tenant, out var organizationId)) return TenantRequired();
        if (!ValidText(request.Description, 1000) || request.Position < 0) return Validation("description and a non-negative position are required");
        if (!await db.InspectionRooms.AnyAsync(x => x.Id == roomId, cancellationToken)) return Results.NotFound();
        var item = new InspectionItem { Id = Guid.NewGuid(), InspectionRoomId = roomId, Description = request.Description.Trim(), Position = request.Position, OrganizationId = organizationId };
        db.InspectionItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/rooms/{roomId}/items/{item.Id}", new { item.Id, item.Description, item.Position, item.RowVersion });
    }

    private static async Task<IResult> UpdateItemAsync(Guid itemId, UpdateItemRequest request, VistoraDbContext db, CancellationToken cancellationToken)
    {
        if (request.Response?.Length > 2000 || request.Notes?.Length > 4000 || request.RowVersion == 0) return Validation("response, notes or rowVersion are invalid");
        var item = await db.InspectionItems.SingleOrDefaultAsync(x => x.Id == itemId, cancellationToken);
        if (item is null) return Results.NotFound();
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
        if (!await db.InspectionItems.AnyAsync(x => x.Id == itemId, cancellationToken)) return Results.NotFound();

        var safeName = Path.GetFileName(file.FileName);
        var objectKey = $"{organizationId:N}/inspections/items/{itemId:N}/{Guid.NewGuid():N}-{safeName}";
        await using var stream = file.OpenReadStream();
        await storage.UploadAsync(new StorageUpload(objectKey, stream, file.ContentType, file.Length), cancellationToken);
        var evidence = new Evidence { Id = Guid.NewGuid(), InspectionItemId = itemId, ObjectKey = objectKey, FileName = safeName, ContentType = file.ContentType, SizeBytes = file.Length, Sha256 = await ComputeHashAsync(stream, cancellationToken), CreatedAtUtc = DateTimeOffset.UtcNow, OrganizationId = organizationId };
        db.Evidence.Add(evidence);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch { await storage.DeleteAsync(objectKey, CancellationToken.None); throw; }
        return Results.Created($"/api/v1/evidence/{evidence.Id}/download", new { evidence.Id, evidence.FileName, evidence.ContentType, evidence.SizeBytes });
    }

    private static async Task<IResult> DownloadEvidenceAsync(Guid evidenceId, VistoraDbContext db, IPrivateObjectStorage storage, CancellationToken cancellationToken)
    {
        var evidence = await db.Evidence.AsNoTracking().SingleOrDefaultAsync(x => x.Id == evidenceId, cancellationToken);
        return evidence is null ? Results.NotFound() : Results.Ok(new { Url = storage.CreateDownloadUrl(evidence.ObjectKey, TimeSpan.FromMinutes(10)), evidence.FileName, evidence.ContentType });
    }

    private static async Task<IResult> UploadReportAsync(Guid inspectionId, HttpRequest request, VistoraDbContext db, ITenantContext tenant, IPrivateObjectStorage storage, CancellationToken cancellationToken)
    {
        if (!HasTenant(tenant, out var organizationId)) return TenantRequired();
        var file = request.Form.Files.GetFile("file");
        if (file is null || file.Length == 0 || file.Length > 50 * 1024 * 1024) return Validation("file is required and must be at most 50 MiB");
        if (!await db.Inspections.AnyAsync(x => x.Id == inspectionId, cancellationToken)) return Results.NotFound();
        var version = (await db.Reports.Where(x => x.InspectionId == inspectionId).MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0) + 1;
        var objectKey = $"{organizationId:N}/inspections/{inspectionId:N}/reports/{version}-{Guid.NewGuid():N}-{Path.GetFileName(file.FileName)}";
        await using var stream = file.OpenReadStream();
        await storage.UploadAsync(new StorageUpload(objectKey, stream, file.ContentType, file.Length), cancellationToken);
        var report = new Report { Id = Guid.NewGuid(), InspectionId = inspectionId, Version = version, ObjectKey = objectKey, Sha256 = await ComputeHashAsync(stream, cancellationToken), CreatedAtUtc = DateTimeOffset.UtcNow, OrganizationId = organizationId };
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
            : Results.Ok(new { Url = storage.CreateDownloadUrl(report.ObjectKey, TimeSpan.FromMinutes(10)), report.Version, report.CreatedAtUtc });
    }

    private static bool HasTenant(ITenantContext tenant, out Guid organizationId)
    { organizationId = tenant.OrganizationId ?? Guid.Empty; return organizationId != Guid.Empty; }

    private static IResult TenantRequired() => Results.Problem("A valid organization tenant is required.", statusCode: StatusCodes.Status401Unauthorized);
    private static IResult Validation(string message) => Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [message] });
    private static bool ValidText(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max;
    private static bool IsValidTransition(InspectionStatus current, InspectionStatus next) =>
        (current, next) is (InspectionStatus.Draft, InspectionStatus.Completed)
            or (InspectionStatus.Completed, InspectionStatus.Approved);
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
