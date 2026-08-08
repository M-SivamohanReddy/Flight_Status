using FlightStatus.Api.Models;

namespace FlightStatus.Api.Services;

/// <summary>
/// Converts a provider's RawStatus string and time deltas into the unified FlightStatus enum.
/// Time delta takes precedence over the raw status label when actual times are present.
/// </summary>
public static class StatusNormaliser
{
    private const double OnTimeThresholdSeconds = 900.0; // 15 minutes inclusive

    private static readonly HashSet<string> CancelledTokens =
        new(StringComparer.OrdinalIgnoreCase) { "cancelled", "cancel", "canceled" };

    private static readonly HashSet<string> DivertedTokens =
        new(StringComparer.OrdinalIgnoreCase) { "diverted" };

    private static readonly HashSet<string> OnTimeTokens =
        new(StringComparer.OrdinalIgnoreCase) { "on_schedule", "on-time", "ontime", "on time" };

    private static readonly HashSet<string> DelayedTokens =
        new(StringComparer.OrdinalIgnoreCase) { "delayed", "delay" };

    public static Models.FlightStatus Normalise(ProviderFlightStatus provider)
    {
        var raw = provider.RawStatus?.Trim() ?? string.Empty;

        // Terminal statuses — no time-delta check needed
        if (CancelledTokens.Contains(raw)) return Models.FlightStatus.Cancelled;
        if (DivertedTokens.Contains(raw))  return Models.FlightStatus.Diverted;

        // Resolve via departure delta first, then arrival delta
        var depDelta = ComputeDeltaSeconds(provider.ScheduledDeparture, provider.ActualDeparture);
        if (depDelta.HasValue)
            return depDelta.Value <= OnTimeThresholdSeconds
                ? Models.FlightStatus.OnTime
                : Models.FlightStatus.Delayed;

        var arrDelta = ComputeDeltaSeconds(provider.ScheduledArrival, provider.ActualArrival);
        if (arrDelta.HasValue)
            return arrDelta.Value <= OnTimeThresholdSeconds
                ? Models.FlightStatus.OnTime
                : Models.FlightStatus.Delayed;

        // Fall back to raw status label
        if (OnTimeTokens.Contains(raw))  return Models.FlightStatus.OnTime;
        if (DelayedTokens.Contains(raw)) return Models.FlightStatus.Delayed;

        return Models.FlightStatus.Unknown;
    }

    private static double? ComputeDeltaSeconds(DateTimeOffset? scheduled, DateTimeOffset? actual)
    {
        if (scheduled is null || actual is null) return null;
        return (actual.Value - scheduled.Value).TotalSeconds;
    }
}
