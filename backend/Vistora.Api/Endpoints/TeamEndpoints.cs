using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vistora.Application.Persistence;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Api.Endpoints;

public static class TeamEndpoints
{
    private static readonly HashSet<string> AllowedRoles = ["Admin", "Vistoriador", "Leitor"];

    public static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/team", ListTeamAsync)
            .RequireAuthorization(AccessPolicies.ManageOrganization);
        endpoints.MapPost("/api/v1/team/invitations", CreateInvitationAsync)
            .RequireAuthorization(AccessPolicies.ManageOrganization);
        endpoints.MapDelete("/api/v1/team/invitations/{invitationId:guid}", RevokeInvitationAsync)
            .RequireAuthorization(AccessPolicies.ManageOrganization);
        endpoints.MapPost("/api/v1/auth/invitations/accept", AcceptInvitationAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AccountEndpoints.RateLimitPolicy);
        return endpoints;
    }

    private static async Task<IResult> ListTeamAsync(VistoraDbContext db, ITenantContext tenant, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId is not { } organizationId) return Results.Unauthorized();
        var users = await db.Users.AsNoTracking().OrderBy(x => x.Email)
            .Select(x => new { x.Id, x.Email, x.Role }).ToListAsync(cancellationToken);
        var ids = users.Select(x => x.Id).ToList();
        var names = await db.Accounts.AsNoTracking().Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var invitations = await db.AccountInvitations.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.AcceptedAtUtc == null && x.RevokedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.Email, x.Role, x.CreatedAtUtc, x.ExpiresAtUtc })
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            Members = users.Select(user => new
            {
                user.Id, user.Email, user.Role,
                Name = names.GetValueOrDefault(user.Id, user.Email)
            }),
            Invitations = invitations.Select(invitation => new
            {
                invitation.Id, invitation.Email, invitation.Role, invitation.CreatedAtUtc, invitation.ExpiresAtUtc,
                Status = invitation.ExpiresAtUtc <= now ? "Expired" : "Pending"
            })
        });
    }

    private static async Task<IResult> CreateInvitationAsync(
        CreateInvitationRequest? request, VistoraDbContext db, ITenantContext tenant,
        HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 320 ||
            !new EmailAddressAttribute().IsValid(request.Email.Trim()))
            return Results.BadRequest(new { error = "Informe um e-mail válido." });
        if (!AllowedRoles.Contains(request.Role))
            return Results.BadRequest(new { error = "Perfil deve ser Admin, Vistoriador ou Leitor." });
        if (tenant.OrganizationId is not { } organizationId ||
            !Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Results.Unauthorized();

        var email = request.Email.Trim();
        var normalizedEmail = NormalizeEmail(email);
        if (await db.Accounts.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken))
            return Results.Conflict(new { code = "email_in_use", error = "Este e-mail já possui uma conta." });

        var now = DateTimeOffset.UtcNow;
        var expiredInvitations = await db.AccountInvitations.Where(x =>
                x.NormalizedEmail == normalizedEmail && x.AcceptedAtUtc == null && x.RevokedAtUtc == null && x.ExpiresAtUtc <= now)
            .ToListAsync(cancellationToken);
        foreach (var expired in expiredInvitations) expired.RevokedAtUtc = now;
        if (expiredInvitations.Count > 0)
        {
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { code = "invitation_changed", error = "O convite expirado foi alterado por outra solicitação." });
            }
        }

        if (await db.AccountInvitations.AnyAsync(x =>
                x.NormalizedEmail == normalizedEmail && x.AcceptedAtUtc == null && x.RevokedAtUtc == null,
                cancellationToken))
            return Results.Conflict(new { code = "invitation_exists", error = "Já existe um convite pendente para este e-mail." });

        var token = InvitationToken.Create();
        var invitation = new AccountInvitation
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, InvitedByUserId = actorId,
            Email = email, NormalizedEmail = normalizedEmail, Role = request.Role,
            TokenHash = InvitationToken.Hash(token), CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(7)
        };
        db.AccountInvitations.Add(invitation);
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, ActorUserId = actorId,
            EventType = "TeamInvitationCreated", EntityType = "AccountInvitation",
            EntityId = invitation.Id.ToString(), OccurredAtUtc = now
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Results.Conflict(new { code = "invitation_exists", error = "Já existe um convite ou conta para este e-mail." });
        }

        return Results.Created($"/api/v1/team/invitations/{invitation.Id}", new
        {
            invitation.Id, invitation.Email, invitation.Role, invitation.ExpiresAtUtc, Token = token
        });
    }

    private static async Task<IResult> RevokeInvitationAsync(
        Guid invitationId, VistoraDbContext db, ITenantContext tenant, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId is not { } organizationId) return Results.Unauthorized();
        var invitation = await db.AccountInvitations.SingleOrDefaultAsync(x =>
            x.Id == invitationId && x.OrganizationId == organizationId && x.AcceptedAtUtc == null && x.RevokedAtUtc == null,
            cancellationToken);
        if (invitation is null) return Results.NotFound();

        var now = DateTimeOffset.UtcNow;
        invitation.RevokedAtUtc = now;
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            ActorUserId = Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null,
            EventType = "TeamInvitationRevoked", EntityType = "AccountInvitation",
            EntityId = invitation.Id.ToString(), OccurredAtUtc = now
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { code = "invitation_changed", error = "O convite foi aceito ou alterado por outra solicitação." });
        }
    }

    private static async Task<IResult> AcceptInvitationAsync(
        AcceptInvitationRequest? request, VistoraDbContext db, TenantContext tenant,
        IPasswordHasher<Account> passwordHasher, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (request is null || !InvitationToken.IsValid(request.Token) ||
            string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 150 ||
            string.IsNullOrEmpty(request.Password) || request.Password.Length is < 12 or > 128)
            return Results.BadRequest(new { error = "Convite, nome ou senha inválidos." });

        var tokenHash = InvitationToken.Hash(request.Token);
        var invitation = await db.AccountInvitations.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (invitation is null || invitation.AcceptedAtUtc.HasValue || invitation.RevokedAtUtc.HasValue || invitation.ExpiresAtUtc <= now)
            return InvalidInvitation();
        if (await db.Accounts.AnyAsync(x => x.NormalizedEmail == invitation.NormalizedEmail, cancellationToken))
            return InvalidInvitation();

        // Set the invitation's organization before writing membership rows protected by tenant RLS.
        tenant.SetOrganization(invitation.OrganizationId);
        var user = new User
        {
            Id = Guid.NewGuid(), OrganizationId = invitation.OrganizationId,
            Email = invitation.Email, Role = invitation.Role, CreatedAtUtc = now
        };
        var account = new Account
        {
            Id = user.Id, OrganizationId = invitation.OrganizationId,
            Name = request.Name.Trim(), Email = invitation.Email, NormalizedEmail = invitation.NormalizedEmail,
            PasswordHash = string.Empty, Role = invitation.Role, CreatedAtUtc = now, User = user
        };
        account.PasswordHash = passwordHasher.HashPassword(account, request.Password);
        invitation.AcceptedAtUtc = now;
        db.Users.Add(user);
        db.Accounts.Add(account);
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = invitation.OrganizationId, ActorUserId = user.Id,
            EventType = "TeamInvitationAccepted", EntityType = "AccountInvitation",
            EntityId = invitation.Id.ToString(), OccurredAtUtc = now
        });

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InvalidInvitation();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return InvalidInvitation();
        }

        await AccountEndpoints.SignInAsync(httpContext, account, rememberMe: true);
        return Results.Created("/api/v1/me", new AccountResponse(account.Id, account.Name, account.Email, account.OrganizationId, account.Role));
    }

    private static IResult InvalidInvitation() => Results.BadRequest(new { code = "invalid_invitation", error = "Este convite é inválido, expirou ou já foi utilizado." });
    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}

public static class InvitationToken
{
    // Return a random link token once and persist only its irreversible SHA-256 digest.
    public static string Create() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public static bool IsValid(string? token) => token is { Length: 43 } &&
        token.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}

public sealed record CreateInvitationRequest(string Email, string Role);
public sealed record AcceptInvitationRequest(string Token, string Name, string Password);
