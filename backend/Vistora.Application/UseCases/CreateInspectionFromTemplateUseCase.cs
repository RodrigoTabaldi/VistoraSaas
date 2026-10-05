using Vistora.Application.Idempotency;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vistora.Application.Persistence;
using Vistora.Domain;

namespace Vistora.Application.UseCases;

public sealed class CreateInspectionFromTemplateUseCase(IVistoraDbContext dbContext, ITenantContext tenantContext)
{
    public async Task<CreateInspectionResult> ExecuteAsync(
        Guid unitId,
        Guid checklistTemplateId,
        InspectionType type,
        CancellationToken cancellationToken = default,
        DateTimeOffset? scheduledAtUtc = null,
        QuickInspectionLocation? location = null)
    {
        if (!InspectionScheduleRules.IsValid(scheduledAtUtc, DateTimeOffset.UtcNow))
            throw new ArgumentException("Scheduled time must be in the future.", nameof(scheduledAtUtc));

        if (!Enum.IsDefined(type)) throw new ArgumentException("Invalid inspection type.", nameof(type));
        if ((unitId == Guid.Empty) == (location is null))
            throw new ArgumentException("Choose an existing unit or provide a location.");
        if (location is not null && !location.IsValid())
            throw new ArgumentException("Invalid inspection location.", nameof(location));

        var template = await dbContext.ChecklistTemplates
            .Include(t => t.Rooms)
            .ThenInclude(r => r.Items)
            .FirstOrDefaultAsync(t => t.Id == checklistTemplateId, cancellationToken);

        if (checklistTemplateId != Guid.Empty && (template is null || !template.IsActive))
        {
            return new CreateInspectionResult.TemplateNotFound();
        }

        if (template is null)
        {
            template = new ChecklistTemplate { Name = "Checklist inicial", OrganizationId = tenantContext.OrganizationId ?? Guid.Empty };
            var room = new ChecklistTemplateRoom { Name = "Ambiente geral", Position = 0 };
            room.Items.Add(new ChecklistTemplateItem { Description = "Estado geral de conservação", Position = 0 });
            template.Rooms.Add(room);
        }

        if (location is not null)
        {
            var organizationId = tenantContext.OrganizationId
                ?? throw new InvalidOperationException("Organization context is missing.");
            var property = new Property
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId,
                Name = string.IsNullOrWhiteSpace(location.Name) ? location.Address.Trim() : location.Name.Trim(),
                Address = location.Address.Trim(), CreatedAtUtc = DateTimeOffset.UtcNow
            };
            var unit = new Unit
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, PropertyId = property.Id,
                Property = property,
                Identifier = string.IsNullOrWhiteSpace(location.UnitIdentifier) ? "Principal" : location.UnitIdentifier.Trim()
            };
            unitId = unit.Id;
            dbContext.Units.Add(unit);
        }
        else if (!await dbContext.Units.AnyAsync(u => u.Id == unitId, cancellationToken))
        {
            return new CreateInspectionResult.UnitNotFound();
        }

        var relatedInspectionId = type == InspectionType.MoveOut
            ? await dbContext.Inspections
                .Where(x => x.UnitId == unitId && x.Type == InspectionType.MoveIn && x.Status == InspectionStatus.Approved)
                .OrderByDescending(x => x.CompletedAtUtc)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var now = DateTimeOffset.UtcNow;
        var inspection = new Inspection
        {
            Id = Guid.NewGuid(),
            OrganizationId = tenantContext.OrganizationId ?? throw new InvalidOperationException("Organization context is missing."),
            UnitId = unitId,
            ChecklistTemplateId = checklistTemplateId == Guid.Empty ? null : checklistTemplateId,
            RelatedInspectionId = relatedInspectionId,
            Type = type,
            Status = InspectionStatus.Draft,
            CreatedAtUtc = now,
            ScheduledAtUtc = InspectionScheduleRules.Normalize(scheduledAtUtc)
        };

        dbContext.Inspections.Add(inspection);

        foreach (var templateRoom in (template?.Rooms ?? Array.Empty<ChecklistTemplateRoom>()).OrderBy(r => r.Position))
        {
            var inspectionRoom = new InspectionRoom
            {
                Id = Guid.NewGuid(),
                OrganizationId = inspection.OrganizationId,
                InspectionId = inspection.Id,
                Name = templateRoom.Name,
                Position = templateRoom.Position
            };

            dbContext.InspectionRooms.Add(inspectionRoom);

            foreach (var templateItem in templateRoom.Items.OrderBy(i => i.Position))
            {
                var inspectionItem = new InspectionItem
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = inspection.OrganizationId,
                    InspectionRoomId = inspectionRoom.Id,
                    Description = templateItem.Description,
                    Position = templateItem.Position,
                    Response = null,
                    Notes = null
                };

                dbContext.InspectionItems.Add(inspectionItem);
            }
        }

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(), OrganizationId = inspection.OrganizationId,
            Type = "inspection.created",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { organizationId = inspection.OrganizationId, entityId = inspection.Id }),
            CreatedAtUtc = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateInspectionResult.Created(inspection.Id);
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$result")]
[JsonDerivedType(typeof(CreateInspectionResult.Created), "Created")]
[JsonDerivedType(typeof(CreateInspectionResult.TemplateNotFound), "TemplateNotFound")]
[JsonDerivedType(typeof(CreateInspectionResult.UnitNotFound), "UnitNotFound")]
public abstract record CreateInspectionResult : IIdempotencyResult
{
    [JsonIgnore]
    public bool IsSuccessful => this is Created;
    public sealed record Created(Guid InspectionId) : CreateInspectionResult;
    public sealed record TemplateNotFound : CreateInspectionResult;
    public sealed record UnitNotFound : CreateInspectionResult;
}

public sealed record QuickInspectionLocation(string Address, string? Name = null, string? UnitIdentifier = null)
{
    public bool IsValid() => !string.IsNullOrWhiteSpace(Address) && Address.Trim().Length <= 1000
        && (string.IsNullOrWhiteSpace(Name) ? Address.Trim().Length <= 200 : Name.Trim().Length <= 200)
        && (UnitIdentifier?.Trim().Length ?? 0) <= 100;
}
