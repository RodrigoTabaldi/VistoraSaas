using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vistora.Api.Endpoints;
using Vistora.Application.Persistence;
using Vistora.Application.Storage;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Xunit;

namespace Vistora.Tests;

public sealed class InspectionAcceptanceEndpointStartupTests
{
    [Fact]
    public void Acceptance_routes_can_be_registered_without_startup_binding_errors()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<ITenantContext>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<VistoraDbContext>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<IPrivateObjectStorage>(_ => throw new NotSupportedException());
        var app = builder.Build();

        app.MapInspectionAcceptanceEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText?.TrimEnd('/') == "/api/v1/inspections/{inspectionId:guid}/acceptance" &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST") == true);
    }
}
