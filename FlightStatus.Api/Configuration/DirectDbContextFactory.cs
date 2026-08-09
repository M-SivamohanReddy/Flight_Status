using FlightStatus.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightStatus.Api.Configuration;

/// <summary>Self-contained factory — independent of the DI-registered Scoped DbContextOptions.</summary>
public sealed class DirectDbContextFactory(DbContextOptions<FlightStatusDbContext> options)
    : IDbContextFactory<FlightStatusDbContext>
{
    public FlightStatusDbContext CreateDbContext() => new(options);

    public Task<FlightStatusDbContext> CreateDbContextAsync(CancellationToken ct = default)
        => Task.FromResult(CreateDbContext());
}
