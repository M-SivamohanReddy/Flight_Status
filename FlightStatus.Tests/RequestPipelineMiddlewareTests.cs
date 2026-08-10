using FlightStatus.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlightStatus.Tests;

public class RequestPipelineMiddlewareTests
{
    private readonly RequestPipelineMiddleware _sut =
        new(NullLogger<RequestPipelineMiddleware>.Instance);

    // ── Context factory ──────────────────────────────────────────────────────

    private static DefaultHttpContext MakeContext(
        string  method       = "GET",
        string  path         = "/test",
        bool    hasEndpoint  = true,
        bool    requiresAuth = false,
        string? authHeader   = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Request.Path   = new PathString(path);
        ctx.Response.Body  = new MemoryStream();

        if (authHeader is not null)
            ctx.Request.Headers.Authorization = authHeader;

        if (hasEndpoint)
        {
            var metadata = requiresAuth
                ? new EndpointMetadataCollection(new AuthorizeAttribute())
                : EndpointMetadataCollection.Empty;
            ctx.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "test"));
        }

        return ctx;
    }

    private static async Task<(int Status, string Body)> ReadAsync(DefaultHttpContext ctx)
    {
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        return (ctx.Response.StatusCode, body);
    }

    // next delegate that simply returns without writing anything
    private static readonly RequestDelegate PassThrough = _ => Task.CompletedTask;

    // ── Endpoint existence ────────────────────────────────────────────────────

    [Fact]
    public async Task Returns404_WhenNoEndpointMatched()
    {
        var ctx = MakeContext(hasEndpoint: false);

        await _sut.InvokeAsync(ctx, PassThrough);

        var (status, body) = await ReadAsync(ctx);
        Assert.Equal(404, status);
        Assert.Contains("No route matched", body);
        Assert.Contains("/test", body);
    }

    [Fact]
    public async Task DoesNotCallNext_WhenNoEndpointMatched()
    {
        var ctx     = MakeContext(hasEndpoint: false);
        var reached = false;

        await _sut.InvokeAsync(ctx, _ => { reached = true; return Task.CompletedTask; });

        Assert.False(reached);
    }

    // ── JWT structural validation ─────────────────────────────────────────────

    [Fact]
    public async Task Returns401_WhenProtectedEndpoint_AndNoAuthHeader()
    {
        var ctx = MakeContext(requiresAuth: true);

        await _sut.InvokeAsync(ctx, PassThrough);

        var (status, body) = await ReadAsync(ctx);
        Assert.Equal(401, status);
        Assert.Contains("Authorization header is missing", body);
    }

    [Fact]
    public async Task Returns401_WhenProtectedEndpoint_AndNonBearerScheme()
    {
        var ctx = MakeContext(requiresAuth: true, authHeader: "Basic dXNlcjpwYXNz");

        await _sut.InvokeAsync(ctx, PassThrough);

        var (status, body) = await ReadAsync(ctx);
        Assert.Equal(401, status);
        Assert.Contains("Bearer scheme", body);
    }

    [Fact]
    public async Task Returns401_WhenProtectedEndpoint_AndEmptyBearerValue()
    {
        var ctx = MakeContext(requiresAuth: true, authHeader: "Bearer ");

        await _sut.InvokeAsync(ctx, PassThrough);

        var (status, body) = await ReadAsync(ctx);
        Assert.Equal(401, status);
        Assert.Contains("empty", body);
    }

    [Fact]
    public async Task Returns401_WhenProtectedEndpoint_AndMalformedJwt_OneSegment()
    {
        var ctx = MakeContext(requiresAuth: true, authHeader: "Bearer notajwt");

        await _sut.InvokeAsync(ctx, PassThrough);

        var (status, body) = await ReadAsync(ctx);
        Assert.Equal(401, status);
        Assert.Contains("malformed", body);
    }

    [Fact]
    public async Task Returns401_WhenProtectedEndpoint_AndMalformedJwt_TwoSegments()
    {
        var ctx = MakeContext(requiresAuth: true, authHeader: "Bearer header.payload");

        await _sut.InvokeAsync(ctx, PassThrough);

        var (status, body) = await ReadAsync(ctx);
        Assert.Equal(401, status);
        Assert.Contains("malformed", body);
    }

    // ── Pass-through cases ────────────────────────────────────────────────────

    [Fact]
    public async Task CallsNext_WhenProtectedEndpoint_AndWellFormedJwt()
    {
        var ctx     = MakeContext(requiresAuth: true, authHeader: "Bearer header.payload.sig");
        var reached = false;

        await _sut.InvokeAsync(ctx, _ => { reached = true; return Task.CompletedTask; });

        Assert.True(reached);
    }

    [Fact]
    public async Task CallsNext_WhenAnonymousEndpoint_AndNoToken()
    {
        var ctx     = MakeContext(requiresAuth: false);
        var reached = false;

        await _sut.InvokeAsync(ctx, _ => { reached = true; return Task.CompletedTask; });

        Assert.True(reached);
    }

    [Fact]
    public async Task CallsNext_WhenAnonymousEndpoint_EvenWithMalformedToken()
    {
        // JWT guard only fires for [Authorize] endpoints
        var ctx     = MakeContext(requiresAuth: false, authHeader: "Bearer bad");
        var reached = false;

        await _sut.InvokeAsync(ctx, _ => { reached = true; return Task.CompletedTask; });

        Assert.True(reached);
    }

    // ── Exception → status code mapping ──────────────────────────────────────

    [Theory]
    [InlineData(typeof(KeyNotFoundException),        404)]
    [InlineData(typeof(ArgumentException),           400)]
    [InlineData(typeof(UnauthorizedAccessException), 403)]
    [InlineData(typeof(OperationCanceledException),  499)]
    [InlineData(typeof(InvalidOperationException),   500)]
    public async Task MapsExceptionType_ToCorrectStatusCode(Type exType, int expectedStatus)
    {
        var ctx = MakeContext();
        RequestDelegate throwing = _ => throw (Exception)Activator.CreateInstance(exType)!;

        await _sut.InvokeAsync(ctx, throwing);

        var (status, _) = await ReadAsync(ctx);
        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public async Task ExceptionResponse_ContainsProblemDetailsBody()
    {
        var ctx = MakeContext();
        RequestDelegate throwing = _ => throw new InvalidOperationException("something broke");

        await _sut.InvokeAsync(ctx, throwing);

        var (status, body) = await ReadAsync(ctx);
        Assert.Equal(500, status);
        Assert.Contains("something broke", body);
    }

    [Fact]
    public async Task DoesNotCallNextTwice_OnExceptionPath()
    {
        var ctx   = MakeContext();
        var calls = 0;
        RequestDelegate throwing = _ =>
        {
            calls++;
            throw new Exception("boom");
        };

        await _sut.InvokeAsync(ctx, throwing);

        Assert.Equal(1, calls);
    }
}
