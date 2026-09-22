using Vistora.Application.Persistence;
using Vistora.Domain;

namespace Vistora.Application.UseCases;

public sealed record CreateChecklistTemplateRoomInput(string Name, int Position, IReadOnlyList<CreateChecklistTemplateItemInput> Items);
public sealed record CreateChecklistTemplateItemInput(string Description, int Position);

public sealed class CreateChecklistTemplateUseCase(IVistoraDbContext dbContext, ITenantContext tenantContext)
{
    public async Task<Guid> ExecuteAsync(string name, IReadOnlyList<CreateChecklistTemplateRoomInput> rooms, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template name cannot be empty.", nameof(name));
        }

        var template = new ChecklistTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = tenantContext.OrganizationId ?? Guid.Empty,
            Name = name,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (rooms is not null)
        {
            foreach (var roomInput in rooms)
            {
                var room = new ChecklistTemplateRoom
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = template.OrganizationId,
                    ChecklistTemplateId = template.Id,
                    Name = roomInput.Name,
                    Position = roomInput.Position,
                    Template = template
                };

                if (roomInput.Items is not null)
                {
                    foreach (var itemInput in roomInput.Items)
                    {
                        var item = new ChecklistTemplateItem
                        {
                            Id = Guid.NewGuid(),
                            OrganizationId = template.OrganizationId,
                            ChecklistTemplateRoomId = room.Id,
                            Description = itemInput.Description,
                            Position = itemInput.Position,
                            Room = room
                        };
                        room.Items.Add(item);
                    }
                }

                template.Rooms.Add(room);
            }
        }

        dbContext.ChecklistTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        return template.Id;
    }
}
