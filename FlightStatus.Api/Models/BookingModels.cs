namespace FlightStatus.Api.Models;

public sealed record BookingRequest
{
    public required string FlightNumber { get; init; }
    public required DateOnly TravelDate { get; init; }
}

public sealed record BookingResponse
{
    public int Id { get; init; }
    public required string FlightNumber { get; init; }
    public string Route { get; init; } = "";
    public string Origin { get; init; } = "";
    public string Destination { get; init; } = "";
    public required string TravelDate { get; init; }
    public required string BookedAtUtc { get; init; }
    public string? UserEmail { get; init; }
    public string? UserFullName { get; init; }
}
