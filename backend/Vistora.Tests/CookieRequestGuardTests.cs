using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vistora.Api.Endpoints;
using Vistora.Api.Middleware;
using Xunit;

namespace Vistora.Tests;

public sealed class CookieRequestGuardTests
{
    [Theory]
    [InlineData("POST", false)]
    [InlineData("PUT", false)]
    [InlineData("PATCH", false)]
    [InlineData("DELETE", false)]
    [InlineData("GET", true)]
    public void CookieWritesRequireTheApplicationHeader(string method, bool expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.User = new ClaimsPrincipal(new ClaimsIdentity([], AccountEndpoints.CookieScheme));
        Assert.Equal(expected, CookieRequestGuard.IsAllowed(context));
        context.Request.Headers[CookieRequestGuard.HeaderName] = "1";
        Assert.True(CookieRequestGuard.IsAllowed(context));
    }

    [Fact]
    public void BearerApiClientsDoNotNeedTheCookieHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([], "Bearer"));
        Assert.True(CookieRequestGuard.IsAllowed(context));
    }
}
