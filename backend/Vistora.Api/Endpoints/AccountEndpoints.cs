using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;

namespace Vistora.Api.Endpoints;

public static class AccountEndpoints
{
    public const string CookieScheme = "VistoraCookie";
    public const string RateLimitPolicy = "account-auth";

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/v1/auth").WithTags("Authentication");
        auth.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicy);
        auth.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicy);
        auth.MapPost("/logout", (Delegate)LogoutAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterAccountRequest? request,
        AccountRegistrationService registration,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var errors = ValidateRegistration(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var account = await registration.CreateAsync(
            request!.Name!.Trim(),
            request.OrganizationName!.Trim(),
            request.Email!.Trim(),
            request.Password!,
            cancellationToken);

        if (account is null)
        {
            return EmailInUse();
        }

        await SignInAsync(httpContext, account, rememberMe: true);
        return Results.Created("/api/v1/me", ToAccountResponse(account));
    }

    private static async Task<IResult> LoginAsync(
        LoginAccountRequest? request,
        VistoraDbContext db,
        IPasswordHasher<Account> passwordHasher,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) ||
            request.Email.Length > 320 || string.IsNullOrEmpty(request.Password) ||
            request.Password.Length > 128)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["Informe um e-mail e uma senha válidos."]
            });
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var account = await db.Accounts.SingleOrDefaultAsync(
            x => x.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (account is null || passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
        {
            return Results.Json(new { code = "invalid_credentials", error = "E-mail ou senha inválidos." }, statusCode: StatusCodes.Status401Unauthorized);
        }

        await SignInAsync(httpContext, account, request.RememberMe);
        return Results.Ok(ToAccountResponse(account));
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieScheme);
        return Results.NoContent();
    }

    private static Dictionary<string, string[]> ValidateRegistration(RegisterAccountRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
        {
            errors["form"] = ["Os dados da conta são obrigatórios."];
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 150)
        {
            errors["name"] = ["Informe seu nome (até 150 caracteres)."];
        }

        if (string.IsNullOrWhiteSpace(request.OrganizationName) || request.OrganizationName.Trim().Length > 200)
        {
            errors["organizationName"] = ["Informe o nome da empresa (até 200 caracteres)."];
        }

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320 || !new EmailAddressAttribute().IsValid(email))
        {
            errors["email"] = ["Informe um e-mail válido."];
        }

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length is < 12 or > 128)
        {
            errors["password"] = ["A senha deve ter entre 12 e 128 caracteres."];
        }

        return errors;
    }

    private static async Task SignInAsync(HttpContext httpContext, Account account, bool rememberMe)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Name),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, account.Role),
            new Claim("organization_id", account.OrganizationId.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieScheme);
        var expiresAt = DateTimeOffset.UtcNow.Add(rememberMe ? TimeSpan.FromDays(14) : TimeSpan.FromHours(12));
        await httpContext.SignInAsync(
            CookieScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true,
                ExpiresUtc = expiresAt
            });
    }

    private static AccountResponse ToAccountResponse(Account account) =>
        new(account.Id, account.Name, account.Email, account.OrganizationId, account.Role);

    private static IResult EmailInUse() => Results.Conflict(new
    {
        code = "email_in_use",
        error = "Já existe uma conta com esse e-mail."
    });
}

public sealed record RegisterAccountRequest(
    string? Name,
    string? OrganizationName,
    string? Email,
    string? Password);

public sealed record LoginAccountRequest(string? Email, string? Password, bool RememberMe = false);

public sealed record AccountResponse(Guid UserId, string Name, string Email, Guid OrganizationId, string Role);
