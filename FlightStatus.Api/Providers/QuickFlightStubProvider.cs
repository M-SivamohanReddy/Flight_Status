using FlightStatus.Api.Data.Entities;
using FlightStatus.Api.Data.Repositories;
using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

public sealed class QuickFlightStubProvider(IFlightProviderDataRepository repository) : IFlightStatusProvider
{
    public string ProviderName => "QuickFlight";

    public async Task<ProviderFlightStatus?> GetFlightStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByFlightAndProviderAsync(flightNumber, "QuickFlight", cancellationToken);
        return entity is null ? null : Map(entity);
    }

    private static ProviderFlightStatus Map(FlightProviderDataEntity e) => new()
    {
        FlightNumber       = e.FlightNumber,
        ProviderName       = e.ProviderName,
        RawStatus          = e.RawStatus,
        ScheduledDeparture = e.ScheduledDeparture,
        ActualDeparture    = e.ActualDeparture,
        ScheduledArrival   = e.ScheduledArrival,
        ActualArrival      = e.ActualArrival,
        Terminal           = e.Terminal,
        Gate               = e.Gate,
        DelayReason        = e.DelayReason,
        LastUpdatedUtc     = e.LastUpdatedUtc
    };
}
