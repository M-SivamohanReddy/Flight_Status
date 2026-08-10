using FlightStatus.Api.CQRS.Commands.Auth;
using FlightStatus.Api.CQRS.Queries.Auth;
using FlightStatus.Api.Models.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlightStatus.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(IMediator mediator, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        logger.LogInformation("Register request for {Email}", request.Email);
        var result = await mediator.Send(new RegisterCommand(request.FirstName, request.LastName, request.Email, request.Password));
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        return Ok(new { message = "Registration successful. Please log in." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        logger.LogInformation("Login attempt for {Email}", request.Email);
        var response = await mediator.Send(new LoginQuery(request.Email, request.Password));
        if (response is null)
        {
            logger.LogWarning("Failed login for {Email}", request.Email);
            return Unauthorized();
        }
        return Ok(response);
    }
}
