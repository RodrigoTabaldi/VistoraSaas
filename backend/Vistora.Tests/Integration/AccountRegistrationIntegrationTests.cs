using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vistora.Api;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Xunit;

namespace Vistora.Tests.Integration;

[Collection(PostgreSqlCollection.Name)]
public sealed class AccountRegistrationIntegrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Registration_persists_an_admin_membership_and_only_a_password_hash()
    {
        const string password = "FirstAccountPassword!2026";
        var tenantContext = new TenantContext();
        await using var db = fixture.CreateContext(tenantContext);
        var passwordHasher = new PasswordHasher<Account>();
        var service = new AccountRegistrationService(db, tenantContext, passwordHasher);

        var account = await service.CreateAsync(
            "Ana Silva",
            "Imobiliária Horizonte",
            $"{Guid.NewGuid():N}@example.com",
            password,
            CancellationToken.None);

        Assert.NotNull(account);
        Assert.NotEqual(password, account.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password));

        var organization = await db.Organizations.SingleAsync(x => x.Id == account.OrganizationId);
        var membership = await db.Users.SingleAsync(x => x.Id == account.Id);
        Assert.Equal("Imobiliária Horizonte", organization.Name);
        Assert.Equal(account.OrganizationId, membership.OrganizationId);
        Assert.Equal("Admin", membership.Role);
    }

    [Fact]
    public async Task Registration_rejects_an_email_that_only_differs_by_case_across_tenants()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var firstTenant = new TenantContext();
        await using (var firstDb = fixture.CreateContext(firstTenant))
        {
            var firstService = new AccountRegistrationService(firstDb, firstTenant, new PasswordHasher<Account>());
            Assert.NotNull(await firstService.CreateAsync(
                "Ana Silva", "Horizonte", email, "FirstAccountPassword!2026", CancellationToken.None));
        }

        var secondTenant = new TenantContext();
        await using var secondDb = fixture.CreateContext(secondTenant);
        var secondService = new AccountRegistrationService(secondDb, secondTenant, new PasswordHasher<Account>());
        var duplicate = await secondService.CreateAsync(
            "Outra Pessoa", "Outra empresa", email.ToUpperInvariant(), "AnotherPassword!2026", CancellationToken.None);

        Assert.Null(duplicate);
        Assert.Null(secondTenant.OrganizationId);
    }
}
