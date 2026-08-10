using System.Text.RegularExpressions;
using FlightStatus.Api.CQRS.Queries.Flights;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlightStatus.Api.Controllers;

[ApiController]
[Route("flights")]
public sealed class FlightController(IMediator mediator, ILogger<FlightController> logger) : ControllerBase
{
    private static readonly Regex FlightNumberPattern =
        new(@"^[A-Za-z]{2,3}\d{1,4}$", RegexOptions.Compiled);

    // public -- catalog needed by the MCP chatbot without credentials
    [HttpGet]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
    {
        logger.LogInformation("Flight catalog requested");
        return Ok(await mediator.Send(new GetFlightCatalogQuery(), ct));
    }

    // public -- flight status available without login
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(
        [FromQuery] string? flightNumber,
        [FromQuery] string? date,
        CancellationToken ct)
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
            return BadRequest(new { errors });

        logger.LogInformation("Status query: {FlightNumber} on {Date}", flightNumber, date);
        return Ok(await mediator.Send(
            new GetFlightStatusQuery(
                flightNumber!.ToUpperInvariant(),
                DateOnly.ParseExact(date!, "yyyy-MM-dd")), ct));
    }
}
