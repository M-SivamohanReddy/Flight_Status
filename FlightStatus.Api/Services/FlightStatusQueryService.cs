using FlightStatus.Api.Models;
using FlightStatus.Api.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FlightStatus.Api.Services;

public sealed class FlightStatusQueryService
{
    private readonly IEnumerable<IFlightStatusProvider> _providers;
    private readonly ILogger<FlightStatusQueryService> _logger;
    private readonly IMemoryCache _cache;

    public FlightStatusQueryService(
        IEnumerable<IFlightStatusProvider> providers,
        ILogger<FlightStatusQueryService> logger,
        IMemoryCache cache)
    {
        _providers = providers;
        _logger    = logger;
        _cache     = cache;
    }

    public async Task<FlightStatusResult> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        // Cache per flight+date for 60 s to avoid redundant provider round-trips
        var cacheKey = $"fstatus:{flightNumber}:{date:yyyyMMdd}";
        if (_cache.TryGetValue(cacheKey, out FlightStatusResult? hit)) return hit!;

        var tasks   = _providers.Select(p => FetchSafeAsync(p, flightNumber, date, cancellationToken));
        var results = await Task.WhenAll(tasks);
        var valid   = results.Where(r => r is not null).Select(r => r!).ToList();

        var result = valid.Count switch
        {
            0 => BuildUnknown(flightNumber, date),
            1 => BuildResult(valid[0], date),
            _ => BuildResult(SelectWinner(valid), date)
        };

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
            SlidingExpiration               = TimeSpan.FromSeconds(30)
        });
        return result;
    }

    private async Task<ProviderFlightStatus?> FetchSafeAsync(
        IFlightStatusProvider provider,
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.GetFlightStatusAsync(flightNumber, date, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider {Provider} threw an exception for flight {FlightNumber}; treating as no-data.",
                provider.ProviderName, flightNumber);
            return null;
        }
    }

    private static ProviderFlightStatus SelectWinner(List<ProviderFlightStatus> results)
    {
        // Prefer later LastUpdatedUtc; tie-break in favour of AeroTrack (more detail)
        return results
            .OrderByDescending(r => r.LastUpdatedUtc)
            .ThenBy(r => r.ProviderName == "AeroTrack" ? 0 : 1)
            .First();
    }

    private static FlightStatusResult BuildResult(ProviderFlightStatus source, DateOnly date)
    {
        var status = StatusNormaliser.Normalise(source);
        return new FlightStatusResult
        {
            FlightNumber = source.FlightNumber,
            Date = date,
            Status = status,
            ScheduledDeparture = source.ScheduledDeparture,
            ActualDeparture = source.ActualDeparture,
            ScheduledArrival = source.ScheduledArrival,
            ActualArrival = source.ActualArrival,
            Terminal = source.Terminal,
            Gate = source.Gate,
            DelayReason = source.DelayReason,
            LastUpdatedUtc = source.LastUpdatedUtc,
            SourceProvider = source.ProviderName
        };
    }

    private static FlightStatusResult BuildUnknown(string flightNumber, DateOnly date) =>
        new()
        {
            FlightNumber = flightNumber,
            Date = date,
            Status = Models.FlightStatus.Unknown,
            LastUpdatedUtc = default,
            SourceProvider = "None",
            Message = "No flight data returned by either provider."
        };
}
