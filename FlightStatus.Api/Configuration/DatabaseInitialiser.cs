using FlightStatus.Api.Data;
using FlightStatus.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace FlightStatus.Api.Configuration;

public static class DatabaseInitialiser
{
    public static async Task InitialiseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<FlightStatusDbContext>();

        // Only drop + recreate when Identity tables are absent (first run or schema migration)
        bool identityMissing = false;
        try { _ = db.Users.Any(); }
        catch { identityMissing = true; }

        if (identityMissing)
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            DatabaseSeeder.Seed(db);
        }

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        await IdentitySeeder.SeedAsync(roleManager, userManager);

        // Idempotent — inserts any new catalog/provider rows added since last boot
        DatabaseSeeder.SeedDelta(db);
    }
}
