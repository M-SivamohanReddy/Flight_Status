using FlightStatus.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightStatus.Api.Data.Repositories;

// Uses Scoped DbContext — one per HTTP request; no concurrent calls within a single request
public sealed class BookingRepository(FlightStatusDbContext db) : IBookingRepository
{
    public async Task<FlightBookingEntity> AddAsync(FlightBookingEntity booking, CancellationToken ct = default)
    {
        db.FlightBookings.Add(booking);
        await db.SaveChangesAsync(ct);
        return booking;
    }

    public async Task<IReadOnlyList<FlightBookingEntity>> GetByUserIdAsync(string userId, CancellationToken ct = default) =>
        await db.FlightBookings
            .AsNoTracking()
            .Include(b => b.User)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FlightBookingEntity>> GetAllAsync(CancellationToken ct = default) =>
        await db.FlightBookings
            .AsNoTracking()
            .Include(b => b.User)
            .OrderByDescending(b => b.BookedAtUtc)
            .ToListAsync(ct);
}
