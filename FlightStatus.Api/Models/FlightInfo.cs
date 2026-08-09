namespace FlightStatus.Api.Models;

public sealed record FlightInfo
{
    public required string FlightNumber { get; init; }
    public required string Route { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
}
