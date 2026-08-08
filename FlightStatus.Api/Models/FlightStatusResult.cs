namespace FlightStatus.Api.Models;

/// <summary>
/// Unified response returned to API callers for every valid request.
/// A successful query always returns this record — Unknown status handles the no-data case.
/// </summary>
public sealed record FlightStatusResult
{
    public required string FlightNumber { get; init; }
    public required DateOnly Date { get; init; }
    public required FlightStatus Status { get; init; }
    public DateTimeOffset? ScheduledDeparture { get; init; }
    public DateTimeOffset? ActualDeparture { get; init; }
    public DateTimeOffset? ScheduledArrival { get; init; }
    public DateTimeOffset? ActualArrival { get; init; }
    public string? Terminal { get; init; }
    public string? Gate { get; init; }
    public string? DelayReason { get; init; }
    public DateTimeOffset LastUpdatedUtc { get; init; }
    public required string SourceProvider { get; init; }
    public string? Message { get; init; }
}
