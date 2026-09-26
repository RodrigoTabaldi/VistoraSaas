using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Api;

public sealed class AccountRegistrationService(
    VistoraDbContext db,
    TenantContext tenantContext,
    IPasswordHasher<Account> passwordHasher)
{
    public async Task<Account?> CreateAsync(
        string name,
        string organizationName,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        if (await db.Accounts.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = organizationName.Trim(),
            CreatedAtUtc = now
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Email = email.Trim(),
            Role = "Admin",
            CreatedAtUtc = now,
            Organization = organization
        };
        var account = new Account
        {
            Id = user.Id,
            OrganizationId = organization.Id,
            Name = name.Trim(),
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = string.Empty,
            Role = user.Role,
            CreatedAtUtc = now,
            User = user
        };
        account.PasswordHash = passwordHasher.HashPassword(account, password);

        // The initial user's tenant is created in this request; set it before the transaction so
        // PostgreSQL's tenant interceptor can apply the matching RLS context to the membership row.
        tenantContext.SetOrganization(organization.Id);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Organizations.Add(organization);
        db.Users.Add(user);
        db.Accounts.Add(account);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return account;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }
    }
}
