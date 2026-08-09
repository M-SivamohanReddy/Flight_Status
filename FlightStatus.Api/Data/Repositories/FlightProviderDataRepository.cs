using FlightStatus.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightStatus.Api.Data.Repositories;

// IDbContextFactory creates a fresh DbContext per call, safe for concurrent use
public sealed class FlightProviderDataRepository(IDbContextFactory<FlightStatusDbContext> factory) : IFlightProviderDataRepository
{
    public async Task<FlightProviderDataEntity?> GetByFlightAndProviderAsync(
        string flightNumber,
        string providerName,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.FlightProviderData
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.FlightNumber == flightNumber && e.ProviderName == providerName,
                ct);
    }
}

