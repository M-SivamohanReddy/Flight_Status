using FlightStatus.Api.Models.Auth;
using FlightStatus.Api.Services.Interfaces;

namespace FlightStatus.Api.Controllers;

public sealed class AuthController(ILogger<AuthController> logger) : IController
{
    public void RegisterRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(Routes.Auth.Register, async (RegisterRequest req, IAuthService auth) =>
        {
            logger.LogInformation("Register request for {Email}", req.Email);

            var result = await auth.RegisterAsync(req);
            return result.Succeeded
                ? Results.Ok(new { message = "Registration successful. Please log in." })
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        });

        app.MapPost(Routes.Auth.Login, async (LoginRequest req, IAuthService auth) =>
        {
            logger.LogInformation("Login attempt for {Email}", req.Email);

            var response = await auth.LoginAsync(req);
            if (response is null)
            {
                logger.LogWarning("Failed login for {Email}", req.Email);
                return Results.Unauthorized();
            }
            return Results.Ok(response);
        });
    }
}
