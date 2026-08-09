using System.Security.Claims;
using FlightStatus.Api.Models;
using FlightStatus.Api.Services.Interfaces;

namespace FlightStatus.Api.Controllers;

public sealed class BookingController : IController
{
    public void RegisterRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(Routes.Bookings.Create, async (
            BookingRequest req,
            IBookingService svc,
            ClaimsPrincipal principal,
            CancellationToken ct) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();
            var booking = await svc.BookAsync(userId, req, ct);
            return Results.Created($"{Routes.Bookings.Create}/{booking.Id}", booking);
        })
        .RequireAuthorization(p => p.RequireRole("User"));

        app.MapGet(Routes.Bookings.My, async (
            IBookingService svc,
            ClaimsPrincipal principal,
            CancellationToken ct) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();
            return Results.Ok(await svc.GetMyBookingsAsync(userId, ct));
        })
        .RequireAuthorization();

        app.MapGet(Routes.Bookings.AdminAll, async (IBookingService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAllBookingsAsync(ct)))
        .RequireAuthorization(p => p.RequireRole("Admin"));
    }
}
