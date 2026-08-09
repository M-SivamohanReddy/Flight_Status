namespace FlightStatus.Api.Endpoints;

/// <summary>
/// Marker interface for Minimal API endpoint groups.
/// Implementations are auto-discovered from the assembly and registered at startup.
/// </summary>
public interface IEndpointDefinition
{
    void RegisterEndpoints(IEndpointRouteBuilder app);
}
