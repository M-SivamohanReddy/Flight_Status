namespace FlightStatus.Api.Data.Entities;

public sealed class FlightCatalogEntity
{
    public int Id { get; set; }
    public string FlightNumber { get; set; } = "";
    public string Route { get; set; } = "";
    public string Origin { get; set; } = "";
    public string Destination { get; set; } = "";
}
