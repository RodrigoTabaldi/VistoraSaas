using Vistora.Application.Persistence;

namespace Vistora.Infrastructure.Persistence.PostgreSql;

/// <summary>
/// Scoped tenant state. Authentication middleware must set it from a trusted claim before accessing data.
/// A missing tenant intentionally results in no readable rows and rejected writes.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid? OrganizationId { get; private set; }

    public void SetOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("The organization identifier cannot be empty.", nameof(organizationId));
        }

        OrganizationId = organizationId;
    }
}
