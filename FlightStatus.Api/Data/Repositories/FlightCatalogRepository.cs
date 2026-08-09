using FlightStatus.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightStatus.Api.Data.Repositories;

// IDbContextFactory creates a fresh DbContext per call, safe for concurrent use
public sealed class FlightCatalogRepository(IDbContextFactory<FlightStatusDbContext> factory) : IFlightCatalogRepository
{
    public async Task<IReadOnlyList<FlightCatalogEntity>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.FlightCatalog
            .AsNoTracking()
            .OrderBy(e => e.FlightNumber)
            .ToListAsync(ct);
    }

    public async Task<FlightCatalogEntity?> GetByFlightNumberAsync(string flightNumber, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.FlightCatalog
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.FlightNumber == flightNumber.ToUpperInvariant(), ct);
    }
}

