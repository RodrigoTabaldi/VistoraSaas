using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vistora.Application;
using Vistora.Infrastructure;
using Vistora.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddVistoraApplication()
    .AddVistoraInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<WorkerReadinessHealthCheck>("worker_readiness");
builder.Services.AddHostedService<HealthCheckStartupService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
