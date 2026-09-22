using Vistora.Api.Endpoints;
using Vistora.Api.Middleware;
using Vistora.Application;
using Vistora.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddVistoraApplication()
    .AddVistoraInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<DevOnlyTenantResolutionMiddleware>();

app.MapHealthChecks("/health");
app.MapInspectionsEndpoints();
app.MapChecklistTemplatesEndpoints();

app.Run();

public partial class Program;
