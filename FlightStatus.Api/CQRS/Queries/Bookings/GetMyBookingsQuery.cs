using FlightStatus.Api.Models;
using FlightStatus.Api.Services.Interfaces;
using MediatR;

namespace FlightStatus.Api.CQRS.Queries.Bookings;

public sealed record GetMyBookingsQuery(string UserId) : IRequest<IReadOnlyList<BookingResponse>>;

public sealed class GetMyBookingsQueryHandler(IBookingService bookingService)
    : IRequestHandler<GetMyBookingsQuery, IReadOnlyList<BookingResponse>>
{
    public Task<IReadOnlyList<BookingResponse>> Handle(GetMyBookingsQuery request, CancellationToken ct)
        => bookingService.GetMyBookingsAsync(request.UserId, ct);
}
