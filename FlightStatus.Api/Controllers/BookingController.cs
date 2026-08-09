using System.Security.Claims;
using FlightStatus.Api.Models;
using FlightStatus.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightStatus.Api.Controllers;

[ApiController]
[Route("bookings")]
[Authorize]
public sealed class BookingController(
    IBookingService bookingService,
    ILogger<BookingController> logger) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> Create([FromBody] BookingRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        logger.LogInformation("Booking {FlightNumber} for user {UserId}", request.FlightNumber, userId);

        var booking = await bookingService.BookAsync(userId, request, ct);
        return Created($"/bookings/{booking.Id}", booking);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        logger.LogInformation("My bookings requested by user {UserId}", userId);
        return Ok(await bookingService.GetMyBookingsAsync(userId, ct));
    }
}
