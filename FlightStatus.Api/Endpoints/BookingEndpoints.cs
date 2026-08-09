using System.Security.Claims;
using FlightStatus.Api.Models;
using FlightStatus.Api.Services;

namespace FlightStatus.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/bookings", async (
            BookingRequest req,
            BookingService svc,
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
            BookingService svc,
            ClaimsPrincipal principal,
            CancellationToken ct) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();
            return Results.Ok(await svc.GetMyBookingsAsync(userId, ct));
        })
        .RequireAuthorization();

        app.MapGet("/admin/bookings", async (BookingService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAllBookingsAsync(ct)))
        .RequireAuthorization(p => p.RequireRole("Admin"));

        return app;
    }
}
