using FlightStatus.Api.Models;
using FlightStatus.Api.Providers;
using Microsoft.Extensions.Logging;

namespace FlightStatus.Api.Services;

/// <summary>
/// Queries all registered providers concurrently, normalises their responses,
/// and applies the merge rules to return a single FlightStatusResult.
/// </summary>
public sealed class FlightStatusQueryService
{
    private readonly IEnumerable<IFlightStatusProvider> _providers;
    private readonly ILogger<FlightStatusQueryService> _logger;

    public FlightStatusQueryService(
        IEnumerable<IFlightStatusProvider> providers,
        ILogger<FlightStatusQueryService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<FlightStatusResult> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var tasks = _providers.Select(p => FetchSafeAsync(p, flightNumber, date, cancellationToken));
        var results = await Task.WhenAll(tasks);
        var valid = results.Where(r => r is not null).Select(r => r!).ToList();

        return valid.Count switch
        {
            0 => BuildUnknown(flightNumber, date),
            1 => BuildResult(valid[0], date),
            _ => BuildResult(SelectWinner(valid), date)
        };
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
