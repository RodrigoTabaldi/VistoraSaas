using Vistora.Domain;

namespace Vistora.Infrastructure.Persistence.PostgreSql;

public static class InspectionReadQueries
{
    public static IQueryable<InspectionSummary> Summaries(IQueryable<Inspection> inspections) => inspections.Select(x => new InspectionSummary(
        x.Id, x.UnitId, x.Type, x.Status, x.CreatedAtUtc, x.ScheduledAtUtc,
        x.Unit!.Property!.Name, x.Unit.Property.Address, x.Unit.Identifier,
        x.Rooms.SelectMany(r => r.Items).Count(),
        x.Rooms.SelectMany(r => r.Items).Count(i => i.Response == "Conforme" || i.Response == "Atenção" || i.Response == "Não conforme"),
        x.Reports.Any()));

    public static IQueryable<InspectionStatistic> Statistics(IQueryable<Inspection> inspections) => inspections.GroupBy(x => x.Type)
        .Select(group => new InspectionStatistic(group.Key, group.Count(),
            group.Count(x => x.Status != InspectionStatus.Draft),
            group.Count(x => x.Status != InspectionStatus.Draft && !x.Reports.Any())));
}

public sealed record InspectionSummary(Guid Id, Guid UnitId, InspectionType Type, InspectionStatus Status,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? ScheduledAtUtc, string PropertyName, string Address,
    string UnitIdentifier, int TotalItems, int AnsweredItems, bool HasReport);
public sealed record InspectionStatistic(InspectionType Type, int Total, int Completed, int PendingReports);
