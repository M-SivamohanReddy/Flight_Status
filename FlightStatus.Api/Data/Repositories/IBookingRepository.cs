using FlightStatus.Api.Data.Entities;

namespace FlightStatus.Api.Data.Repositories;

public interface IBookingRepository
{
    Task<FlightBookingEntity> AddAsync(FlightBookingEntity booking, CancellationToken ct = default);
    Task<IReadOnlyList<FlightBookingEntity>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<FlightBookingEntity>> GetAllAsync(CancellationToken ct = default);
}
