using FlightStatus.Api.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace FlightStatus.Api.Services.Interfaces;

public interface IAuthService
{
    Task<IdentityResult> RegisterAsync(RegisterRequest request);
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
