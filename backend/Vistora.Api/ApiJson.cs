using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;

namespace Vistora.Api;

public static class ApiJson
{
    public static void Configure(JsonOptions options) =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
}
