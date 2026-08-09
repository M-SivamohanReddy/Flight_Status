using FlightStatus.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightStatus.Api.Data;

public static class DatabaseSeeder
{
    private static readonly DateTimeOffset Base = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Adds any flight catalog entries and provider rows that are missing.
    /// Safe to call on every startup — only inserts what isn't already there.
    /// </summary>
    public static void SeedDelta(FlightStatusDbContext db)
    {
        var existingCatalog = db.FlightCatalog.Select(f => f.FlightNumber).ToHashSet();
        var catalogToAdd = DeltaCatalog().Where(e => !existingCatalog.Contains(e.FlightNumber)).ToList();
        if (catalogToAdd.Count > 0) { db.FlightCatalog.AddRange(catalogToAdd); db.SaveChanges(); }

        var existingKeys = db.FlightProviderData
            .Select(p => p.FlightNumber + "|" + p.ProviderName).ToHashSet();
        var providerToAdd = DeltaProviderData()
            .Where(e => !existingKeys.Contains(e.FlightNumber + "|" + e.ProviderName)).ToList();
        if (providerToAdd.Count > 0) { db.FlightProviderData.AddRange(providerToAdd); db.SaveChanges(); }
    }

    private static IEnumerable<FlightCatalogEntity> DeltaCatalog() =>
    [
        new() { FlightNumber = "AA1100", Route = "New York → Tokyo",      Origin = "JFK", Destination = "NRT" },
        new() { FlightNumber = "AA1200", Route = "London → Singapore",    Origin = "LHR", Destination = "SIN" },
        new() { FlightNumber = "AA1300", Route = "Paris → Sydney",        Origin = "CDG", Destination = "SYD" },
        new() { FlightNumber = "AA1400", Route = "Dubai → Los Angeles",   Origin = "DXB", Destination = "LAX" },
    ];

    private static IEnumerable<FlightProviderDataEntity> DeltaProviderData() =>
    [
        // AA1100 — early arrival (Δ = -10 min → OnTime)
        new() { FlightNumber="AA1100", ProviderName="AeroTrack",   RawStatus="ON_SCHEDULE",
                ScheduledDeparture=Base, ActualDeparture=Base.AddMinutes(-3),
                ScheduledArrival=Base.AddHours(14), ActualArrival=Base.AddHours(14).AddMinutes(-10),
                Terminal="T1", Gate="G2", DelayReason=null, LastUpdatedUtc=Base.AddHours(12) },
        new() { FlightNumber="AA1100", ProviderName="QuickFlight", RawStatus="on-time",
                ScheduledDeparture=Base, ScheduledArrival=Base.AddHours(14),
                LastUpdatedUtc=Base.AddHours(11) },
        // AA1200 — exactly 15-min boundary (Δ = 900 s → OnTime inclusive)
        new() { FlightNumber="AA1200", ProviderName="AeroTrack",   RawStatus="ON_SCHEDULE",
                ScheduledDeparture=Base, ActualDeparture=Base.AddMinutes(15),
                ScheduledArrival=Base.AddHours(13), ActualArrival=Base.AddHours(13).AddMinutes(15),
                Terminal="T2", Gate="H4", DelayReason=null, LastUpdatedUtc=Base.AddMinutes(20) },
        new() { FlightNumber="AA1200", ProviderName="QuickFlight", RawStatus="on-time",
                ScheduledDeparture=Base, ScheduledArrival=Base.AddHours(13),
                LastUpdatedUtc=Base.AddMinutes(15) },
        // AA1300 — 16-min delay; raw label says ON_SCHEDULE but delta overrides to Delayed
        new() { FlightNumber="AA1300", ProviderName="AeroTrack",   RawStatus="ON_SCHEDULE",
                ScheduledDeparture=Base, ActualDeparture=Base.AddMinutes(16),
                ScheduledArrival=Base.AddHours(22), ActualArrival=Base.AddHours(22).AddMinutes(16),
                Terminal="T3", Gate="J9", DelayReason="Late inbound aircraft",
                LastUpdatedUtc=Base.AddMinutes(20) },
        new() { FlightNumber="AA1300", ProviderName="QuickFlight", RawStatus="on-time",
                ScheduledDeparture=Base, ScheduledArrival=Base.AddHours(22),
                LastUpdatedUtc=Base.AddMinutes(5) },
        // AA1400 — QuickFlight-only cancelled
        new() { FlightNumber="AA1400", ProviderName="QuickFlight", RawStatus="cancelled",
                ScheduledDeparture=Base, ScheduledArrival=Base.AddHours(16),
                LastUpdatedUtc=Base.AddHours(3) },
    ];

    public static void Seed(FlightStatusDbContext db)
    {
        SeedCatalog(db);
        SeedProviderData(db);
    }

