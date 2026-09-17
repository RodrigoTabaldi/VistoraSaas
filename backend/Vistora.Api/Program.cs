using Vistora.Application;
using Vistora.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddVistoraApplication()
    .AddVistoraInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
