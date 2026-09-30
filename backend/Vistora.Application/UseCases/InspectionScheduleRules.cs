namespace Vistora.Application.UseCases;

public static class InspectionScheduleRules
{
    // Keeps create and reschedule flows consistent about future UTC timestamps.
    public static bool IsValid(DateTimeOffset? scheduledAtUtc, DateTimeOffset nowUtc) =>
        !scheduledAtUtc.HasValue || scheduledAtUtc.Value.ToUniversalTime() > nowUtc.ToUniversalTime();

    public static DateTimeOffset? Normalize(DateTimeOffset? scheduledAtUtc) => scheduledAtUtc?.ToUniversalTime();
}
