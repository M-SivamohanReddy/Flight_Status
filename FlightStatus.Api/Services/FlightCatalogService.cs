using FlightStatus.Api.Data.Repositories;
using FlightStatus.Api.Models;

namespace FlightStatus.Api.Services;

public sealed class FlightCatalogService(IFlightCatalogRepository repository)
{
    public async Task<IReadOnlyList<FlightInfo>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await repository.GetAllAsync(ct);
        return entities.Select(e => new FlightInfo
        {
            FlightNumber = e.FlightNumber,
            Route        = e.Route,
            Origin       = e.Origin,
            Destination  = e.Destination
        }).ToList();
    }

    public async Task<FlightInfo?> GetByNumberAsync(string flightNumber, CancellationToken ct = default)
    {
        var entity = await repository.GetByFlightNumberAsync(flightNumber, ct);
        if (entity is null) return null;
        return new FlightInfo
        {
            FlightNumber = entity.FlightNumber,
            Route        = entity.Route,
            Origin       = entity.Origin,
            Destination  = entity.Destination
        };
    }
}
