using Microsoft.EntityFrameworkCore;
using Vistora.Application.Persistence;
using Vistora.Application.UseCases;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Xunit;

namespace Vistora.Tests;

public sealed class CreateInspectionFromTemplateUseCaseTests
{
    [Fact]
    public async Task Creating_an_inspection_copies_the_template_rooms_and_items()
    {
        var organizationId = Guid.NewGuid();
        await using var db = CreateContext(organizationId);
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = organizationId, PropertyId = Guid.NewGuid(), Identifier = "101" };
        var template = new ChecklistTemplate
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, Name = "Entrada", IsActive = true
        };
        var room = new ChecklistTemplateRoom
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, ChecklistTemplateId = template.Id,
            Name = "Sala", Position = 0
        };
        room.Items.Add(new ChecklistTemplateItem
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, ChecklistTemplateRoomId = room.Id,
            Description = "Paredes", Position = 0
        });
        template.Rooms.Add(room);
        db.Units.Add(unit);
        db.ChecklistTemplates.Add(template);
        await db.SaveChangesAsync();

        var result = await new CreateInspectionFromTemplateUseCase(db, new TestTenantContext(organizationId))
            .ExecuteAsync(unit.Id, template.Id, InspectionType.MoveIn, scheduledAtUtc: DateTimeOffset.Now.AddDays(1).ToOffset(TimeSpan.FromHours(-3)));

        var created = Assert.IsType<CreateInspectionResult.Created>(result);
        var inspection = await db.Inspections.Include(x => x.Rooms).ThenInclude(x => x.Items)
            .SingleAsync(x => x.Id == created.InspectionId);
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
        Assert.Equal(template.Id, inspection.ChecklistTemplateId);
        var copiedRoom = Assert.Single(inspection.Rooms);
        Assert.Equal("Sala", copiedRoom.Name);
        Assert.Equal("Paredes", Assert.Single(copiedRoom.Items).Description);
        template.Rooms.Single().Name = "Sala alterada";
        await db.SaveChangesAsync();
        Assert.Equal("Sala", (await db.InspectionRooms.SingleAsync()).Name);
        Assert.True(inspection.ScheduledAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Creating_an_inspection_for_a_missing_unit_does_not_write_an_inspection()
    {
        var organizationId = Guid.NewGuid();
        await using var db = CreateContext(organizationId);
        var template = new ChecklistTemplate
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, Name = "Entrada", IsActive = true
        };
        db.ChecklistTemplates.Add(template);
        await db.SaveChangesAsync();

        var result = await new CreateInspectionFromTemplateUseCase(db, new TestTenantContext(organizationId))
            .ExecuteAsync(Guid.NewGuid(), template.Id, InspectionType.MoveIn);

        Assert.IsType<CreateInspectionResult.UnitNotFound>(result);
        Assert.Empty(await db.Inspections.ToListAsync());
    }

    [Fact]
    public async Task ExitInspectionLinksToTheLatestApprovedEntryForTheSameUnit()
    {
        var organizationId = Guid.NewGuid();
        await using var db = CreateContext(organizationId);
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = organizationId,
            PropertyId = Guid.NewGuid(), Identifier = "101" };
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), OrganizationId = organizationId,
            Name = "Saída", IsActive = true };
        var older = new Inspection { Id = Guid.NewGuid(), OrganizationId = organizationId,
            UnitId = unit.Id, Type = InspectionType.MoveIn, Status = InspectionStatus.Approved,
            CompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-30) };
        var latest = new Inspection { Id = Guid.NewGuid(), OrganizationId = organizationId,
            UnitId = unit.Id, Type = InspectionType.MoveIn, Status = InspectionStatus.Approved,
            CompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-1) };
        db.Units.Add(unit);
        db.ChecklistTemplates.Add(template);
        db.Inspections.AddRange(older, latest);
        await db.SaveChangesAsync();

        var created = await new CreateInspectionFromTemplateUseCase(db, new TestTenantContext(organizationId))
            .ExecuteAsync(unit.Id, template.Id, InspectionType.MoveOut);

        var result = Assert.IsType<CreateInspectionResult.Created>(created);
        var inspection = await db.Inspections.SingleAsync(x => x.Id == result.InspectionId);
        Assert.Equal(latest.Id, inspection.RelatedInspectionId);
    }

    private static VistoraDbContext CreateContext(Guid organizationId)
    {
        var options = new DbContextOptionsBuilder<VistoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VistoraDbContext(options, new TestTenantContext(organizationId));
    }

    private sealed class TestTenantContext(Guid organizationId) : ITenantContext
    {
        public Guid? OrganizationId => organizationId;
    }
}
