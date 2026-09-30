namespace Vistora.Application.UseCases;

public static class ChecklistTemplateRules
{
    // Rejects invalid structure before it reaches database length and position constraints.
    public static bool TryValidate(
        string? name,
        IReadOnlyList<CreateChecklistTemplateRoomInput>? rooms,
        out string error)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
        {
            error = "Template name is required and must have at most 200 characters.";
            return false;
        }

        if (rooms is null or { Count: 0 } or { Count: > 50 })
        {
            error = "A template must contain between 1 and 50 rooms.";
            return false;
        }

        if (rooms.Any(room => string.IsNullOrWhiteSpace(room.Name) || room.Name.Trim().Length > 200 || room.Position < 0) ||
            rooms.Select(room => room.Position).Distinct().Count() != rooms.Count)
        {
            error = "Room names must be valid and room positions must be unique non-negative numbers.";
            return false;
        }

        var items = rooms.SelectMany(room => room.Items ?? Array.Empty<CreateChecklistTemplateItemInput>()).ToList();
        if (items.Count is 0 or > 2000 || rooms.Any(room => room.Items.Count > 100))
        {
            error = "A template must contain between 1 and 2,000 items, with at most 100 items per room.";
            return false;
        }

        foreach (var room in rooms)
        {
            if (room.Items.Any(item => string.IsNullOrWhiteSpace(item.Description) || item.Description.Trim().Length > 1000 || item.Position < 0) ||
                room.Items.Select(item => item.Position).Distinct().Count() != room.Items.Count)
            {
                error = "Item descriptions must be valid and item positions must be unique non-negative numbers per room.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }
}
