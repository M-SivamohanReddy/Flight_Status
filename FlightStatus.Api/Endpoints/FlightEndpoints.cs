using System.Text.RegularExpressions;
using FlightStatus.Api.Services;

namespace FlightStatus.Api.Endpoints;

public static class FlightEndpoints
{
    private static readonly Regex FlightNumberPattern =
        new(@"^[A-Za-z]{2,3}\d{1,4}$", RegexOptions.Compiled);

    public static IEndpointRouteBuilder MapFlightEndpoints(this IEndpointRouteBuilder app)
    {
        // public — catalog needed by the MCP chatbot without credentials
        app.MapGet("/flights", async (FlightCatalogService catalog, CancellationToken ct) =>
            Results.Ok(await catalog.GetAllAsync(ct)))
            .WithName("GetFlights");

        // public — flight status is available without login
        app.MapGet("/flights/status", async (
            string? flightNumber, string? date,
            FlightStatusQueryService queryService, CancellationToken ct) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(flightNumber))
                errors["flightNumber"] = ["flightNumber is required."];
            else if (!FlightNumberPattern.IsMatch(flightNumber))
                errors["flightNumber"] = ["flightNumber must be 2-3 letters followed by 1-4 digits."];

            if (string.IsNullOrWhiteSpace(date))
                errors["date"] = ["date is required."];
            else if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out _))
                errors["date"] = ["date must be yyyy-MM-dd."];

            if (errors.Count > 0)
                return Results.Json(new { errors }, statusCode: 400);

            var result = await queryService.GetStatusAsync(
                flightNumber!.ToUpperInvariant(),
                DateOnly.ParseExact(date!, "yyyy-MM-dd"),
                ct);

            return Results.Ok(result);
        })
        .WithName("GetFlightStatus");

        return app;
    }
}
