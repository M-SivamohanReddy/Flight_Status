using FlightStatus.Api.Models;

namespace FlightStatus.Api.Services.Interfaces;

public interface IFlightStatusQueryService
{
    Task<FlightStatusResult> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
