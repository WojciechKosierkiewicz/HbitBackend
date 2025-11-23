using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using HbitBackend.Controllers;
using HbitBackend.Data;
using HbitBackend.Models.ActivityGoal;
using HbitBackend.Models.Activity;
using HbitBackend.Models.User;

namespace HbitBackend.Tests.Controllers;

public class ActivityGoalControllerTests
{
    private PgDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<PgDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new PgDbContext(options);
    }

    private ClaimsPrincipal CreateUserPrincipal(int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task CreateActivityGoal_CreatesGoalAndOwnerParticipant()
    {
        using var db = CreateInMemoryDb("CreateActivityGoal_CreatesGoalAndOwnerParticipant");

        var controller = new ActivityGoalController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(100) };

        var dto = new HbitBackend.Models.ActivityGoal.PostActivityGoalDto
        {
            Name = "Goal1",
            Description = "Desc",
            StartsAt = DateTimeOffset.UtcNow.AddDays(-1),
            EndsAt = DateTimeOffset.UtcNow.AddDays(10),
            Range = ActivityGoalRange.Daily,
            TargetValue = 10,
            AcceptedActivityTypes = new List<ActivityType> { ActivityType.Running }
        };

        var result = await controller.CreateActivityGoal(dto);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(created.Value);
        var idProp = created.Value!.GetType().GetProperty("id");
        Assert.NotNull(idProp);
        var idVal = (int)idProp.GetValue(created.Value)!;

        // Verify DB
        var goal = await db.ActivityGoals.FirstOrDefaultAsync(g => g.Name == "Goal1");
        Assert.NotNull(goal);
        var participant = await db.ActivityGoalParticipants.FirstOrDefaultAsync(p => p.ActivityGoalId == goal!.Id && p.UserId == 100);
        Assert.NotNull(participant);
        Assert.True(participant!.IsOwner);
    }

    [Fact]
    public async Task GetActivityGoals_ReturnsGoalsForUser()
    {
        using var db = CreateInMemoryDb("GetActivityGoals_ReturnsGoalsForUser");

        var goal = new ActivityGoal { Name = "G2", Description = "D", Range = ActivityGoalRange.Daily, TargetValue = 5, StartsAt = DateTimeOffset.UtcNow, EndsAt = DateTimeOffset.UtcNow.AddDays(5) };
        db.ActivityGoals.Add(goal);
        db.ActivityGoalParticipants.Add(new ActivityGoalParticipant { ActivityGoal = goal, UserId = 201, IsOwner = true, JoinedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var controller = new ActivityGoalController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(201) };

        var result = await controller.GetActivityGoals();
        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value!);
        Assert.Single(items);
    }

    [Fact]
    public async Task Leaderboard_ReturnsTop3_WhenUserNotInRank()
    {
        using var db = CreateInMemoryDb("Leaderboard_ReturnsTop3_WhenUserNotInRank");

        // create users
        db.Users.Add(new User { Id = 1, Name = "U1", Surname = "S1", UserName = "u1" });
        db.Users.Add(new User { Id = 2, Name = "U2", Surname = "S2", UserName = "u2" });
        db.Users.Add(new User { Id = 3, Name = "U3", Surname = "S3", UserName = "u3" });
        db.Users.Add(new User { Id = 99, Name = "You", Surname = "You", UserName = "you" });

        var goal = new ActivityGoal { Id = 5, Name = "GoalL", Range = ActivityGoalRange.Daily, TargetValue = 1 };
        db.ActivityGoals.Add(goal);

        // activities and points
        db.Activities.Add(new Activity { Id = 10, UserId = 1, Date = DateTime.UtcNow, Type = ActivityType.Running, Name = "a" });
        db.ActivityGoalPoints.Add(new HbitBackend.Models.ActivityGoalPoints.ActivityGoalPoints { ActivityId = 10, ActivityGoalId = 5, Points = 50 });
        db.Activities.Add(new Activity { Id = 11, UserId = 2, Date = DateTime.UtcNow, Type = ActivityType.Running, Name = "b" });
        db.ActivityGoalPoints.Add(new HbitBackend.Models.ActivityGoalPoints.ActivityGoalPoints { ActivityId = 11, ActivityGoalId = 5, Points = 30 });
        db.Activities.Add(new Activity { Id = 12, UserId = 3, Date = DateTime.UtcNow, Type = ActivityType.Running, Name = "c" });
        db.ActivityGoalPoints.Add(new HbitBackend.Models.ActivityGoalPoints.ActivityGoalPoints { ActivityId = 12, ActivityGoalId = 5, Points = 20 });

        await db.SaveChangesAsync();

        var controller = new ActivityGoalController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(99) };

        var result = await controller.Leaderboard(5);
        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<HbitBackend.DTOs.ActivityGoal.LeaderboardItemDto>>(ok.Value!);
        Assert.Equal(3, items.Count());
        Assert.DoesNotContain(items, i => i.IsCurrentUser);
    }

    [Fact]
    public async Task Leaderboard_ReturnsNeighbors_WhenUserInRank()
    {
        using var db = CreateInMemoryDb("Leaderboard_ReturnsNeighbors_WhenUserInRank");

        db.Users.Add(new User { Id = 1, Name = "U1", Surname = "S1", UserName = "u1" });
        db.Users.Add(new User { Id = 2, Name = "U2", Surname = "S2", UserName = "u2" });
        db.Users.Add(new User { Id = 3, Name = "U3", Surname = "S3", UserName = "u3" });

        var goal = new ActivityGoal { Id = 6, Name = "GoalM", Range = ActivityGoalRange.Daily, TargetValue = 1 };
        db.ActivityGoals.Add(goal);

        db.Activities.Add(new Activity { Id = 20, UserId = 1, Date = DateTime.UtcNow, Type = ActivityType.Running, Name = "a" });
        db.ActivityGoalPoints.Add(new HbitBackend.Models.ActivityGoalPoints.ActivityGoalPoints { ActivityId = 20, ActivityGoalId = 6, Points = 100 });
        db.Activities.Add(new Activity { Id = 21, UserId = 2, Date = DateTime.UtcNow, Type = ActivityType.Running, Name = "b" });
        db.ActivityGoalPoints.Add(new HbitBackend.Models.ActivityGoalPoints.ActivityGoalPoints { ActivityId = 21, ActivityGoalId = 6, Points = 80 });
        db.Activities.Add(new Activity { Id = 22, UserId = 3, Date = DateTime.UtcNow, Type = ActivityType.Running, Name = "c" });
        db.ActivityGoalPoints.Add(new HbitBackend.Models.ActivityGoalPoints.ActivityGoalPoints { ActivityId = 22, ActivityGoalId = 6, Points = 60 });

        await db.SaveChangesAsync();

        var controller = new ActivityGoalController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(2) };

        var result = await controller.Leaderboard(6);
        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<HbitBackend.DTOs.ActivityGoal.LeaderboardItemDto>>(ok.Value!);
        // should contain neighbor above, current, neighbor below (3 items)
        Assert.Equal(3, items.Count());
        Assert.Contains(items, i => i.IsCurrentUser);
    }

    [Fact]
    public async Task CreateInvite_AuthorOwner_CreatesInvite()
    {
        using var db = CreateInMemoryDb("CreateInvite_AuthorOwner_CreatesInvite");

        // setup goal and owner
        var goal = new ActivityGoal { Id = 30, Name = "Ginv", Range = ActivityGoalRange.Daily, TargetValue = 1 };
        db.ActivityGoals.Add(goal);
        db.ActivityGoalParticipants.Add(new ActivityGoalParticipant { ActivityGoal = goal, UserId = 500, IsOwner = true, JoinedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var controller = new ActivityGoalController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(500) };

        var dto = new HbitBackend.Models.ActivityGoal.ActivityGoalInviteCreateDto { ActivityGoalId = 30, ToUserId = 501 };
        var result = await controller.CreateInvite(dto);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var inv = await db.ActivityGoalInvites.FirstOrDefaultAsync(i => i.ActivityGoalId == 30 && i.ToUserId == 501);
        Assert.NotNull(inv);
        Assert.Equal(ActivityGoalInviteStatus.Pending, inv!.Status);
    }

    [Fact]
    public async Task AcceptInvite_AddsParticipantAndMarksAccepted()
    {
        using var db = CreateInMemoryDb("AcceptInvite_AddsParticipantAndMarksAccepted");

        var invite = new ActivityGoalInvite { Id = 99, ActivityGoalId = 40, FromUserId = 600, ToUserId = 601, Status = ActivityGoalInviteStatus.Pending, CreatedAt = DateTimeOffset.UtcNow };
        db.ActivityGoalInvites.Add(invite);
        db.ActivityGoals.Add(new ActivityGoal { Id = 40, Name = "G", Range = ActivityGoalRange.Daily, TargetValue = 1 });
        await db.SaveChangesAsync();

        var controller = new ActivityGoalController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(601) };

        var result = await controller.AcceptInvite(99);
        Assert.IsType<NoContentResult>(result);

        var updated = await db.ActivityGoalInvites.FindAsync(99);
        Assert.Equal(ActivityGoalInviteStatus.Accepted, updated!.Status);
        var participant = await db.ActivityGoalParticipants.FirstOrDefaultAsync(p => p.ActivityGoalId == 40 && p.UserId == 601);
        Assert.NotNull(participant);
    }

    [Fact]
    public async Task DeclineInvite_MarksDeclined()
    {
        using var db = CreateInMemoryDb("DeclineInvite_MarksDeclined");

        var invite = new ActivityGoalInvite { Id = 199, ActivityGoalId = 50, FromUserId = 700, ToUserId = 701, Status = ActivityGoalInviteStatus.Pending, CreatedAt = DateTimeOffset.UtcNow };
        db.ActivityGoalInvites.Add(invite);
        db.ActivityGoals.Add(new ActivityGoal { Id = 50, Name = "G2", Range = ActivityGoalRange.Daily, TargetValue = 1 });
        await db.SaveChangesAsync();

        var controller = new ActivityGoalController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(701) };

        var result = await controller.DeclineInvite(199);
        Assert.IsType<NoContentResult>(result);

        var updated = await db.ActivityGoalInvites.FindAsync(199);
        Assert.Equal(ActivityGoalInviteStatus.Declined, updated!.Status);
    }
}
