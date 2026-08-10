using FlightStatus.Api.CQRS.Queries.Bookings;
using FlightStatus.Api.Services.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightStatus.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(IMediator mediator, ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet("bookings")]
    public async Task<IActionResult> GetAllBookings(CancellationToken ct)
    {
        logger.LogInformation("Admin: all bookings requested");
        return Ok(await mediator.Send(new GetAllBookingsQuery(), ct));
    }
}
