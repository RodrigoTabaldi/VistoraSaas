using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vistora.Application.Messaging;
using Vistora.Application.Persistence;
using Vistora.Application.Storage;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Vistora.Api;
using Xunit;

namespace Vistora.Tests;

public sealed class AccessPoliciesTests
{
    [Theory]
    [InlineData("Admin", true, true)]
    [InlineData("Vistoriador", false, true)]
    [InlineData("Leitor", false, false)]
    [InlineData("Unknown", false, false)]
    public void RolesHaveOnlyTheirAuthorizedWrites(string role, bool canManage, bool canEdit)
    {
        var options = new AuthorizationOptions();
        AccessPolicies.Configure(options);
        Assert.Equal(canManage, Allows(options.GetPolicy(AccessPolicies.ManageOrganization)!, role));
        Assert.Equal(canEdit, Allows(options.GetPolicy(AccessPolicies.EditInspection)!, role));
    }

    private static bool Allows(AuthorizationPolicy policy, string role) =>
        policy.Requirements.OfType<RolesAuthorizationRequirement>()
            .Single().AllowedRoles.Contains(role);

    [Theory]
    [InlineData("POST", "/api/v1/properties", AccessPolicies.ManageOrganization)]
    [InlineData("POST", "/api/v1/inspections", AccessPolicies.EditInspection)]
    [InlineData("PATCH", "/api/v1/inspections/{inspectionId:guid}/status", AccessPolicies.ManageOrganization)]
    [InlineData("POST", "/api/v1/items/{itemId:guid}/evidence", AccessPolicies.EditInspection)]
    [InlineData("POST", "/api/v1/inspections/{inspectionId:guid}/report-jobs/retry", AccessPolicies.ManageOrganization)]
    public void WriteRoutesRequireTheExpectedPolicy(string method, string path, string expectedPolicy)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<ITenantContext>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<VistoraDbContext>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<IMessageBus>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<IPrivateObjectStorage>(_ => throw new NotSupportedException());
        var app = builder.Build();
        app.MapVistoraApi(requireAuthorization: true);

        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(x => x.RoutePattern.RawText?.TrimEnd('/') == path &&
                x.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(method) == true);
        Assert.Contains(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
            metadata => metadata.Policy == expectedPolicy);
    }
}
