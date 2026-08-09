using FlightStatus.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFlightStatusServices(builder.Configuration);

var app = builder.Build();
await app.Services.InitialiseAsync();

app.UseFlightStatusPipeline();

await app.RunAsync();
