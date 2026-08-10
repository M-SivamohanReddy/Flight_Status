using System.Security.Claims;
using FlightStatus.Api.CQRS.Commands.Bookings;
using FlightStatus.Api.CQRS.Queries.Bookings;
using FlightStatus.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightStatus.Api.Controllers;

[ApiController]
[Route("bookings")]
[Authorize]
public sealed class BookingController(IMediator mediator, ILogger<BookingController> logger) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> Create([FromBody] BookingRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        logger.LogInformation("Booking {FlightNumber} for user {UserId}", request.FlightNumber, userId);
        var booking = await mediator.Send(
            new CreateBookingCommand(userId, request.FlightNumber, request.TravelDate), ct);
        return Created($"/bookings/{booking.Id}", booking);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        logger.LogInformation("My bookings requested by user {UserId}", userId);
        return Ok(await mediator.Send(new GetMyBookingsQuery(userId), ct));
    }
}
