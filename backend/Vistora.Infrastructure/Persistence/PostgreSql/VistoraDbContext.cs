using Microsoft.EntityFrameworkCore;
using Vistora.Application.Persistence;
using Vistora.Domain;

namespace Vistora.Infrastructure.Persistence.PostgreSql;

public sealed class VistoraDbContext(
    DbContextOptions<VistoraDbContext> options,
    ITenantContext tenantContext) : DbContext(options), IVistoraDbContext
{
    private readonly ITenantContext _tenantContext = tenantContext;
    private Guid? CurrentOrganizationId => _tenantContext.OrganizationId;

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<InspectionRoom> InspectionRooms => Set<InspectionRoom>();
    public DbSet<InspectionItem> InspectionItems => Set<InspectionItem>();
    public DbSet<Evidence> Evidence => Set<Evidence>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ReportJob> ReportJobs => Set<ReportJob>();
    public DbSet<ChecklistTemplateRoom> ChecklistTemplateRooms => Set<ChecklistTemplateRoom>();
    public DbSet<ChecklistTemplateItem> ChecklistTemplateItems => Set<ChecklistTemplateItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("vistora");
        ConfigureOrganization(modelBuilder);
        ConfigureTenantEntity<User>(modelBuilder, "users");
        ConfigureTenantEntity<Property>(modelBuilder, "properties");
        ConfigureTenantEntity<Unit>(modelBuilder, "units");
        ConfigureTenantEntity<ChecklistTemplate>(modelBuilder, "checklist_templates");
        ConfigureTenantEntity<ChecklistTemplateRoom>(modelBuilder, "checklist_template_rooms");
        ConfigureTenantEntity<ChecklistTemplateItem>(modelBuilder, "checklist_template_items");
        ConfigureTenantEntity<Inspection>(modelBuilder, "inspections");
        ConfigureTenantEntity<InspectionRoom>(modelBuilder, "inspection_rooms");
        ConfigureTenantEntity<InspectionItem>(modelBuilder, "inspection_items");
        ConfigureTenantEntity<Evidence>(modelBuilder, "evidence");
        ConfigureTenantEntity<Report>(modelBuilder, "reports");
        ConfigureTenantEntity<AuditEvent>(modelBuilder, "audit_events");
        ConfigureTenantEntity<ReportJob>(modelBuilder, "report_jobs");
        ConfigureRowVersionedEntities(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.Email }).IsUnique();
        });
        modelBuilder.Entity<Property>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(1000).IsRequired();
        });
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.Property(x => x.Identifier).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.PropertyId, x.Identifier }).IsUnique();
            entity.HasOne(x => x.Property).WithMany(x => x.Units).HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ChecklistTemplate>(entity => entity.Property(x => x.Name).HasMaxLength(200).IsRequired());
        modelBuilder.Entity<ChecklistTemplateRoom>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.ChecklistTemplateId, x.Position }).IsUnique();
            entity.HasOne(x => x.Template).WithMany(x => x.Rooms).HasForeignKey(x => x.ChecklistTemplateId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ChecklistTemplateItem>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.ChecklistTemplateRoomId, x.Position }).IsUnique();
            entity.HasOne(x => x.Room).WithMany(x => x.Items).HasForeignKey(x => x.ChecklistTemplateRoomId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Inspection>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasOne(x => x.Unit).WithMany(x => x.Inspections).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ChecklistTemplate).WithMany().HasForeignKey(x => x.ChecklistTemplateId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.RelatedInspection).WithMany().HasForeignKey(x => x.RelatedInspectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.OrganizationId, x.UnitId, x.Type })
                .IsUnique()
                .HasFilter("\"Status\" <> 'Approved'");
        });
        modelBuilder.Entity<InspectionRoom>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.InspectionId, x.Position }).IsUnique();
            entity.HasOne(x => x.Inspection).WithMany(x => x.Rooms).HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<InspectionItem>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Response).HasMaxLength(2000);
            entity.Property(x => x.Notes).HasMaxLength(4000);
            entity.HasIndex(x => new { x.OrganizationId, x.InspectionRoomId, x.Position }).IsUnique();
            entity.HasOne(x => x.Room).WithMany(x => x.Items).HasForeignKey(x => x.InspectionRoomId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Evidence>(entity =>
        {
            entity.Property(x => x.ObjectKey).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(512).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.ObjectKey }).IsUnique();
            entity.HasOne(x => x.InspectionItem).WithMany(x => x.Evidence).HasForeignKey(x => x.InspectionItemId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Report>(entity =>
        {
            entity.Property(x => x.ObjectKey).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.InspectionId, x.Version }).IsUnique();
            entity.HasOne(x => x.Inspection).WithMany(x => x.Reports).HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.HasIndex(x => new { x.OrganizationId, x.OccurredAtUtc });
        });
        modelBuilder.Entity<ReportJob>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();
            entity.HasOne(x => x.Inspection).WithMany().HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResultReport).WithMany().HasForeignKey(x => x.ResultReportId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.OrganizationId, x.InspectionId })
                .IsUnique()
                .HasFilter("\"Status\" IN ('Pending', 'Processing')");
            entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique();
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareTenantEntities();
        if (!Database.IsRelational() || Database.CurrentTransaction is not null)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        using var transaction = Database.BeginTransaction();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        transaction.Commit();
        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareTenantEntities();
        if (!Database.IsRelational() || Database.CurrentTransaction is not null)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
        });
    }

    private void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder, string tableName)
        where TEntity : class, IOrganizationScoped
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey("Id");
            entity.Property(x => x.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.HasQueryFilter(x => CurrentOrganizationId.HasValue && x.OrganizationId == CurrentOrganizationId.Value);
            entity.HasIndex(x => x.OrganizationId);
        });
    }

    private static void ConfigureRowVersionedEntities(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => typeof(IRowVersioned).IsAssignableFrom(entityType.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property<uint>(nameof(IRowVersioned.RowVersion))
                .HasColumnName("xmin")
                .IsRowVersion()
                .ValueGeneratedOnAddOrUpdate();
        }
    }

    private void PrepareTenantEntities()
    {
        var organizationId = _tenantContext.OrganizationId;
        foreach (var entry in ChangeTracker.Entries<IOrganizationScoped>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (!organizationId.HasValue)
            {
                throw new InvalidOperationException("A tenant context is required to create tenant-scoped data.");
            }

            if (entry.State == EntityState.Added && entry.Entity.OrganizationId == Guid.Empty)
            {
                entry.Entity.OrganizationId = organizationId.Value;
            }
            else if (entry.Entity.OrganizationId != organizationId.Value)
            {
                throw new InvalidOperationException("Tenant-scoped data cannot be changed for another organization.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditEvent>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Audit events are append-only.");
            }
        }
    }
}
