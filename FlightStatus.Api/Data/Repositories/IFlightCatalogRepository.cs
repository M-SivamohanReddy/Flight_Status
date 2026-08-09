using FlightStatus.Api.Data.Entities;

namespace FlightStatus.Api.Data.Repositories;

public interface IFlightCatalogRepository
{
    Task<IReadOnlyList<FlightCatalogEntity>> GetAllAsync(CancellationToken ct = default);
    Task<FlightCatalogEntity?> GetByFlightNumberAsync(string flightNumber, CancellationToken ct = default);
}
