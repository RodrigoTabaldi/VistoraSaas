namespace Vistora.Application.Persistence;

/// <summary>Provides the organization authorized for the current execution scope.</summary>
public interface ITenantContext
{
    Guid? OrganizationId { get; }
}
