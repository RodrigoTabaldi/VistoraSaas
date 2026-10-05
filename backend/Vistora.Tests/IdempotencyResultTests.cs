using System.Text.Json;
using Vistora.Application.UseCases;
using Xunit;

namespace Vistora.Tests;

public sealed class IdempotencyResultTests
{
    [Fact]
    public void Inspection_creation_result_preserves_type_and_id_when_replayed()
    {
        CreateInspectionResult expected = new CreateInspectionResult.Created(Guid.NewGuid());
        var json = JsonSerializer.Serialize(expected, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(expected, JsonSerializer.Deserialize<CreateInspectionResult>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    [Fact]
    public void Completion_result_preserves_type_and_job_id_when_replayed()
    {
        CompleteInspectionResult expected = new CompleteInspectionResult.Created(Guid.NewGuid());
        var json = JsonSerializer.Serialize(expected, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(expected, JsonSerializer.Deserialize<CompleteInspectionResult>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
