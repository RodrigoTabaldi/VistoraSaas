using Microsoft.EntityFrameworkCore;
using Vistora.Domain;

namespace Vistora.Application.Persistence;

public interface IVistoraDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<Inspection> Inspections { get; }
    DbSet<ReportJob> ReportJobs { get; }
    DbSet<Report> Reports { get; }
    DbSet<AuditEvent> AuditEvents { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
