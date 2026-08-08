namespace FlightStatus.Api.Models;

/// <summary>
/// Internal intermediate DTO produced by each provider after deserialising its proprietary response.
/// The normalisation service consumes this model — it is never exposed to API callers.
/// </summary>
public sealed record ProviderFlightStatus
{
    public required string FlightNumber { get; init; }
    public required string ProviderName { get; init; }
    public required string RawStatus { get; init; }
    public DateTimeOffset? ScheduledDeparture { get; init; }
    public DateTimeOffset? ActualDeparture { get; init; }
    public DateTimeOffset? ScheduledArrival { get; init; }
    public DateTimeOffset? ActualArrival { get; init; }
    public string? Terminal { get; init; }
    public string? Gate { get; init; }
    public string? DelayReason { get; init; }
    public required DateTimeOffset LastUpdatedUtc { get; init; }
}
