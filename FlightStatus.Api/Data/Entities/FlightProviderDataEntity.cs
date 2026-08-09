namespace FlightStatus.Api.Data.Entities;

public sealed class FlightProviderDataEntity
{
    public int Id { get; set; }
    public string FlightNumber { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public string RawStatus { get; set; } = "";
    public DateTimeOffset? ScheduledDeparture { get; set; }
    public DateTimeOffset? ActualDeparture { get; set; }
    public DateTimeOffset? ScheduledArrival { get; set; }
    public DateTimeOffset? ActualArrival { get; set; }
    public string? Terminal { get; set; }
    public string? Gate { get; set; }
    public string? DelayReason { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }
}
