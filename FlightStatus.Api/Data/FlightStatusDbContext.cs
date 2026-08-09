using FlightStatus.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlightStatus.Api.Data;

public sealed class FlightStatusDbContext(DbContextOptions<FlightStatusDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<FlightCatalogEntity>      FlightCatalog     => Set<FlightCatalogEntity>();
    public DbSet<FlightProviderDataEntity> FlightProviderData => Set<FlightProviderDataEntity>();
    public DbSet<FlightBookingEntity>      FlightBookings    => Set<FlightBookingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FlightCatalogEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.FlightNumber).IsUnique();
            e.Property(x => x.FlightNumber).HasMaxLength(10).IsRequired();
            e.Property(x => x.Route).HasMaxLength(100).IsRequired();
            e.Property(x => x.Origin).HasMaxLength(5).IsRequired();
            e.Property(x => x.Destination).HasMaxLength(5).IsRequired();
        });

        modelBuilder.Entity<FlightProviderDataEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.FlightNumber, x.ProviderName }).IsUnique();
            e.Property(x => x.FlightNumber).HasMaxLength(10).IsRequired();
            e.Property(x => x.ProviderName).HasMaxLength(20).IsRequired();
            e.Property(x => x.RawStatus).HasMaxLength(30).IsRequired();
            e.Property(x => x.Terminal).HasMaxLength(10);
            e.Property(x => x.Gate).HasMaxLength(10);
            e.Property(x => x.DelayReason).HasMaxLength(200);
        });

        modelBuilder.Entity<FlightBookingEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.FlightNumber).HasMaxLength(10).IsRequired();
            e.Property(x => x.UserId).IsRequired();
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
