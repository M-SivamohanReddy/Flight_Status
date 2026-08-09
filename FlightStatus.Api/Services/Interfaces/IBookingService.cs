using FlightStatus.Api.Models;

namespace FlightStatus.Api.Services.Interfaces;

public interface IBookingService
{
    Task<BookingResponse> BookAsync(string userId, BookingRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<BookingResponse>> GetMyBookingsAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<BookingResponse>> GetAllBookingsAsync(CancellationToken ct = default);
}
