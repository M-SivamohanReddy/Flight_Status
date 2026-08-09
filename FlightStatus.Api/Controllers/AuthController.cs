using FlightStatus.Api.Models.Auth;
using FlightStatus.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FlightStatus.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        logger.LogInformation("Register request for {Email}", request.Email);

        var result = await authService.RegisterAsync(request);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Registration successful. Please log in." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        logger.LogInformation("Login attempt for {Email}", request.Email);

        var response = await authService.LoginAsync(request);
        if (response is null)
        {
            logger.LogWarning("Failed login for {Email}", request.Email);
            return Unauthorized();
        }
        return Ok(response);
    }
}
