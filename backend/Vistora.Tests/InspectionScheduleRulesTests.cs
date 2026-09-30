using Vistora.Application.UseCases;
using Xunit;

namespace Vistora.Tests;

public sealed class InspectionScheduleRulesTests
{
    [Fact]
    public void Optional_schedule_can_be_cleared_but_past_and_current_times_are_rejected()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.True(InspectionScheduleRules.IsValid(null, now));
        Assert.True(InspectionScheduleRules.IsValid(now.AddTicks(1), now));
        Assert.False(InspectionScheduleRules.IsValid(now, now));
        Assert.False(InspectionScheduleRules.IsValid(now.AddTicks(-1), now));
    }

    [Fact]
    public void Schedule_is_normalized_to_utc_before_persistence()
    {
        var localOffset = new DateTimeOffset(2030, 5, 10, 9, 30, 0, TimeSpan.FromHours(-3));

        Assert.Equal(TimeSpan.Zero, InspectionScheduleRules.Normalize(localOffset)!.Value.Offset);
        Assert.Null(InspectionScheduleRules.Normalize(null));
    }
}
