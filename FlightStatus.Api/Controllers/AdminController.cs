using FlightStatus.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightStatus.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(
    IBookingService bookingService,
    ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet("bookings")]
    public async Task<IActionResult> GetAllBookings(CancellationToken ct)
    {
        logger.LogInformation("Admin: all bookings requested");
        return Ok(await bookingService.GetAllBookingsAsync(ct));
    }
}
