namespace FlightStatus.Api.Configuration;

public static class PipelineConfiguration
{
    public static WebApplication UseFlightStatusPipeline(this WebApplication app)
    {
        // GlobalExceptionHandler is the single reusable try-catch for all controllers
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        // Discovers all [ApiController] classes and maps their [Http*] attribute routes
        app.MapControllers();

        return app;
    }
}
