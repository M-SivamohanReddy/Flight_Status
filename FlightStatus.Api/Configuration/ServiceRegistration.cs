using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightStatus.Api.Configuration;
using FlightStatus.Api.Data;
using FlightStatus.Api.Data.Entities;
using FlightStatus.Api.Data.Repositories;
using FlightStatus.Api.Endpoints;
using FlightStatus.Api.Providers;
using FlightStatus.Api.Services;
using FlightStatus.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FlightStatus.Api.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddFlightStatusServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        });

        services.AddDatabase(config);
        services.AddIdentityAndAuth(config);
        services.AddRepositories();
        services.AddApplicationServices();
        services.AddEndpointDefinitions();

        services.AddMemoryCache();
        services.AddOpenApi();
        services.AddCors(opts =>
            opts.AddDefaultPolicy(p =>
                p.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                 .AllowAnyMethod()
                 .AllowAnyHeader()));

        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("FlightStatusDb")!;

        // Scoped DbContext for Identity and BookingRepository
        services.AddDbContext<FlightStatusDbContext>(opts => opts.UseSqlServer(connectionString));

        // Singleton factory avoids the captive dependency between Singleton repos and Scoped DbContextOptions
        services.AddSingleton<IDbContextFactory<FlightStatusDbContext>>(_ =>
            new DirectDbContextFactory(
                new DbContextOptionsBuilder<FlightStatusDbContext>()
                    .UseSqlServer(connectionString)
                    .Options));
    }

    private static void AddIdentityAndAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
        {
            opts.Password.RequireDigit            = true;
            opts.Password.RequiredLength          = 6;
            opts.Password.RequireNonAlphanumeric  = false;
            opts.Password.RequireUppercase        = true;
            opts.Password.RequireLowercase        = false;
        })
        .AddEntityFrameworkStores<FlightStatusDbContext>()
        .AddDefaultTokenProviders();

        var jwtSecret = config["JwtSettings:Secret"]!;
        services.AddAuthentication(opts =>
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
                ValidIssuer              = config["JwtSettings:Issuer"],
                ValidateAudience         = true,
                ValidAudience            = config["JwtSettings:Audience"],
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            };
        });

        services.AddAuthorization();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        // Singleton — backed by the Singleton IDbContextFactory (concurrent-safe)
        services.AddSingleton<IFlightCatalogRepository,      FlightCatalogRepository>();
        services.AddSingleton<IFlightProviderDataRepository, FlightProviderDataRepository>();
        // Scoped — uses the Scoped DbContext; no concurrent access within a single request
        services.AddScoped<IBookingRepository, BookingRepository>();
    }

    private static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IFlightStatusProvider, AeroTrackStubProvider>();
        services.AddScoped<IFlightStatusProvider, QuickFlightStubProvider>();
        // Registered against their interfaces so endpoint handlers inject abstractions, not concretions
        services.AddScoped<IFlightCatalogService,      FlightCatalogService>();
        services.AddScoped<IFlightStatusQueryService,  FlightStatusQueryService>();
        services.AddScoped<IAuthService,               AuthService>();
        services.AddScoped<IBookingService,            BookingService>();
    }

    // Auto-discovers all IEndpointDefinition implementations in this assembly
    private static void AddEndpointDefinitions(this IServiceCollection services)
    {
        var definitions = typeof(IEndpointDefinition).Assembly
            .ExportedTypes
            .Where(t => typeof(IEndpointDefinition).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .Select(Activator.CreateInstance)
            .Cast<IEndpointDefinition>()
            .ToList();

        services.AddSingleton<IReadOnlyCollection<IEndpointDefinition>>(definitions);
    }
}
