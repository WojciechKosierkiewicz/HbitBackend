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

        modelBuilder.Entity<HeartRateSample>()
            .HasOne(h => h.Activity)
            .WithMany(a => a.HeartRateSamples)
            .HasForeignKey(h => h.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HeartRateSample>()
            .HasIndex(h => new { h.ActivityId, h.Timestamp });

        modelBuilder.Entity<HeartRateZones>()
            .HasOne< User >()                
            .WithOne(u => u.HeartRateZones)
            .HasForeignKey<HeartRateZones>(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HeartRateZones>()
            .HasIndex(h => h.UserId)
            .IsUnique();

        modelBuilder.Entity<ActivityGoalParticipant>()
            .HasKey(p => new { p.ActivityGoalId, p.UserId });

        modelBuilder.Entity<ActivityGoalParticipant>()
            .HasOne(p => p.ActivityGoal)
            .WithMany(g => g.Participants)
            .HasForeignKey(p => p.ActivityGoalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityGoalParticipant>()
            .HasOne(p => p.User)
            .WithMany(u => u.ActivityGoals)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<ActivityGoalPoints>()
            .HasIndex(p => new { p.ActivityId, p.ActivityGoalId });

        modelBuilder.Entity<ActivityGoalPoints>()
            .HasOne(p => p.Activity)
            .WithMany() 
            .HasForeignKey(p => p.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityGoalPoints>()
            .HasOne(p => p.ActivityGoal)
            .WithMany()
            .HasForeignKey(p => p.ActivityGoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Friend configuration
        modelBuilder.Entity<Friend>()
            .HasKey(f => new { f.UserAId, f.UserBId });

        modelBuilder.Entity<Friend>()
            .HasOne(f => f.UserA)
            .WithMany()
            .HasForeignKey(f => f.UserAId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Friend>()
            .HasOne(f => f.UserB)
            .WithMany()
            .HasForeignKey(f => f.UserBId)
            .OnDelete(DeleteBehavior.Cascade);

        // FriendRequest configuration
        modelBuilder.Entity<FriendRequest>()
            .HasOne(fr => fr.FromUser)
            .WithMany()
            .HasForeignKey(fr => fr.FromUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FriendRequest>()
            .HasOne(fr => fr.ToUser)
            .WithMany()
            .HasForeignKey(fr => fr.ToUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ActivityGoalInvite configuration
        modelBuilder.Entity<HbitBackend.Models.ActivityGoal.ActivityGoalInvite>()
            .HasOne(i => i.FromUser)
            .WithMany()
            .HasForeignKey(i => i.FromUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HbitBackend.Models.ActivityGoal.ActivityGoalInvite>()
            .HasOne(i => i.ToUser)
            .WithMany()
            .HasForeignKey(i => i.ToUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HbitBackend.Models.ActivityGoal.ActivityGoalInvite>()
            .HasOne(i => i.ActivityGoal)
            .WithMany()
            .HasForeignKey(i => i.ActivityGoalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}