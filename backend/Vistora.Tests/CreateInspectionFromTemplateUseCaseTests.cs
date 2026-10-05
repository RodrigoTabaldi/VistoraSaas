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

    [Fact]
    public async Task Address_creates_property_unit_and_inspection_together()
    {
        var organizationId = Guid.NewGuid();
        await using var db = CreateContext(organizationId);
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), OrganizationId = organizationId,
            Name = "Basic", IsActive = true };
        db.ChecklistTemplates.Add(template);
        await db.SaveChangesAsync();
        var result = await new CreateInspectionFromTemplateUseCase(db, new TestTenantContext(organizationId))
            .ExecuteAsync(Guid.Empty, template.Id, InspectionType.MoveIn,
                location: new QuickInspectionLocation("  Rua A, 10  "));
        var created = Assert.IsType<CreateInspectionResult.Created>(result);
        var property = Assert.Single(await db.Properties.ToListAsync());
        Assert.Equal("Rua A, 10", property.Address);
        Assert.Equal(property.Address, property.Name);
        Assert.Equal(organizationId, property.OrganizationId);
        var unit = Assert.Single(await db.Units.ToListAsync());
        Assert.Equal("Principal", unit.Identifier);
        Assert.Equal(property.Id, unit.PropertyId);
        Assert.Equal(unit.Id, (await db.Inspections.SingleAsync(x => x.Id == created.InspectionId)).UnitId);
    }

    [Fact]
    public async Task Missing_template_does_not_create_a_property_or_unit()
    {
        var organizationId = Guid.NewGuid();
        await using var db = CreateContext(organizationId);
        var result = await new CreateInspectionFromTemplateUseCase(db, new TestTenantContext(organizationId))
            .ExecuteAsync(Guid.Empty, Guid.NewGuid(), InspectionType.MoveIn,
                location: new QuickInspectionLocation("Rua A, 10"));
        Assert.IsType<CreateInspectionResult.TemplateNotFound>(result);
        Assert.Empty(await db.Properties.ToListAsync());
        Assert.Empty(await db.Units.ToListAsync());
        Assert.Empty(await db.Inspections.ToListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_address_is_rejected_without_writes(string address)
    {
        var organizationId = Guid.NewGuid();
        await using var db = CreateContext(organizationId);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new CreateInspectionFromTemplateUseCase(db, new TestTenantContext(organizationId))
                .ExecuteAsync(Guid.Empty, Guid.NewGuid(), InspectionType.MoveIn,
                    location: new QuickInspectionLocation(address)));
        Assert.Empty(await db.Properties.ToListAsync());
    }

    [Fact]
    public async Task Initial_checklist_does_not_require_an_existing_property_or_template()
    {
        var organizationId = Guid.NewGuid();
        await using var db = CreateContext(organizationId);
        var result = await new CreateInspectionFromTemplateUseCase(db, new TestTenantContext(organizationId))
            .ExecuteAsync(Guid.Empty, Guid.Empty, InspectionType.MoveIn, location: new QuickInspectionLocation("Street, 10"));
        var created = Assert.IsType<CreateInspectionResult.Created>(result);
        var inspection = await db.Inspections.Include(x => x.Rooms).ThenInclude(x => x.Items).SingleAsync(x => x.Id == created.InspectionId);
        Assert.Null(inspection.ChecklistTemplateId);
        Assert.Single(Assert.Single(inspection.Rooms).Items);
        Assert.Empty(await db.ChecklistTemplates.ToListAsync());
        Assert.Equal("inspection.created", Assert.Single(await db.OutboxMessages.ToListAsync()).Type);
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
