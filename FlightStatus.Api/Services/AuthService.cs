using FlightStatus.Api.Data.Entities;
using FlightStatus.Api.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using FlightStatus.Api.Services.Interfaces;

namespace FlightStatus.Api.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration) : IAuthService
{
    public async Task<IdentityResult> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName  = request.LastName,
            Email     = request.Email,
            UserName  = request.Email
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, "User");
        return result;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return null;

        var roles = await userManager.GetRolesAsync(user);
        var role  = roles.FirstOrDefault() ?? "User";
        var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(configuration["JwtSettings:ExpiryMinutes"] ?? "60"));

        return new LoginResponse
        {
            Token     = GenerateToken(user, role, expiry),
            Email     = user.Email!,
            FirstName = user.FirstName,
            LastName  = user.LastName,
            Role      = role,
            ExpiresAt = expiry
        };
    }

    private string GenerateToken(ApplicationUser user, string role, DateTime expiry)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier,     user.Id),
            new Claim(ClaimTypes.Role,               role),
            new Claim("firstName",                   user.FirstName),
            new Claim("lastName",                    user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer:             configuration["JwtSettings:Issuer"],
            audience:           configuration["JwtSettings:Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
