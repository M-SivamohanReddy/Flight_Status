namespace FlightStatus.Api.Controllers;

/// <summary>All route constants in one place — no magic strings anywhere else.</summary>
public static class Routes
{
    public static class Auth
    {
        public const string Register = "/auth/register";
        public const string Login    = "/auth/login";
    }

    public static class Flights
    {
        public const string Catalog = "/flights";
        public const string Status  = "/flights/status";
    }

    public static class Bookings
    {
        public const string Create   = "/bookings";
        public const string My       = "/bookings/my";
        public const string AdminAll = "/admin/bookings";
    }
}
