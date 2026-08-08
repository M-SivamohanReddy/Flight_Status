using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FlightStatus.Api.Providers;
using FlightStatus.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});

builder.Services.AddOpenApi();
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:4200")
         .AllowAnyMethod()
         .AllowAnyHeader()));

builder.Services.AddSingleton<IFlightStatusProvider, AeroTrackStubProvider>();
builder.Services.AddSingleton<IFlightStatusProvider, QuickFlightStubProvider>();
builder.Services.AddScoped<FlightStatusQueryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseHttpsRedirection();

var flightNumberPattern = new Regex(@"^[A-Za-z]{2,3}\d{1,4}$", RegexOptions.Compiled);

app.MapGet("/flights/status", async (
    string? flightNumber,
    string? date,
    FlightStatusQueryService queryService,
    CancellationToken ct) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(flightNumber))
        errors["flightNumber"] = ["flightNumber is required."];
    else if (!flightNumberPattern.IsMatch(flightNumber))
        errors["flightNumber"] = ["flightNumber must be 2-3 letters followed by 1-4 digits (e.g., BA123)."];

    if (string.IsNullOrWhiteSpace(date))
        errors["date"] = ["date is required."];
    else if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out _))
        errors["date"] = ["date must be in yyyy-MM-dd format."];

    if (errors.Count > 0)
        return Results.Json(new { errors }, statusCode: 400);

    var parsedDate = DateOnly.ParseExact(date!, "yyyy-MM-dd");
    var result = await queryService.GetStatusAsync(flightNumber!.ToUpperInvariant(), parsedDate, ct);

    return Results.Ok(result);
})
.WithName("GetFlightStatus");

app.Run();
