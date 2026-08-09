using FlightStatus.Api.Endpoints;

namespace FlightStatus.Api.Configuration;

public static class PipelineConfiguration
{
    public static WebApplication UseFlightStatusPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpointDefinitions();

        return app;
    }

    // Resolves all IEndpointDefinition instances from DI and calls RegisterEndpoints on each
    private static void UseEndpointDefinitions(this WebApplication app)
    {
        var definitions = app.Services.GetRequiredService<IReadOnlyCollection<IEndpointDefinition>>();
        foreach (var definition in definitions)
            definition.RegisterEndpoints(app);
    }
}
