using System.ComponentModel;
using System.Text;
using FlightStatus.Api.Services.Interfaces;
using ModelContextProtocol.Server;

namespace FlightStatus.Api.Infrastructure;

/// <summary>
/// MCP tools hosted inside the .NET API — inject services directly, no HTTP round-trip.
/// Claude Desktop / VS Code Agent connects via SSE at GET /mcp.
/// </summary>
[McpServerToolType]
public sealed class FlightMcpTools(
    IFlightCatalogService    catalogService,
    IFlightStatusQueryService queryService)
{
    [McpServerTool(Name = "list_flights")]
    [Description("List all available SkyRoute flights with their route names and IATA airport codes.")]
    public async Task<string> ListFlights(CancellationToken ct = default)
    {
        var flights = await catalogService.GetAllAsync(ct);
        var lines   = flights.Select(f => $"  * {f.FlightNumber,-7} {f.Route}  ({f.Origin} -> {f.Destination})");
        return $"SkyRoute fleet -- {flights.Count} flights available:\n\n{string.Join('\n', lines)}";
    }

    [McpServerTool(Name = "check_flight_status")]
    [Description(
        "Check the real-time status of a SkyRoute flight. " +
        "Returns: status (OnTime / Delayed / Cancelled / Diverted / Unknown), " +
        "scheduled and actual departure/arrival times, terminal, gate, delay reason, and source provider. " +
        "Date defaults to today if omitted.")]
    public async Task<string> CheckFlightStatus(
        [Description("IATA-style flight number e.g. AA100. 2-3 uppercase letters + 1-4 digits.")] string flightNumber,
        [Description("Travel date in yyyy-MM-dd format. Defaults to today.")] string? date = null,
        CancellationToken ct = default)
    {
        var parsedDate = date is not null
            ? DateOnly.ParseExact(date, "yyyy-MM-dd")
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var s = await queryService.GetStatusAsync(flightNumber.ToUpperInvariant(), parsedDate, ct);

        static string FmtTime(DateTimeOffset? iso) =>
            iso.HasValue ? iso.Value.ToString("HH:mm") + " UTC" : "--";

        var sb = new StringBuilder();
        sb.AppendLine($"Flight {s.FlightNumber}  |  {s.Date}");
        sb.AppendLine($"Status   : {s.Status}");
        sb.AppendLine($"\nDeparture");
        sb.AppendLine($"  Scheduled : {FmtTime(s.ScheduledDeparture)}");
        sb.AppendLine($"  Actual    : {FmtTime(s.ActualDeparture)}");
        sb.AppendLine($"\nArrival");
        sb.AppendLine($"  Scheduled : {FmtTime(s.ScheduledArrival)}");
        sb.AppendLine($"  Actual    : {FmtTime(s.ActualArrival)}");
        if (s.Terminal is not null)    sb.AppendLine($"\nTerminal  : {s.Terminal}");
        if (s.Gate is not null)        sb.AppendLine($"Gate      : {s.Gate}");
        if (s.DelayReason is not null) sb.AppendLine($"Delay     : {s.DelayReason}");
        if (s.Message is not null)     sb.AppendLine($"Note      : {s.Message}");
        sb.AppendLine($"\nData from : {s.SourceProvider}  (updated {FmtTime(s.LastUpdatedUtc)})");
        return sb.ToString();
    }

    [McpServerTool(Name = "search_by_route")]
    [Description(
        "Search SkyRoute flights by route name, origin IATA code, destination IATA code, or flight number. " +
        "Returns matching flights. Use check_flight_status to get the live status of any result.")]
    public async Task<string> SearchByRoute(
        [Description("Search term — route name, IATA code, or flight number prefix. Examples: 'London', 'JFK', 'AA1'.")] string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Please provide a search term (e.g. \"London\", \"JFK\", \"AA1\").";

        var q       = query.Trim().ToLowerInvariant();
        var all     = await catalogService.GetAllAsync(ct);
        var matches = all.Where(f =>
            f.Route.ToLower().Contains(q) ||
            f.Origin.ToLower().Contains(q) ||
            f.Destination.ToLower().Contains(q) ||
            f.FlightNumber.ToLower().Contains(q)).ToList();

        if (matches.Count == 0)
            return $"No flights found matching \"{query}\". Try an airport code (JFK, LHR) or city name (London, Tokyo).";

        var lines = matches.Select(f => $"  * {f.FlightNumber,-7} {f.Route}  ({f.Origin} -> {f.Destination})");
        return $"Found {matches.Count} flight{(matches.Count == 1 ? "" : "s")} matching \"{query}\":\n\n" +
               $"{string.Join('\n', lines)}\n\n" +
               $"Use check_flight_status with any flight number above to see its live status.";
    }
}
