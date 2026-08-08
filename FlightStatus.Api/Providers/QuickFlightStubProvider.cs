using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

/// <summary>
/// Deterministic in-memory stub for QuickFlight — minimal provider (no actual times, terminal, gate, or delay reason).
/// Returns data for the 10 defined scenarios; returns null for unknown flights.
/// </summary>
public sealed class QuickFlightStubProvider : IFlightStatusProvider
{
    public string ProviderName => "QuickFlight";

    private static readonly DateTimeOffset Base = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyDictionary<string, ProviderFlightStatus> Data =
        new Dictionary<string, ProviderFlightStatus>(StringComparer.OrdinalIgnoreCase)
        {
            ["AA100"] = new()
            {
                FlightNumber = "AA100",
                ProviderName = "QuickFlight",
                RawStatus = "on-time",
                ScheduledDeparture = Base,
                ActualDeparture = null,
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = null,
                LastUpdatedUtc = Base.AddMinutes(45)
            },
            ["AA200"] = new()
            {
                FlightNumber = "AA200",
                ProviderName = "QuickFlight",
                RawStatus = "delayed",
                ScheduledDeparture = Base,
                ActualDeparture = null,
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = null,
                LastUpdatedUtc = Base.AddMinutes(30)
            },
            ["AA300"] = new()
            {
                FlightNumber = "AA300",
                ProviderName = "QuickFlight",
                RawStatus = "cancelled",
                ScheduledDeparture = Base,
                ActualDeparture = null,
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = null,
                LastUpdatedUtc = Base.AddMinutes(-150)  // earlier than AeroTrack
            },
            ["AA500"] = new()
            {
                FlightNumber = "AA500",
                ProviderName = "QuickFlight",
                RawStatus = "on-time",
                ScheduledDeparture = Base,
                ActualDeparture = null,
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = null,
                LastUpdatedUtc = Base.AddMinutes(20)
            },
            ["AA700"] = new()
            {
                FlightNumber = "AA700",
                ProviderName = "QuickFlight",
                RawStatus = "on-time",
                ScheduledDeparture = Base,
                ActualDeparture = null,
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = null,
                LastUpdatedUtc = Base.AddMinutes(10)
            },
            ["AA800"] = new()
            {
                FlightNumber = "AA800",
                ProviderName = "QuickFlight",
                RawStatus = "on-time",
                ScheduledDeparture = Base,
                ActualDeparture = null,
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = null,
                LastUpdatedUtc = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero)  // later than AeroTrack
            },
            ["AA900"] = new()
            {
                FlightNumber = "AA900",
                ProviderName = "QuickFlight",
                RawStatus = "on-time",
                ScheduledDeparture = Base,
                ActualDeparture = null,
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = null,
                LastUpdatedUtc = Base.AddMinutes(30)
            },
            ["AA1000"] = new()
            {
                FlightNumber = "AA1000",
                ProviderName = "QuickFlight",
                RawStatus = "delayed",
                ScheduledDeparture = Base,
                ActualDeparture = null,
                ScheduledArrival = Base.AddHours(2),
                ActualArrival = null,
                LastUpdatedUtc = Base.AddMinutes(45)
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
