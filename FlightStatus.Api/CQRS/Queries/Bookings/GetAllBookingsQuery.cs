using FlightStatus.Api.Models;
using FlightStatus.Api.Services.Interfaces;
using MediatR;

namespace FlightStatus.Api.CQRS.Queries.Bookings;

public sealed record GetAllBookingsQuery : IRequest<IReadOnlyList<BookingResponse>>;

public sealed class GetAllBookingsQueryHandler(IBookingService bookingService)
    : IRequestHandler<GetAllBookingsQuery, IReadOnlyList<BookingResponse>>
{
    public Task<IReadOnlyList<BookingResponse>> Handle(GetAllBookingsQuery request, CancellationToken ct)
        => bookingService.GetAllBookingsAsync(ct);
}
