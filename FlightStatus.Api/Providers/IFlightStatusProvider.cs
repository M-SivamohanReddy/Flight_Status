using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

/// <summary>
/// Abstraction over a single flight-status data provider.
/// Returns null when the provider has no data for the requested flight/date.
/// </summary>
public interface IFlightStatusProvider
{
    string ProviderName { get; }

    Task<ProviderFlightStatus?> GetFlightStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
