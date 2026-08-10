using FlightStatus.Api.Models;
using FlightStatus.Api.Services.Interfaces;
using MediatR;

namespace FlightStatus.Api.CQRS.Queries.Flights;

public sealed record GetFlightCatalogQuery : IRequest<IReadOnlyList<FlightInfo>>;

public sealed class GetFlightCatalogQueryHandler(IFlightCatalogService catalogService)
    : IRequestHandler<GetFlightCatalogQuery, IReadOnlyList<FlightInfo>>
{
    public Task<IReadOnlyList<FlightInfo>> Handle(GetFlightCatalogQuery request, CancellationToken ct)
        => catalogService.GetAllAsync(ct);
}
