using Microsoft.EntityFrameworkCore;
using Vistora.Application.Persistence;
using Vistora.Application.UseCases;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Xunit;

namespace Vistora.Tests;

public sealed class CompleteInspectionUseCaseTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("Não verificado")]
    [InlineData("unknown")]
    public async Task Unverified_checklist_is_not_completed_or_queued(string? response)
    {
        var tenant = new TestTenant(Guid.NewGuid());
        await using var db = Context(tenant);
        var inspection = Seed(db, tenant.OrganizationId!.Value, response);
        await db.SaveChangesAsync();
        var result = await new CompleteInspectionUseCase(db, tenant).ExecuteAsync(inspection.Id, "key");
        Assert.IsType<CompleteInspectionResult.IncompleteChecklist>(result);
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
        Assert.Empty(await db.ReportJobs.ToListAsync());
    }

    [Theory]
    [InlineData("Conforme")]
    [InlineData("Atenção")]
    [InlineData("Não conforme")]
    public async Task Verified_checklist_is_completed_and_durably_queued(string response)
    {
        var tenant = new TestTenant(Guid.NewGuid());
        await using var db = Context(tenant);
        var inspection = Seed(db, tenant.OrganizationId!.Value, response);
        await db.SaveChangesAsync();
        var result = await new CompleteInspectionUseCase(db, tenant).ExecuteAsync(inspection.Id, "key");
        var created = Assert.IsType<CompleteInspectionResult.Created>(result);
        Assert.Equal(InspectionStatus.Completed, inspection.Status);
        Assert.Equal(created.ReportJobId, Assert.Single(await db.ReportJobs.ToListAsync()).Id);
        Assert.IsType<CompleteInspectionResult.InvalidState>(await new CompleteInspectionUseCase(db, tenant).ExecuteAsync(inspection.Id, "other-key"));
        Assert.Single(await db.ReportJobs.ToListAsync());
    }

    [Fact]
    public async Task Empty_checklist_cannot_be_completed()
    {
        var tenant = new TestTenant(Guid.NewGuid());
        await using var db = Context(tenant);
        var inspection = Seed(db, tenant.OrganizationId!.Value, "Conforme");
        inspection.Rooms.Clear();
        await db.SaveChangesAsync();
        Assert.IsType<CompleteInspectionResult.IncompleteChecklist>(await new CompleteInspectionUseCase(db, tenant).ExecuteAsync(inspection.Id, "key"));
    }

    private static Inspection Seed(VistoraDbContext db, Guid organizationId, string? response)
    {
        var inspection = new Inspection { Id = Guid.NewGuid(), OrganizationId = organizationId,
            UnitId = Guid.NewGuid(), Type = InspectionType.MoveIn, Status = InspectionStatus.Draft };
        var room = new InspectionRoom { Id = Guid.NewGuid(), OrganizationId = organizationId, Name = "Room", InspectionId = inspection.Id };
        room.Items.Add(new InspectionItem { Id = Guid.NewGuid(), OrganizationId = organizationId,
            InspectionRoomId = room.Id, Description = "Wall", Response = response });
        inspection.Rooms.Add(room);
        db.Inspections.Add(inspection);
        return inspection;
    }

    private static VistoraDbContext Context(TestTenant tenant) => new(new DbContextOptionsBuilder<VistoraDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
    private sealed record TestTenant(Guid? OrganizationId) : ITenantContext;
}
