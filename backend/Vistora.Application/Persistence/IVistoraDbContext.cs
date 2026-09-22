using Microsoft.EntityFrameworkCore;
using Vistora.Domain;

namespace Vistora.Application.Persistence;

public interface IVistoraDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<Unit> Units { get; }
    DbSet<ChecklistTemplate> ChecklistTemplates { get; }
    DbSet<ChecklistTemplateRoom> ChecklistTemplateRooms { get; }
    DbSet<ChecklistTemplateItem> ChecklistTemplateItems { get; }
    DbSet<Inspection> Inspections { get; }
    DbSet<InspectionRoom> InspectionRooms { get; }
    DbSet<InspectionItem> InspectionItems { get; }
    DbSet<ReportJob> ReportJobs { get; }
    DbSet<Report> Reports { get; }
    DbSet<AuditEvent> AuditEvents { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
