using FlightStatus.Api.Models;
using FlightStatus.Api.Providers;
using FlightStatus.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlightStatus.Tests;

public class FlightStatusQueryServiceTests
{
    private static readonly DateOnly AnyDate = new(2026, 6, 15);
    private static readonly DateTimeOffset Base = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static FlightStatusQueryService Build(params IFlightStatusProvider[] providers) =>
        new(providers, NullLogger<FlightStatusQueryService>.Instance);

    private static ProviderFlightStatus MakeStatus(
        string flightNumber,
        string providerName,
        string rawStatus,
        DateTimeOffset lastUpdated,
        DateTimeOffset? schedDep = null,
        DateTimeOffset? actDep = null,
        string? terminal = null,
        string? gate = null,
        string? delayReason = null) =>
        new()
        {
            FlightNumber = flightNumber,
            ProviderName = providerName,
            RawStatus = rawStatus,
            ScheduledDeparture = schedDep ?? Base,
            ActualDeparture = actDep,
            LastUpdatedUtc = lastUpdated,
            Terminal = terminal,
            Gate = gate,
            DelayReason = delayReason
        };

    [Fact]
    public async Task Returns_Unknown_when_no_providers_respond()
    {
        var service = Build(new NullProvider("P1"), new NullProvider("P2"));
        var result = await service.GetStatusAsync("XX999", AnyDate);

        Assert.Equal(Api.Models.FlightStatus.Unknown, result.Status);
        Assert.Equal("None", result.SourceProvider);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task Returns_result_from_single_responding_provider()
    {
        var provider = new FixedProvider("AeroTrack",
            MakeStatus("AA400", "AeroTrack", "DIVERTED", Base.AddHours(2)));
        var service = Build(provider, new NullProvider("QuickFlight"));
        var result = await service.GetStatusAsync("AA400", AnyDate);

        Assert.Equal(Api.Models.FlightStatus.Diverted, result.Status);
        Assert.Equal("AeroTrack", result.SourceProvider);
    }

    [Fact]
    public async Task Prefers_provider_with_later_lastUpdatedUtc()
    {
        var aeroTrack = new FixedProvider("AeroTrack",
            MakeStatus("AA800", "AeroTrack", "on-time", new DateTimeOffset(2026, 1, 1, 7, 0, 0, TimeSpan.Zero)));
        var quickFlight = new FixedProvider("QuickFlight",
            MakeStatus("AA800", "QuickFlight", "on-time", new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero)));

        var service = Build(aeroTrack, quickFlight);
        var result = await service.GetStatusAsync("AA800", AnyDate);

        Assert.Equal("QuickFlight", result.SourceProvider);
    }

    [Fact]
    public async Task Tie_breaks_to_AeroTrack_when_timestamps_equal()
    {
        var ts = Base.AddHours(1);
        var aeroTrack = new FixedProvider("AeroTrack",
            MakeStatus("TIE1", "AeroTrack", "on-time", ts, gate: "A1"));
        var quickFlight = new FixedProvider("QuickFlight",
            MakeStatus("TIE1", "QuickFlight", "on-time", ts));

        var service = Build(aeroTrack, quickFlight);
        var result = await service.GetStatusAsync("TIE1", AnyDate);

        Assert.Equal("AeroTrack", result.SourceProvider);
        Assert.Equal("A1", result.Gate);
    }

    [Fact]
    public async Task Provider_exception_is_treated_as_no_data()
    {
        var service = Build(new ThrowingProvider("BadProvider"), new NullProvider("GoodProvider"));
        var result = await service.GetStatusAsync("XX500", AnyDate);

        Assert.Equal(Api.Models.FlightStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task Both_providers_on_time_returns_OnTime()
    {
        // AA100 scenario — both providers respond; AeroTrack has later timestamp
        var service = Build(new AeroTrackStubProvider(), new QuickFlightStubProvider());
        var result = await service.GetStatusAsync("AA100", AnyDate);

        Assert.Equal(Api.Models.FlightStatus.OnTime, result.Status);
        Assert.Equal("AeroTrack", result.SourceProvider);
        Assert.Equal("T1", result.Terminal);
    }

    [Fact]
    public async Task Both_providers_delayed_returns_Delayed_with_reason()
    {
        var service = Build(new AeroTrackStubProvider(), new QuickFlightStubProvider());
        var result = await service.GetStatusAsync("AA200", AnyDate);

        Assert.Equal(Api.Models.FlightStatus.Delayed, result.Status);
        Assert.Equal("Air Traffic Control", result.DelayReason);
    }

    [Fact]
    public async Task AA300_returns_Cancelled()
    {
        var service = Build(new AeroTrackStubProvider(), new QuickFlightStubProvider());
        var result = await service.GetStatusAsync("AA300", AnyDate);

        Assert.Equal(Api.Models.FlightStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task AA700_raw_OnSchedule_but_delayed_by_delta()
    {
        var service = Build(new AeroTrackStubProvider(), new QuickFlightStubProvider());
        var result = await service.GetStatusAsync("AA700", AnyDate);

        Assert.Equal(Api.Models.FlightStatus.Delayed, result.Status);
    }

    [Fact]
    public async Task Unknown_flight_returns_Unknown()
    {
        var service = Build(new AeroTrackStubProvider(), new QuickFlightStubProvider());
        var result = await service.GetStatusAsync("XX999", AnyDate);

        Assert.Equal(Api.Models.FlightStatus.Unknown, result.Status);
        Assert.Equal("None", result.SourceProvider);
    }

    // --- Test doubles ---

    private sealed class NullProvider(string name) : IFlightStatusProvider
    {
        public string ProviderName => name;
        public Task<ProviderFlightStatus?> GetFlightStatusAsync(string f, DateOnly d, CancellationToken ct)
            => Task.FromResult<ProviderFlightStatus?>(null);
    }

    private sealed class FixedProvider(string name, ProviderFlightStatus status) : IFlightStatusProvider
    {
        public string ProviderName => name;
        public Task<ProviderFlightStatus?> GetFlightStatusAsync(string f, DateOnly d, CancellationToken ct)
            => Task.FromResult<ProviderFlightStatus?>(status);
    }

    private sealed class ThrowingProvider(string name) : IFlightStatusProvider
    {
        public string ProviderName => name;
        public Task<ProviderFlightStatus?> GetFlightStatusAsync(string f, DateOnly d, CancellationToken ct)
            => throw new InvalidOperationException("Simulated provider failure");
    }
}
