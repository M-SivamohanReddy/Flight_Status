using FlightStatus.Api.Configuration;
using FlightStatus.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFlightStatusServices(builder.Configuration);

var app = builder.Build();
await app.Services.InitialiseAsync();

app.UseFlightStatusPipeline()
   .MapAuthEndpoints()
   .MapFlightEndpoints()
   .MapBookingEndpoints();

await app.RunAsync();
