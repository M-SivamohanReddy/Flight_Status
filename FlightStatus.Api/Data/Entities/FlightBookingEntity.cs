namespace FlightStatus.Api.Data.Entities;

public sealed class FlightBookingEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public DateOnly TravelDate { get; set; }
    public DateTimeOffset BookedAtUtc { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
