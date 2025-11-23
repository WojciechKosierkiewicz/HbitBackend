using Microsoft.EntityFrameworkCore;
using HbitBackend.Models.Activity;
using HbitBackend.Models.HeartRateSample;
using HbitBackend.Models.User;
using HbitBackend.Models.HeartRateZones;
using HbitBackend.Models.ActivityGoal;
using HbitBackend.Models.ActivityGoalPoints;
using HbitBackend.Models.Friend;
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
    public DbSet<HeartRateZones> HeartRateZones { get; set; } = null!;
    public DbSet<ActivityGoal> ActivityGoals { get; set; } = null!;
    public DbSet<ActivityGoalParticipant> ActivityGoalParticipants { get; set; } = null!;
    public DbSet<ActivityGoalPoints> ActivityGoalPoints { get; set; } = null!;

    // Friends
    public DbSet<Friend> Friends { get; set; } = null!;
    public DbSet<FriendRequest> FriendRequests { get; set; } = null!;

    // Activity Goal Invites
    public DbSet<HbitBackend.Models.ActivityGoal.ActivityGoalInvite> ActivityGoalInvites { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> implementations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PgDbContext).Assembly);
    }
}