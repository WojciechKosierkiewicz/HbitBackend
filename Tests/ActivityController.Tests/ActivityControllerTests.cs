using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using HbitBackend.Controllers;
using HbitBackend.Data;
using HbitBackend.Models.Activity;

namespace HbitBackend.Tests.Controllers;

public class ActivityControllerTests
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
    public async Task GetAll_ReturnsOnlyUsersActivities()
    {
        using var db = CreateInMemoryDb("GetAll_ReturnsOnlyUsersActivities");

        // arrange
        db.Activities.Add(new Activity { Id = 1, Name = "A1", Date = DateTime.UtcNow, Type = ActivityType.Running, UserId = 1 });
        db.Activities.Add(new Activity { Id = 2, Name = "A2", Date = DateTime.UtcNow, Type = ActivityType.Walking, UserId = 2 });
        await db.SaveChangesAsync();

        var controller = new ActivityController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(1) };

        // act
        var result = await controller.GetAll();

        // assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<Activity>>(ok.Value!);
        Assert.Single(items);
        Assert.All(items, a => Assert.Equal(1, a.UserId));
    }

    [Fact]
    public async Task Create_CreatesActivity_WhenNotExists()
    {
        using var db = CreateInMemoryDb("Create_CreatesActivity_WhenNotExists");
        var controller = new ActivityController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(5) };

        var dto = new ActivityCreateDto
        {
            ActivityType = ActivityType.Running,
            Date = DateTime.UtcNow.Date,
            Name = "Morning run"
        };

        var result = await controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var createdActivity = Assert.IsType<Activity>(created.Value!);
        Assert.Equal(5, createdActivity.UserId);
        Assert.Equal(dto.Name, createdActivity.Name);
        Assert.Equal(dto.ActivityType.Value, createdActivity.Type);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenDuplicate()
    {
        using var db = CreateInMemoryDb("Create_ReturnsConflict_WhenDuplicate");

        db.Activities.Add(new Activity { Id = 10, Name = "A", Date = DateTime.UtcNow.Date, Type = ActivityType.Running, UserId = 7 });
        await db.SaveChangesAsync();

        var controller = new ActivityController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(7) };

        var dto = new ActivityCreateDto
        {
            ActivityType = ActivityType.Running,
            Date = DateTime.UtcNow.Date,
            Name = "Duplicate"
        };

        var result = await controller.Create(dto);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("already exists", conflict.Value!.ToString()!);
    }

    [Fact]
    public async Task GetById_ReturnsUnauthorized_ForDifferentUser()
    {
        using var db = CreateInMemoryDb("GetById_ReturnsUnauthorized_ForDifferentUser");

        db.Activities.Add(new Activity { Id = 20, Name = "Secret", Date = DateTime.UtcNow, Type = ActivityType.Running, UserId = 9 });
        await db.SaveChangesAsync();

        var controller = new ActivityController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(8) };

        var result = await controller.GetById(20);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetFriendsActivities_ReturnsFriendsActivities()
    {
        using var db = CreateInMemoryDb("GetFriendsActivities_ReturnsFriendsActivities");

        // user 1 has friend 2 and 3
        db.Friends.Add(new HbitBackend.Models.Friend.Friend { UserAId = 1, UserBId = 2 });
        db.Friends.Add(new HbitBackend.Models.Friend.Friend { UserAId = 3, UserBId = 1 });

        db.Activities.Add(new Activity { Id = 30, Name = "F2act", Date = DateTime.UtcNow.AddDays(-1), Type = ActivityType.Cycling, UserId = 2 });
        db.Activities.Add(new Activity { Id = 31, Name = "F3act", Date = DateTime.UtcNow.AddDays(-2), Type = ActivityType.Walking, UserId = 3 });
        db.Activities.Add(new Activity { Id = 32, Name = "Other", Date = DateTime.UtcNow.AddDays(-5), Type = ActivityType.Walking, UserId = 4 });
        await db.SaveChangesAsync();

        var controller = new ActivityController(db);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = CreateUserPrincipal(1) };

        var result = await controller.GetFriendsActivities(7);

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value!);
        Assert.Equal(2, items.Count());
    }
}
