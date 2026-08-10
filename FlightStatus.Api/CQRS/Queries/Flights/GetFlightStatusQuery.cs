using FlightStatus.Api.Models;
using FlightStatus.Api.Services.Interfaces;
using MediatR;

namespace FlightStatus.Api.CQRS.Queries.Flights;

public sealed record GetFlightStatusQuery(string FlightNumber, DateOnly Date) : IRequest<FlightStatusResult>;

public sealed class GetFlightStatusQueryHandler(IFlightStatusQueryService queryService)
    : IRequestHandler<GetFlightStatusQuery, FlightStatusResult>
{
    public Task<FlightStatusResult> Handle(GetFlightStatusQuery request, CancellationToken ct)
        => queryService.GetStatusAsync(request.FlightNumber, request.Date, ct);
}
