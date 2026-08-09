using FlightStatus.Api.Data.Entities;
using FlightStatus.Api.Data.Repositories;
using FlightStatus.Api.Models;

namespace FlightStatus.Api.Services;

public sealed class BookingService(
    IBookingRepository bookingRepository,
    IFlightCatalogRepository catalogRepository)
{
    public async Task<BookingResponse> BookAsync(string userId, BookingRequest request, CancellationToken ct = default)
    {
        var booking = new FlightBookingEntity
        {
            UserId       = userId,
            FlightNumber = request.FlightNumber.ToUpperInvariant(),
            TravelDate   = request.TravelDate,
            BookedAtUtc  = DateTimeOffset.UtcNow
        };
        var saved  = await bookingRepository.AddAsync(booking, ct);
        var flight = await catalogRepository.GetByFlightNumberAsync(request.FlightNumber, ct);
        return Map(saved, flight);
    }

    public async Task<IReadOnlyList<BookingResponse>> GetMyBookingsAsync(string userId, CancellationToken ct = default)
    {
        var bookings   = await bookingRepository.GetByUserIdAsync(userId, ct);
        var flightMap  = (await catalogRepository.GetAllAsync(ct))
            .ToDictionary(f => f.FlightNumber);
        return bookings.Select(b =>
        {
            flightMap.TryGetValue(b.FlightNumber, out var f);
            return Map(b, f);
        }).ToList();
    }

    public async Task<IReadOnlyList<BookingResponse>> GetAllBookingsAsync(CancellationToken ct = default)
    {
        var bookings  = await bookingRepository.GetAllAsync(ct);
        var flightMap = (await catalogRepository.GetAllAsync(ct))
            .ToDictionary(f => f.FlightNumber);
        return bookings.Select(b =>
        {
            flightMap.TryGetValue(b.FlightNumber, out var f);
            return Map(b, f);
        }).ToList();
    }

    private static BookingResponse Map(FlightBookingEntity b, FlightCatalogEntity? f) => new()
    {
        Id           = b.Id,
        FlightNumber = b.FlightNumber,
        Route        = f?.Route ?? b.FlightNumber,
        Origin       = f?.Origin ?? "",
        Destination  = f?.Destination ?? "",
        TravelDate   = b.TravelDate.ToString("yyyy-MM-dd"),
        BookedAtUtc  = b.BookedAtUtc.ToString("o"),
        UserEmail    = b.User?.Email,
        UserFullName = b.User is not null ? $"{b.User.FirstName} {b.User.LastName}".Trim() : null
    };
}
