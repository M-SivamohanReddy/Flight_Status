using FlightStatus.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace FlightStatus.Api.Data;

/// <summary>Seeds Roles table and 2 demo users. Idempotent — checks before inserting.</summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await SeedUserAsync(userManager, "admin@skyroute.com", "Admin",  "SkyRoute",   "Admin@123",  "Admin");
        await SeedUserAsync(userManager, "user@skyroute.com",  "John",   "Traveller",  "User@123",   "User");
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string firstName, string lastName, string password, string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            FirstName = firstName,
            LastName  = lastName,
            Email     = email,
            UserName  = email
        };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }
}
