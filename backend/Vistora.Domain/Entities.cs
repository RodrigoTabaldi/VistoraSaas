namespace Vistora.Domain;

public interface IOrganizationScoped
{
    Guid OrganizationId { get; set; }
}

public interface IRowVersioned
{
    uint RowVersion { get; set; }
}

public sealed class Organization
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public ICollection<User> Users { get; } = new List<User>();
}

public sealed class User : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Organization? Organization { get; set; }
}

public sealed class Property : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Organization? Organization { get; set; }
    public ICollection<Unit> Units { get; } = new List<Unit>();
}

public sealed class Unit : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public required string Identifier { get; set; }
    public Property? Property { get; set; }
    public ICollection<Inspection> Inspections { get; } = new List<Inspection>();
}

public sealed class ChecklistTemplate : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class Inspection : IOrganizationScoped, IRowVersioned
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? ChecklistTemplateId { get; set; }
    public InspectionType Type { get; set; }
    public Guid? RelatedInspectionId { get; set; }
    public InspectionStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public uint RowVersion { get; set; }
    public Unit? Unit { get; set; }
    public ChecklistTemplate? ChecklistTemplate { get; set; }
    public Inspection? RelatedInspection { get; set; }
    public ICollection<InspectionRoom> Rooms { get; } = new List<InspectionRoom>();
    public ICollection<Report> Reports { get; } = new List<Report>();
}

public enum InspectionType
{
    MoveIn = 1,
    MoveOut = 2
}

public enum InspectionStatus
{
    Draft = 1,
    Completed = 2,
    Approved = 3
}

public sealed class InspectionRoom : IOrganizationScoped, IRowVersioned
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid InspectionId { get; set; }
    public required string Name { get; set; }
    public int Position { get; set; }
    public uint RowVersion { get; set; }
    public Inspection? Inspection { get; set; }
    public ICollection<InspectionItem> Items { get; } = new List<InspectionItem>();
}

public sealed class InspectionItem : IOrganizationScoped, IRowVersioned
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid InspectionRoomId { get; set; }
    public required string Description { get; set; }
    public string? Response { get; set; }
    public string? Notes { get; set; }
    public int Position { get; set; }
    public uint RowVersion { get; set; }
    public InspectionRoom? Room { get; set; }
    public ICollection<Evidence> Evidence { get; } = new List<Evidence>();
}

public sealed class Evidence : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid InspectionItemId { get; set; }
    public required string ObjectKey { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string Sha256 { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public InspectionItem? InspectionItem { get; set; }
}

public sealed class Report : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid InspectionId { get; set; }
    public int Version { get; set; }
    public required string ObjectKey { get; set; }
    public required string Sha256 { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Inspection? Inspection { get; set; }
}

public sealed class AuditEvent : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string EventType { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}

public sealed class ReportJob : IOrganizationScoped, IRowVersioned
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid InspectionId { get; set; }

    /// <summary>
    /// Client/API-supplied idempotency key for THIS job creation, distinct from the Redis-backed
    /// HTTP-level Idempotency-Key. Guarantees at most one non-terminal job per Inspection even if
    /// "conclude inspection" is called twice concurrently - enforced by the unique partial index
    /// below, not by this field's uniqueness alone.
    /// </summary>
    public required string IdempotencyKey { get; set; }

    public ReportJobStatus Status { get; set; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Set when the job completes successfully; points at the Report version it produced.</summary>
    public Guid? ResultReportId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public uint RowVersion { get; set; }

    public Inspection? Inspection { get; set; }
    public Report? ResultReport { get; set; }
}

public enum ReportJobStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}

