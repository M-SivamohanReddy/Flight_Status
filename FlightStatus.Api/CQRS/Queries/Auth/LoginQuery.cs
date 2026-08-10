using FlightStatus.Api.Models.Auth;
using FlightStatus.Api.Services.Interfaces;
using MediatR;

namespace FlightStatus.Api.CQRS.Queries.Auth;

// Login is a query — it reads credentials and returns a token without mutating persistent state
public sealed record LoginQuery(string Email, string Password) : IRequest<LoginResponse?>;

public sealed class LoginQueryHandler(IAuthService authService)
    : IRequestHandler<LoginQuery, LoginResponse?>
{
    public Task<LoginResponse?> Handle(LoginQuery request, CancellationToken ct)
        => authService.LoginAsync(new LoginRequest { Email = request.Email, Password = request.Password });
}
