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
        CancellationToken cancellationToken = default)
    {
        var template = await dbContext.ChecklistTemplates
            .Include(t => t.Rooms)
            .ThenInclude(r => r.Items)
            .FirstOrDefaultAsync(t => t.Id == checklistTemplateId, cancellationToken);

        if (template is null || !template.IsActive)
        {
            return new CreateInspectionResult.TemplateNotFound();
        }

        var unitExists = await dbContext.Units
            .AnyAsync(u => u.Id == unitId, cancellationToken);

        if (!unitExists)
        {
            return new CreateInspectionResult.UnitNotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var inspection = new Inspection
        {
            Id = Guid.NewGuid(),
            OrganizationId = tenantContext.OrganizationId ?? template.OrganizationId,
            UnitId = unitId,
            ChecklistTemplateId = checklistTemplateId,
            Type = type,
            Status = InspectionStatus.Draft,
            CreatedAtUtc = now
        };

        dbContext.Inspections.Add(inspection);

        foreach (var templateRoom in template.Rooms.OrderBy(r => r.Position))
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

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateInspectionResult.Created(inspection.Id);
    }
}

public abstract record CreateInspectionResult
{
    public sealed record Created(Guid InspectionId) : CreateInspectionResult;
    public sealed record TemplateNotFound : CreateInspectionResult;
    public sealed record UnitNotFound : CreateInspectionResult;
}
