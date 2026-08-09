namespace FlightStatus.Api.Controllers;

/// <summary>
/// Implemented by each controller class; auto-discovered at startup and
/// registered via UseControllers() — no manual wiring in Program.cs.
/// </summary>
public interface IController
{
    void RegisterRoutes(IEndpointRouteBuilder app);
}
