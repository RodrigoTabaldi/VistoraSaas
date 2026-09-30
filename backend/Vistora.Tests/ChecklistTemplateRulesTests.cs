using Vistora.Application.UseCases;
using Xunit;

namespace Vistora.Tests;

public sealed class ChecklistTemplateRulesTests
{
    [Fact]
    public void Valid_template_has_rooms_and_at_least_one_item()
    {
        var rooms = new[]
        {
            new CreateChecklistTemplateRoomInput("Sala", 0,
                [new CreateChecklistTemplateItemInput("Piso", 0)])
        };

        Assert.True(ChecklistTemplateRules.TryValidate("Entrada", rooms, out var error));
        Assert.Empty(error);
    }

    [Fact]
    public void Duplicate_positions_are_rejected_before_the_database_constraint()
    {
        var rooms = new[]
        {
            new CreateChecklistTemplateRoomInput("Sala", 0, [
                new CreateChecklistTemplateItemInput("Piso", 0),
                new CreateChecklistTemplateItemInput("Paredes", 0)])
        };

        Assert.False(ChecklistTemplateRules.TryValidate("Entrada", rooms, out var error));
        Assert.Contains("positions must be unique", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Empty_rooms_or_items_are_rejected()
    {
        Assert.False(ChecklistTemplateRules.TryValidate("Entrada", [], out _));
        Assert.False(ChecklistTemplateRules.TryValidate("Entrada", [new("Sala", 0, [])], out _));
    }
}
