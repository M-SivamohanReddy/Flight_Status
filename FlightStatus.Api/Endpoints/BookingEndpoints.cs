using System.Security.Claims;
using FlightStatus.Api.Models;
using FlightStatus.Api.Services.Interfaces;

namespace FlightStatus.Api.Endpoints;

public sealed class BookingEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/bookings", async (
            BookingRequest req,
            IBookingService svc,
            ClaimsPrincipal principal,
            CancellationToken ct) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();
            var booking = await svc.BookAsync(userId, req, ct);
            return Results.Created($"/bookings/{booking.Id}", booking);
        })
        .RequireAuthorization(p => p.RequireRole("User"));

        app.MapGet("/bookings/my", async (
            IBookingService svc,
            ClaimsPrincipal principal,
            CancellationToken ct) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();
            return Results.Ok(await svc.GetMyBookingsAsync(userId, ct));
        })
        .RequireAuthorization();

        app.MapGet("/admin/bookings", async (IBookingService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAllBookingsAsync(ct)))
        .RequireAuthorization(p => p.RequireRole("Admin"));
    }
}
