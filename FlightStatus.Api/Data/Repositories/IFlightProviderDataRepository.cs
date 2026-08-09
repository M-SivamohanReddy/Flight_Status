using FlightStatus.Api.Data.Entities;

namespace FlightStatus.Api.Data.Repositories;

public interface IFlightProviderDataRepository
{
    Task<FlightProviderDataEntity?> GetByFlightAndProviderAsync(
        string flightNumber,
        string providerName,
        CancellationToken ct = default);
}
