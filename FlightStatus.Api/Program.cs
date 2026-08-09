using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FlightStatus.Api.Data;
using FlightStatus.Api.Data.Entities;
using FlightStatus.Api.Data.Repositories;
using FlightStatus.Api.Models;
using FlightStatus.Api.Models.Auth;
using FlightStatus.Api.Providers;
using FlightStatus.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});

var connectionString = builder.Configuration.GetConnectionString("FlightStatusDb")!;

// Scoped DbContext for Identity and BookingRepository
builder.Services.AddDbContext<FlightStatusDbContext>(opts => opts.UseSqlServer(connectionString));

// Singleton factory with self-contained options � avoids captive dependency with scoped DbContextOptions
builder.Services.AddSingleton<IDbContextFactory<FlightStatusDbContext>>(_ =>
    new DirectDbContextFactory(
        new DbContextOptionsBuilder<FlightStatusDbContext>()
            .UseSqlServer(connectionString)
            .Options));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
{
    opts.Password.RequireDigit         = true;
    opts.Password.RequiredLength       = 6;
    opts.Password.RequireNonAlphanumeric = false;
    opts.Password.RequireUppercase     = true;
    opts.Password.RequireLowercase     = false;
})
.AddEntityFrameworkStores<FlightStatusDbContext>()
.AddDefaultTokenProviders();

var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer           = true,
        ValidIssuer              = builder.Configuration["JwtSettings:Issuer"],
        ValidateAudience         = true,
        ValidAudience            = builder.Configuration["JwtSettings:Audience"],
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache(); // cache flight-status results for 60 s per flight+date

builder.Services.AddOpenApi();
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
         .AllowAnyMethod().AllowAnyHeader()));

// Singleton repositories � use the Singleton factory
builder.Services.AddSingleton<IFlightCatalogRepository,      FlightCatalogRepository>();
builder.Services.AddSingleton<IFlightProviderDataRepository, FlightProviderDataRepository>();
// Scoped repository � uses the Scoped DbContext
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

builder.Services.AddScoped<IFlightStatusProvider, AeroTrackStubProvider>();
builder.Services.AddScoped<IFlightStatusProvider, QuickFlightStubProvider>();
builder.Services.AddScoped<FlightCatalogService>();
builder.Services.AddScoped<FlightStatusQueryService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BookingService>();

var app = builder.Build();

// Recreate DB if Identity tables are missing; then seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlightStatusDbContext>();
    bool identityMissing = false;
    try { _ = db.Users.Any(); }
    catch { identityMissing = true; }

    if (identityMissing)
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        DatabaseSeeder.Seed(db);
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await IdentitySeeder.SeedAsync(roleManager, userManager);

    // Idempotent — adds any new catalog/provider rows missing from the DB
    DatabaseSeeder.SeedDelta(db);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

var flightNumberPattern = new Regex(@"^[A-Za-z]{2,3}\d{1,4}$", RegexOptions.Compiled);

// Auth
app.MapPost("/auth/register", async (RegisterRequest req, AuthService auth) =>
{
    var result = await auth.RegisterAsync(req);
    return result.Succeeded
        ? Results.Ok(new { message = "Registration successful. Please log in." })
        : Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
});

app.MapPost("/auth/login", async (LoginRequest req, AuthService auth) =>
{
    var response = await auth.LoginAsync(req);
    return response is null ? Results.Unauthorized() : Results.Ok(response);
});

// Flights (authenticated)
app.MapGet("/flights", async (FlightCatalogService catalog, CancellationToken ct) =>
    Results.Ok(await catalog.GetAllAsync(ct)))
    .WithName("GetFlights"); // public — needed by MCP chatbot without credentials

app.MapGet("/flights/status", async (
    string? flightNumber, string? date,
    FlightStatusQueryService queryService, CancellationToken ct) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(flightNumber))
        errors["flightNumber"] = ["flightNumber is required."];
    else if (!flightNumberPattern.IsMatch(flightNumber))
        errors["flightNumber"] = ["flightNumber must be 2-3 letters followed by 1-4 digits."];
    if (string.IsNullOrWhiteSpace(date))
        errors["date"] = ["date is required."];
    else if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out _))
        errors["date"] = ["date must be yyyy-MM-dd."];
    if (errors.Count > 0) return Results.Json(new { errors }, statusCode: 400);
    var result = await queryService.GetStatusAsync(
        flightNumber!.ToUpperInvariant(), DateOnly.ParseExact(date!, "yyyy-MM-dd"), ct);
    return Results.Ok(result);
})
.WithName("GetFlightStatus"); // public — flight status is available without login

// Bookings
app.MapPost("/bookings", async (
    BookingRequest req, BookingService svc, ClaimsPrincipal principal, CancellationToken ct) =>
{
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return Results.Unauthorized();
    var booking = await svc.BookAsync(userId, req, ct);
    return Results.Created($"/bookings/{booking.Id}", booking);
})
.RequireAuthorization(p => p.RequireRole("User"));

app.MapGet("/bookings/my", async (
    BookingService svc, ClaimsPrincipal principal, CancellationToken ct) =>
{
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return Results.Unauthorized();
    return Results.Ok(await svc.GetMyBookingsAsync(userId, ct));
})
.RequireAuthorization();

app.MapGet("/admin/bookings", async (BookingService svc, CancellationToken ct) =>
    Results.Ok(await svc.GetAllBookingsAsync(ct)))
.RequireAuthorization(p => p.RequireRole("Admin"));

await app.RunAsync();

// Self-contained factory � does not depend on DI-registered DbContextOptions
sealed class DirectDbContextFactory(DbContextOptions<FlightStatusDbContext> options)
    : IDbContextFactory<FlightStatusDbContext>
{
    public FlightStatusDbContext CreateDbContext() => new(options);
    public Task<FlightStatusDbContext> CreateDbContextAsync(CancellationToken ct = default)
        => Task.FromResult(CreateDbContext());
}
