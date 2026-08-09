using FlightStatus.Api.Controllers;

namespace FlightStatus.Api.Configuration;

public static class PipelineConfiguration
{
    public static WebApplication UseFlightStatusPipeline(this WebApplication app)
    {
        // GlobalExceptionHandler is the single reusable try-catch for all endpoints
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseControllers();

        return app;
    }

    // Resolves all IController instances registered in DI and calls RegisterRoutes on each
    private static void UseControllers(this WebApplication app)
    {
        var controllers = app.Services.GetServices<IController>();
        foreach (var controller in controllers)
            controller.RegisterRoutes(app);
    }
}
