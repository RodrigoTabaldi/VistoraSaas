using Vistora.Api.Endpoints;

namespace Vistora.Api.Middleware;

public static class CookieRequestGuard
{
    public const string HeaderName = "X-Vistora-Request";

    public static bool IsAllowed(HttpContext context)
    {
        var method = context.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
            return true;

        if (context.User.Identity?.AuthenticationType != AccountEndpoints.CookieScheme)
            return true;

        return context.Request.Headers[HeaderName] == "1";
    }
}
