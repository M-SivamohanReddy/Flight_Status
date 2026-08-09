using Microsoft.AspNetCore.Identity;

namespace FlightStatus.Api.Data.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}
