namespace Vistora.Domain;

public static class InspectionComparison
{
    public static IReadOnlyList<InspectionDifference> Compare(Inspection moveIn, Inspection moveOut)
    {
        if (moveIn.UnitId != moveOut.UnitId || moveIn.Type != InspectionType.MoveIn ||
            moveIn.Status != InspectionStatus.Approved || moveOut.Type != InspectionType.MoveOut)
            throw new ArgumentException("Comparison requires an approved entry and an exit for the same unit.");

        var entries = Flatten(moveIn);
        var exits = Flatten(moveOut);
        var keys = entries.Keys.Union(exits.Keys).OrderBy(x => x.Room).ThenBy(x => x.RoomIndex)
            .ThenBy(x => x.Item).ThenBy(x => x.ItemIndex);
        return keys.Select(key =>
        {
            var hasEntry = entries.TryGetValue(key, out var entry);
            var hasExit = exits.TryGetValue(key, out var exit);
            return new InspectionDifference(
                hasExit ? exit.Room : entry.Room,
                hasExit ? exit.Item.Description : entry.Item.Description,
                hasEntry ? entry.Item.Response : null,
                hasExit ? exit.Item.Response : null,
                hasEntry ? entry.Item.Notes : null,
                hasExit ? exit.Item.Notes : null,
                !hasEntry || !hasExit ||
                !string.Equals(entry.Item.Response, exit.Item.Response, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(entry.Item.Notes, exit.Item.Notes, StringComparison.Ordinal));
        }).ToList();
    }

    private static Dictionary<(string Room, int RoomIndex, string Item, int ItemIndex), (string Room, InspectionItem Item)> Flatten(Inspection inspection)
    {
        var result = new Dictionary<(string Room, int RoomIndex, string Item, int ItemIndex), (string Room, InspectionItem Item)>();
        var roomOccurrences = new Dictionary<string, int>();
        foreach (var room in inspection.Rooms.OrderBy(x => x.Position))
        {
            var roomKey = room.Name.Trim().ToUpperInvariant();
            roomOccurrences.TryGetValue(roomKey, out var roomIndex);
            roomOccurrences[roomKey] = roomIndex + 1;
            var occurrences = new Dictionary<string, int>();
            foreach (var item in room.Items.OrderBy(x => x.Position))
            {
                var itemKey = item.Description.Trim().ToUpperInvariant();
                occurrences.TryGetValue(itemKey, out var index);
                occurrences[itemKey] = index + 1;
                result.Add((roomKey, roomIndex, itemKey, index), (room.Name, item));
            }
        }
        return result;
    }
}

public sealed record InspectionDifference(
    string Room,
    string Item,
    string? MoveInResponse,
    string? MoveOutResponse,
    string? MoveInNotes,
    string? MoveOutNotes,
    bool Changed);
