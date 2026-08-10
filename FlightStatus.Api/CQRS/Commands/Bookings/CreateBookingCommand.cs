using FlightStatus.Api.Models;
using FlightStatus.Api.Services.Interfaces;
using MediatR;

namespace FlightStatus.Api.CQRS.Commands.Bookings;

public sealed record CreateBookingCommand(
    string UserId,
    string FlightNumber,
    DateOnly TravelDate) : IRequest<BookingResponse>;

public sealed class CreateBookingCommandHandler(IBookingService bookingService)
    : IRequestHandler<CreateBookingCommand, BookingResponse>
{
    public Task<BookingResponse> Handle(CreateBookingCommand request, CancellationToken ct)
        => bookingService.BookAsync(
            request.UserId,
            new BookingRequest { FlightNumber = request.FlightNumber, TravelDate = request.TravelDate },
            ct);
}
