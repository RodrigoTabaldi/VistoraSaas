using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Vistora.Api;
using Vistora.Domain;
using Xunit;

namespace Vistora.Tests;

public sealed class ApiJsonTests
{
    [Fact]
    public void InspectionEnumsRoundTripAsNamesUsedByTheFrontend()
    {
        var options = new JsonOptions();
        ApiJson.Configure(options);

        Assert.Equal("\"MoveIn\"", JsonSerializer.Serialize(InspectionType.MoveIn, options.SerializerOptions));
        Assert.Equal("\"Approved\"", JsonSerializer.Serialize(InspectionStatus.Approved, options.SerializerOptions));
        Assert.Equal(InspectionStatus.Approved,
            JsonSerializer.Deserialize<InspectionStatus>("\"Approved\"", options.SerializerOptions));
    }
}
