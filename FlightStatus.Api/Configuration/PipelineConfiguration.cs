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

        return app;
    }
}
