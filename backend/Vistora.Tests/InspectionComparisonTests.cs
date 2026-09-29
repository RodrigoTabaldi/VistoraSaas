using Vistora.Domain;
using Xunit;

namespace Vistora.Tests;

public sealed class InspectionComparisonTests
{
    [Fact]
    public void ComparesConditionsAndKeepsItemsMissingFromEitherInspection()
    {
        var unitId = Guid.NewGuid();
        var moveIn = MakeInspection(unitId, InspectionType.MoveIn, InspectionStatus.Approved,
            ("Sala", "Parede", "Conforme", null),
            ("Sala", "Porta", "Conforme", null));
        var moveOut = MakeInspection(unitId, InspectionType.MoveOut, InspectionStatus.Draft,
            ("Sala", "Parede", "Não conforme", "Risco"),
            ("Sala", "Janela", "Conforme", null));

        var differences = InspectionComparison.Compare(moveIn, moveOut);

        Assert.Equal(3, differences.Count);
        var wall = Assert.Single(differences, x => x.Item == "Parede");
        Assert.Equal("Conforme", wall.MoveInResponse);
        Assert.Equal("Não conforme", wall.MoveOutResponse);
        Assert.True(wall.Changed);
        Assert.Null(Assert.Single(differences, x => x.Item == "Porta").MoveOutResponse);
        Assert.Null(Assert.Single(differences, x => x.Item == "Janela").MoveInResponse);
    }

    [Fact]
    public void RejectsAnEntryFromAnotherUnit()
    {
        var moveIn = MakeInspection(Guid.NewGuid(), InspectionType.MoveIn, InspectionStatus.Approved);
        var moveOut = MakeInspection(Guid.NewGuid(), InspectionType.MoveOut, InspectionStatus.Draft);

        Assert.Throws<ArgumentException>(() => InspectionComparison.Compare(moveIn, moveOut));
    }

    private static Inspection MakeInspection(Guid unitId, InspectionType type, InspectionStatus status,
        params (string Room, string Item, string Response, string? Notes)[] values)
    {
        var inspection = new Inspection { Id = Guid.NewGuid(), UnitId = unitId,
            Type = type, Status = status };
        foreach (var value in values)
        {
            var room = inspection.Rooms.SingleOrDefault(x => x.Name == value.Room);
            if (room is null)
            {
                room = new InspectionRoom { Id = Guid.NewGuid(), InspectionId = inspection.Id,
                    Name = value.Room, Position = inspection.Rooms.Count };
                inspection.Rooms.Add(room);
            }
            room.Items.Add(new InspectionItem { Id = Guid.NewGuid(), InspectionRoomId = room.Id,
                Description = value.Item, Response = value.Response, Notes = value.Notes,
                Position = room.Items.Count });
        }
        return inspection;
    }
}
