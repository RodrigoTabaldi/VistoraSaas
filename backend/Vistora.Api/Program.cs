using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Vistora.Api;
using Vistora.Api.Endpoints;
using Vistora.Api.Middleware;
using Vistora.Application;
using Vistora.Application.Messaging;
using Vistora.Domain;
using Vistora.Infrastructure;
using Vistora.Infrastructure.Persistence.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddVistoraApplication()
    .AddVistoraInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddScoped<AccountRegistrationService>();

var authority = builder.Configuration["Authentication:Authority"];
const string authenticationScheme = "VistoraAuth";
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = authenticationScheme;
        options.DefaultChallengeScheme = authenticationScheme;
        options.DefaultScheme = authenticationScheme;
    })
    .AddPolicyScheme(authenticationScheme, authenticationScheme, options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var hasBearerToken = context.Request.Headers.Authorization
                .ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
            return hasBearerToken && !string.IsNullOrWhiteSpace(authority)
                ? JwtBearerDefaults.AuthenticationScheme
                : AccountEndpoints.CookieScheme;
        };
    })
    .AddCookie(AccountEndpoints.CookieScheme, options =>
    {
        options.Cookie.Name = "vistora.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
if (!string.IsNullOrWhiteSpace(authority))
{
    builder.Services.AddAuthentication()
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = authority;
            options.Audience = builder.Configuration["Authentication:Audience"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        });
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AccountEndpoints.RateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
  {
      var exception = context.Features
          .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?
          .Error;

      context.Response.StatusCode = StatusCodes.Status500InternalServerError;

      await Results.Problem(
          detail: app.Environment.IsDevelopment()
              ? exception?.Message
              : "An unexpected error occurred.",
          statusCode: StatusCodes.Status500InternalServerError,
          title: "Request failed")
          .ExecuteAsync(context);
  }));

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<VistoraDbContext>();
    await db.Database.MigrateAsync();
}

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapInspectionsEndpoints();
app.MapChecklistTemplatesEndpoints();
app.MapAccountEndpoints();

var messages = app.MapGroup("/api/v1/messages");
messages.RequireAuthorization();
messages.MapPost("", async (PublishMessageRequest request, IMessageBus messageBus, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Type) || request.Type.Length > 128)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["type"] = ["Type is required and must have at most 128 characters."] });
    }

    if (string.IsNullOrWhiteSpace(request.Payload) || request.Payload.Length > 64 * 1024)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["payload"] = ["Payload is required and must have at most 64 KiB."] });
    }

    var message = new MessageEnvelope(Guid.NewGuid(), request.Type.Trim(), request.Payload,
        DateTimeOffset.UtcNow);
    await messageBus.PublishAsync(message, cancellationToken);
    return Results.Accepted($"/api/v1/messages/{message.Id}", new { message.Id, message.Type, message.CreatedAtUtc });
});

app.MapVistoraApi(requireAuthorization: true);

app.Run();

public sealed record PublishMessageRequest(string Type, string Payload);
public partial class Program;
