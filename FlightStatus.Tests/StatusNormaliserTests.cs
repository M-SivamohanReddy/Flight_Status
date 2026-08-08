using FlightStatus.Api.Models;
using FlightStatus.Api.Services;

namespace FlightStatus.Tests;

public class StatusNormaliserTests
{
    private static ProviderFlightStatus Build(
        string rawStatus,
        DateTimeOffset? scheduledDep = null,
        DateTimeOffset? actualDep = null,
        DateTimeOffset? scheduledArr = null,
        DateTimeOffset? actualArr = null) =>
        new()
        {
            FlightNumber = "TEST1",
            ProviderName = "Test",
            RawStatus = rawStatus,
            ScheduledDeparture = scheduledDep,
            ActualDeparture = actualDep,
            ScheduledArrival = scheduledArr,
            ActualArrival = actualArr,
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };

    private static readonly DateTimeOffset T = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Cancelled_raw_returns_Cancelled()
    {
        Assert.Equal(Api.Models.FlightStatus.Cancelled, StatusNormaliser.Normalise(Build("CANCELLED")));
        Assert.Equal(Api.Models.FlightStatus.Cancelled, StatusNormaliser.Normalise(Build("cancel")));
        Assert.Equal(Api.Models.FlightStatus.Cancelled, StatusNormaliser.Normalise(Build("canceled")));
    }

    [Fact]
    public void Diverted_raw_returns_Diverted()
    {
        Assert.Equal(Api.Models.FlightStatus.Diverted, StatusNormaliser.Normalise(Build("DIVERTED")));
    }

    [Fact]
    public void OnSchedule_with_5min_delta_returns_OnTime()
    {
        var p = Build("ON_SCHEDULE", scheduledDep: T, actualDep: T.AddMinutes(5));
        Assert.Equal(Api.Models.FlightStatus.OnTime, StatusNormaliser.Normalise(p));
    }

    [Fact]
    public void OnSchedule_with_exactly_15min_delta_returns_OnTime()
    {
        var p = Build("ON_SCHEDULE", scheduledDep: T, actualDep: T.AddMinutes(15));
        Assert.Equal(Api.Models.FlightStatus.OnTime, StatusNormaliser.Normalise(p));
    }

    [Fact]
    public void OnSchedule_with_20min_delta_returns_Delayed()
    {
        // Scenario S7: raw label says ON_SCHEDULE but delta overrides it
        var p = Build("ON_SCHEDULE", scheduledDep: T, actualDep: T.AddMinutes(20));
        Assert.Equal(Api.Models.FlightStatus.Delayed, StatusNormaliser.Normalise(p));
    }

    [Fact]
    public void Delayed_raw_with_no_actual_times_returns_Delayed()
    {
        var p = Build("DELAYED");
        Assert.Equal(Api.Models.FlightStatus.Delayed, StatusNormaliser.Normalise(p));
    }

    [Fact]
    public void Delayed_raw_with_5min_delta_returns_OnTime()
    {
        // Time delta is more reliable than raw label
        var p = Build("DELAYED", scheduledDep: T, actualDep: T.AddMinutes(5));
        Assert.Equal(Api.Models.FlightStatus.OnTime, StatusNormaliser.Normalise(p));
    }

    [Fact]
    public void OnTime_raw_with_no_actual_times_returns_OnTime()
    {
        var p = Build("on-time");
        Assert.Equal(Api.Models.FlightStatus.OnTime, StatusNormaliser.Normalise(p));
    }

    [Fact]
    public void Unknown_raw_returns_Unknown()
    {
        var p = Build("unknown");
        Assert.Equal(Api.Models.FlightStatus.Unknown, StatusNormaliser.Normalise(p));
    }

    [Fact]
    public void ArrivalDelta_used_when_no_departure_actual()
    {
        var p = Build("on-time", scheduledArr: T, actualArr: T.AddMinutes(20));
        Assert.Equal(Api.Models.FlightStatus.Delayed, StatusNormaliser.Normalise(p));
    }

    [Fact]
    public void Departure_delta_preferred_over_arrival_delta()
    {
        // Departure is on-time (5 min) but arrival would be delayed (30 min)
        var p = Build("on-time",
            scheduledDep: T, actualDep: T.AddMinutes(5),
            scheduledArr: T.AddHours(2), actualArr: T.AddHours(2).AddMinutes(30));
        Assert.Equal(Api.Models.FlightStatus.OnTime, StatusNormaliser.Normalise(p));
    }
}