    private static void SeedCatalog(FlightStatusDbContext db)
    {
        if (db.FlightCatalog.Any()) return;

        db.FlightCatalog.AddRange(
            new FlightCatalogEntity { FlightNumber = "AA100",  Route = "New York → London",         Origin = "JFK", Destination = "LHR" },
            new FlightCatalogEntity { FlightNumber = "AA200",  Route = "Los Angeles → Paris",       Origin = "LAX", Destination = "CDG" },
            new FlightCatalogEntity { FlightNumber = "AA300",  Route = "Chicago → Frankfurt",       Origin = "ORD", Destination = "FRA" },
            new FlightCatalogEntity { FlightNumber = "AA400",  Route = "Miami → Amsterdam",         Origin = "MIA", Destination = "AMS" },
            new FlightCatalogEntity { FlightNumber = "AA500",  Route = "Dallas → Dubai",            Origin = "DFW", Destination = "DXB" },
            new FlightCatalogEntity { FlightNumber = "AA600",  Route = "Seattle → Tokyo",           Origin = "SEA", Destination = "NRT" },
            new FlightCatalogEntity { FlightNumber = "AA700",  Route = "Boston → Barcelona",        Origin = "BOS", Destination = "BCN" },
            new FlightCatalogEntity { FlightNumber = "AA800",  Route = "San Francisco → Singapore", Origin = "SFO", Destination = "SIN" },
            new FlightCatalogEntity { FlightNumber = "AA900",  Route = "Denver → Zurich",           Origin = "DEN", Destination = "ZRH" },
            new FlightCatalogEntity { FlightNumber = "AA1000", Route = "Atlanta → Sydney",          Origin = "ATL", Destination = "SYD" },
            // Additional edge-case scenarios
            new FlightCatalogEntity { FlightNumber = "AA1100", Route = "New York → Tokyo",          Origin = "JFK", Destination = "NRT" },
            new FlightCatalogEntity { FlightNumber = "AA1200", Route = "London → Singapore",        Origin = "LHR", Destination = "SIN" },
            new FlightCatalogEntity { FlightNumber = "AA1300", Route = "Paris → Sydney",            Origin = "CDG", Destination = "SYD" },
            new FlightCatalogEntity { FlightNumber = "AA1400", Route = "Dubai → Los Angeles",       Origin = "DXB", Destination = "LAX" }
        );
        db.SaveChanges();
    }

