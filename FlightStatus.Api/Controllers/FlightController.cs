using System.Text.RegularExpressions;
using FlightStatus.Api.Services.Interfaces;

namespace FlightStatus.Api.Controllers;

public sealed class FlightController(ILogger<FlightController> logger) : IController
{
    private static readonly Regex FlightNumberPattern =
        new(@"^[A-Za-z]{2,3}\d{1,4}$", RegexOptions.Compiled);

    public void RegisterRoutes(IEndpointRouteBuilder app)
    {
        // public -- catalog needed by the MCP chatbot without credentials
        app.MapGet(Routes.Flights.Catalog, async (IFlightCatalogService catalog, CancellationToken ct) =>
        {
            logger.LogInformation("Flight catalog requested");
            return Results.Ok(await catalog.GetAllAsync(ct));
        })
        .WithName("GetFlights");

        // public -- flight status available without login
        app.MapGet(Routes.Flights.Status, async (
            string? flightNumber,
            string? date,
            IFlightStatusQueryService queryService,
            CancellationToken ct) =>
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

            logger.LogInformation("Status query: {FlightNumber} on {Date}", flightNumber, date);

            var result = await queryService.GetStatusAsync(
                flightNumber!.ToUpperInvariant(),
                DateOnly.ParseExact(date!, "yyyy-MM-dd"),
                ct);

            return Results.Ok(result);
        })
        .WithName("GetFlightStatus");
    }
}
