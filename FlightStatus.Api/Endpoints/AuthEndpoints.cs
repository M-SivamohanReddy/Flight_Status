using FlightStatus.Api.Models.Auth;
using FlightStatus.Api.Services.Interfaces;

namespace FlightStatus.Api.Endpoints;

public sealed class AuthEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (RegisterRequest req, IAuthService auth) =>
        {
            var result = await auth.RegisterAsync(req);
            return result.Succeeded
                ? Results.Ok(new { message = "Registration successful. Please log in." })
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        });

        app.MapPost("/auth/login", async (LoginRequest req, IAuthService auth) =>
        {
            var response = await auth.LoginAsync(req);
            return response is null ? Results.Unauthorized() : Results.Ok(response);
        });
    }
}
