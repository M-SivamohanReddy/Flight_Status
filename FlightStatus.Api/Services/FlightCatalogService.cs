using FlightStatus.Api.Data.Repositories;
using FlightStatus.Api.Models;
using FlightStatus.Api.Services.Interfaces;

namespace FlightStatus.Api.Services;

public sealed class FlightCatalogService(IFlightCatalogRepository repository) : IFlightCatalogService
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
}
