using FlightStatus.Api.Infrastructure;

namespace FlightStatus.Api.Configuration;

public static class PipelineConfiguration
{
    public static WebApplication UseFlightStatusPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(opts =>
            {
                opts.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyRoute v1");
                opts.RoutePrefix = "swagger"; // UI at /swagger
            });
        }

        app.UseCors();

        // Explicit UseRouting so RequestPipelineMiddleware can call GetEndpoint()
        app.UseRouting();
        app.UseMiddleware<RequestPipelineMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        // Discovers all [ApiController] classes and maps their [Http*] attribute routes
        app.MapControllers();

        // MCP SSE endpoint — Claude Desktop / VS Code Agent connects here
        app.MapMcp("/mcp");

        return app;
    }
}
