using Microsoft.EntityFrameworkCore;
using HbitBackend.Models.Activity;
using HbitBackend.Models.HeartRateSample;
using HbitBackend.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace HbitBackend.Data;

public class PgDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public PgDbContext(DbContextOptions<PgDbContext> options) : base(options)
    {
    }

    public DbSet<Activity> Activities { get; set; } = null!;
    public DbSet<HeartRateSample> HeartRateSamples { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ensure Email and UserName are unique at the database level
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder.Entity<Activity>()
            .HasOne(a => a.User)
            .WithMany(u => u.Activities)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relacja 1 (Activity) -> * (HeartRateSample)
        modelBuilder.Entity<HeartRateSample>()
            .HasOne(h => h.Activity)
            .WithMany(a => a.HeartRateSamples)
            .HasForeignKey(h => h.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HeartRateSample>()
            .HasIndex(h => new { h.ActivityId, h.Timestamp });
    }
}