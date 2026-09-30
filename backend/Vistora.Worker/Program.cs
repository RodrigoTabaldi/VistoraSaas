using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vistora.Application;
using Vistora.Infrastructure;
using Vistora.Infrastructure.Messaging;
using Vistora.Infrastructure.Reporting;
using Vistora.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddVistoraApplication()
    .AddVistoraInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<WorkerReadinessHealthCheck>("worker_readiness");
builder.Services.AddHostedService<HealthCheckStartupService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<ReportJobConsumer>();
builder.Services.AddHostedService<ReportJobRetryScanner>();
builder.Services.AddHostedService<MessageConsumerWorker>();

var app = builder.Build();
app.MapHealthChecks("/health");
await app.RunAsync();
