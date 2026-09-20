using Microsoft.AspNetCore.Authentication.JwtBearer;
using Vistora.Api;
using Vistora.Application;
using Vistora.Application.Messaging;
using Vistora.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddVistoraApplication()
    .AddVistoraInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var authority = builder.Configuration["Authentication:Authority"];
if (!string.IsNullOrWhiteSpace(authority))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = builder.Configuration["Authentication:Audience"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        });
    builder.Services.AddAuthorization();
}

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await Results.Problem(
        detail: app.Environment.IsDevelopment() ? exception?.Message : "An unexpected error occurred.",
        statusCode: StatusCodes.Status500InternalServerError,
        title: "Request failed").ExecuteAsync(context);
}));

app.UseMiddleware<TenantResolutionMiddleware>();
if (!string.IsNullOrWhiteSpace(authority))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapHealthChecks("/health");

var messages = app.MapGroup("/api/v1/messages");
if (!string.IsNullOrWhiteSpace(authority)) messages.RequireAuthorization();
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

app.MapVistoraApi(!string.IsNullOrWhiteSpace(authority));

app.Run();

public sealed record PublishMessageRequest(string Type, string Payload);
public partial class Program;
