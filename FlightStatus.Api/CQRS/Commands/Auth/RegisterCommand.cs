using FlightStatus.Api.Models.Auth;
using FlightStatus.Api.Services.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace FlightStatus.Api.CQRS.Commands.Auth;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<IdentityResult>;

public sealed class RegisterCommandHandler(IAuthService authService)
    : IRequestHandler<RegisterCommand, IdentityResult>
{
    public Task<IdentityResult> Handle(RegisterCommand request, CancellationToken ct)
        => authService.RegisterAsync(new RegisterRequest
        {
            FirstName = request.FirstName,
            LastName  = request.LastName,
            Email     = request.Email,
            Password  = request.Password
        });
}