    private static void SeedProviderData(FlightStatusDbContext db)
    {
        if (db.FlightProviderData.Any()) return;

        // --- AeroTrack rows (full detail: actual times, terminal, gate, delay reason) ---
        db.FlightProviderData.AddRange(
            new FlightProviderDataEntity
            {
                FlightNumber = "AA100", ProviderName = "AeroTrack", RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base,              ActualDeparture = Base.AddMinutes(5),
                ScheduledArrival   = Base.AddHours(2),  ActualArrival   = Base.AddHours(2).AddMinutes(5),
                Terminal = "T1", Gate = "A10", DelayReason = null,
                LastUpdatedUtc = Base.AddHours(1)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA200", ProviderName = "AeroTrack", RawStatus = "DELAYED",
                ScheduledDeparture = Base,              ActualDeparture = Base.AddMinutes(45),
                ScheduledArrival   = Base.AddHours(2),  ActualArrival   = Base.AddHours(2).AddMinutes(45),
                Terminal = "T2", Gate = "B5", DelayReason = "Air Traffic Control",
                LastUpdatedUtc = Base.AddMinutes(45)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA300", ProviderName = "AeroTrack", RawStatus = "CANCELLED",
                ScheduledDeparture = Base,              ActualDeparture = null,
                ScheduledArrival   = Base.AddHours(2),  ActualArrival   = null,
                Terminal = "T3", Gate = null, DelayReason = "Maintenance",
                LastUpdatedUtc = Base.AddHours(-2)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA400", ProviderName = "AeroTrack", RawStatus = "DIVERTED",
                ScheduledDeparture = Base,              ActualDeparture = Base.AddMinutes(10),
                ScheduledArrival   = Base.AddHours(2),  ActualArrival   = Base.AddHours(2).AddMinutes(10),
                Terminal = "T1", Gate = "D3", DelayReason = null,
                LastUpdatedUtc = Base.AddHours(2)
            },
            new FlightProviderDataEntity
            {
                // ON_SCHEDULE label but 20-min delta → normalises to Delayed
                FlightNumber = "AA700", ProviderName = "AeroTrack", RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base,              ActualDeparture = Base.AddMinutes(20),
                ScheduledArrival   = Base.AddHours(2),  ActualArrival   = Base.AddHours(2).AddMinutes(20),
                Terminal = "T4", Gate = "E7", DelayReason = null,
                LastUpdatedUtc = Base.AddMinutes(30)
            },
            new FlightProviderDataEntity
            {
                // AeroTrack updated earlier than QuickFlight → QuickFlight wins merge for AA800
                FlightNumber = "AA800", ProviderName = "AeroTrack", RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base,              ActualDeparture = Base.AddMinutes(5),
                ScheduledArrival   = Base.AddHours(2),  ActualArrival   = Base.AddHours(2).AddMinutes(5),
                Terminal = "T1", Gate = "F1", DelayReason = null,
                LastUpdatedUtc = new DateTimeOffset(2026, 1, 1, 7, 0, 0, TimeSpan.Zero)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA900", ProviderName = "AeroTrack", RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base,              ActualDeparture = Base.AddMinutes(8),
                ScheduledArrival   = Base.AddHours(2),  ActualArrival   = Base.AddHours(2).AddMinutes(8),
                Terminal = "T5", Gate = "C14", DelayReason = null,
                LastUpdatedUtc = Base.AddMinutes(50)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA1000", ProviderName = "AeroTrack", RawStatus = "DELAYED",
                ScheduledDeparture = Base,              ActualDeparture = Base.AddMinutes(90),
                ScheduledArrival   = Base.AddHours(2),  ActualArrival   = Base.AddHours(2).AddMinutes(90),
                Terminal = "T2", Gate = "B8", DelayReason = "Weather",
                LastUpdatedUtc = Base.AddHours(1)
            }
        );

        // --- QuickFlight rows (minimal: scheduled times only, no actual times/terminal/gate) ---
        db.FlightProviderData.AddRange(
            new FlightProviderDataEntity
            {
                FlightNumber = "AA100", ProviderName = "QuickFlight", RawStatus = "on-time",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(2),
                LastUpdatedUtc = Base.AddMinutes(45)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA200", ProviderName = "QuickFlight", RawStatus = "delayed",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(2),
                LastUpdatedUtc = Base.AddMinutes(30)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA300", ProviderName = "QuickFlight", RawStatus = "cancelled",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(2),
                LastUpdatedUtc = Base.AddMinutes(-150)
            },
            new FlightProviderDataEntity
            {
                // QuickFlight only — AeroTrack has no AA500 entry
                FlightNumber = "AA500", ProviderName = "QuickFlight", RawStatus = "on-time",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(2),
                LastUpdatedUtc = Base.AddMinutes(20)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA700", ProviderName = "QuickFlight", RawStatus = "on-time",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(2),
                LastUpdatedUtc = Base.AddMinutes(10)
            },
            new FlightProviderDataEntity
            {
                // QuickFlight updated at 09:00 vs AeroTrack 07:00 → QuickFlight wins
                FlightNumber = "AA800", ProviderName = "QuickFlight", RawStatus = "on-time",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(2),
                LastUpdatedUtc = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA900", ProviderName = "QuickFlight", RawStatus = "on-time",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(2),
                LastUpdatedUtc = Base.AddMinutes(30)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA1000", ProviderName = "QuickFlight", RawStatus = "delayed",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(2),
                LastUpdatedUtc = Base.AddMinutes(45)
            }
        );

        // --- Extra edge-case scenarios ---
        db.FlightProviderData.AddRange(
            // AA1100: Early arrival — actual 10 min early (Δ = -600 s → OnTime)
            new FlightProviderDataEntity
            {
                FlightNumber = "AA1100", ProviderName = "AeroTrack", RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base, ActualDeparture = Base.AddMinutes(-3),
                ScheduledArrival = Base.AddHours(14), ActualArrival = Base.AddHours(14).AddMinutes(-10),
                Terminal = "T1", Gate = "G2", DelayReason = null,
                LastUpdatedUtc = Base.AddHours(12)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA1100", ProviderName = "QuickFlight", RawStatus = "on-time",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(14),
                LastUpdatedUtc = Base.AddHours(11)
            },
            // AA1200: Borderline — exactly 15 min late (Δ = 900 s → exactly OnTime boundary)
            new FlightProviderDataEntity
            {
                FlightNumber = "AA1200", ProviderName = "AeroTrack", RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base, ActualDeparture = Base.AddMinutes(15),
                ScheduledArrival = Base.AddHours(13), ActualArrival = Base.AddHours(13).AddMinutes(15),
                Terminal = "T2", Gate = "H4", DelayReason = null,
                LastUpdatedUtc = Base.AddMinutes(20)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA1200", ProviderName = "QuickFlight", RawStatus = "on-time",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(13),
                LastUpdatedUtc = Base.AddMinutes(15)
            },
            // AA1300: Just-over threshold — 16 min delay → Delayed despite ON_SCHEDULE label
            new FlightProviderDataEntity
            {
                FlightNumber = "AA1300", ProviderName = "AeroTrack", RawStatus = "ON_SCHEDULE",
                ScheduledDeparture = Base, ActualDeparture = Base.AddMinutes(16),
                ScheduledArrival = Base.AddHours(22), ActualArrival = Base.AddHours(22).AddMinutes(16),
                Terminal = "T3", Gate = "J9", DelayReason = "Late inbound aircraft",
                LastUpdatedUtc = Base.AddMinutes(20)
            },
            new FlightProviderDataEntity
            {
                FlightNumber = "AA1300", ProviderName = "QuickFlight", RawStatus = "on-time",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(22),
                LastUpdatedUtc = Base.AddMinutes(5)
            },
            // AA1400: QuickFlight-only Diverted — AeroTrack has no record
            new FlightProviderDataEntity
            {
                FlightNumber = "AA1400", ProviderName = "QuickFlight", RawStatus = "cancelled",
                ScheduledDeparture = Base, ScheduledArrival = Base.AddHours(16),
                LastUpdatedUtc = Base.AddHours(3)
            }
        );

        db.SaveChanges();
    }
}
