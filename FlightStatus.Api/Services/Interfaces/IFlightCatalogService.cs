using FlightStatus.Api.Models;

namespace FlightStatus.Api.Services.Interfaces;

public interface IFlightCatalogService
{
    Task<IReadOnlyList<FlightInfo>> GetAllAsync(CancellationToken ct = default);
    Task<FlightInfo?> GetByNumberAsync(string flightNumber, CancellationToken ct = default);
}
