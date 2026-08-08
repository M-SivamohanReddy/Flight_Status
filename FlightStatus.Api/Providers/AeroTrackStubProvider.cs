using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

/// <summary>
/// Deterministic in-memory stub for AeroTrack — full detail provider.
/// Returns data for the 10 defined scenarios; returns null for unknown flights.
/// </summary>
public sealed class AeroTrackStubProvider : IFlightStatusProvider
{
    public string ProviderName => "AeroTrack";

    private static readonly DateTimeOffset Base = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyDictionary<string, ProviderFlightStatus> Data =
        new Dictionary<string, ProviderFlightStatus>(StringComparer.OrdinalIgnoreCase)
        {
            ["AA100"] = new()
            {
                FlightNumber = "AA100",
                ProviderName = "AeroTrack",
                RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base,
                ActualDeparture = Base.AddMinutes(5),
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = Base.AddHours(2).AddMinutes(5),
                Terminal = "T1",
                Gate = "A10",
                DelayReason = null,
                LastUpdatedUtc = Base.AddHours(1)
            },
            ["AA200"] = new()
            {
                FlightNumber = "AA200",
                ProviderName = "AeroTrack",
                RawStatus = "DELAYED",
                ScheduledDeparture = Base,
                ActualDeparture = Base.AddMinutes(45),
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = Base.AddHours(2).AddMinutes(45),
                Terminal = "T2",
                Gate = "B5",
                DelayReason = "Air Traffic Control",
                LastUpdatedUtc = Base.AddMinutes(45)
            },
            ["AA300"] = new()
            {
                FlightNumber = "AA300",
                ProviderName = "AeroTrack",
                RawStatus = "CANCELLED",
                ScheduledDeparture = Base,
                ActualDeparture = null,
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = null,
                Terminal = "T3",
                Gate = null,
                DelayReason = "Maintenance",
                LastUpdatedUtc = Base.AddHours(-2)
            },
            ["AA400"] = new()
            {
                FlightNumber = "AA400",
                ProviderName = "AeroTrack",
                RawStatus = "DIVERTED",
                ScheduledDeparture = Base,
                ActualDeparture = Base.AddMinutes(10),
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = Base.AddHours(2).AddMinutes(10),
                Terminal = "T1",
                Gate = "D3",
                DelayReason = null,
                LastUpdatedUtc = Base.AddHours(2)
            },
            ["AA700"] = new()
            {
                FlightNumber = "AA700",
                ProviderName = "AeroTrack",
                RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base,
                ActualDeparture = Base.AddMinutes(20),  // 20 min > 15 min threshold → Delayed
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = Base.AddHours(2).AddMinutes(20),
                Terminal = "T4",
                Gate = "E7",
                DelayReason = null,
                LastUpdatedUtc = Base.AddMinutes(30)
            },
            ["AA800"] = new()
            {
                FlightNumber = "AA800",
                ProviderName = "AeroTrack",
                RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base,
                ActualDeparture = Base.AddMinutes(5),
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = Base.AddHours(2).AddMinutes(5),
                Terminal = "T1",
                Gate = "F1",
                DelayReason = null,
                LastUpdatedUtc = new DateTimeOffset(2026, 1, 1, 7, 0, 0, TimeSpan.Zero)  // earlier than QuickFlight
            },
            ["AA900"] = new()
            {
                FlightNumber = "AA900",
                ProviderName = "AeroTrack",
                RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base,
                ActualDeparture = Base.AddMinutes(8),
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = Base.AddHours(2).AddMinutes(8),
                Terminal = "T5",
                Gate = "C14",
                DelayReason = null,
                LastUpdatedUtc = Base.AddMinutes(50)
            },
            ["AA1000"] = new()
            {
                FlightNumber = "AA1000",
                ProviderName = "AeroTrack",
                RawStatus = "DELAYED",
                ScheduledDeparture = Base,
                ActualDeparture = Base.AddMinutes(90),
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = Base.AddHours(2).AddMinutes(90),
                Terminal = "T2",
                Gate = "B8",
                DelayReason = "Weather",
                LastUpdatedUtc = Base.AddHours(1)
            }
        };

    public Task<ProviderFlightStatus?> GetFlightStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        Data.TryGetValue(flightNumber, out var result);
        return Task.FromResult(result);
    }
}
