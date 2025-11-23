using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HbitBackend.Models.User;
using HbitBackend.Models.Activity;
using HbitBackend.Models.HeartRateSample;
using HbitBackend.Models.ActivityGoal;
using HbitBackend.Models.ActivityGoalPoints;

namespace HbitBackend.Data
{
    public class DbSeeder
    {
        private readonly PgDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly Random _rng = new Random(12345); // deterministic seed for reproducibility

        public DbSeeder(PgDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task SeedIfEmptyAsync()
        {
            // If users exist, assume seeded
            if (await _db.Users.AnyAsync()) return;

            // Create 5 users
            var users = new List<User>
            {
                new User { UserName = "Wojtek", Email = "wojtek@example.com", Name = "Wojtek", Surname = "Kowalski", EmailConfirmed = true },
                new User { UserName = "kasia", Email = "kasia@example.com", Name = "Kasia", Surname = "Nowak", EmailConfirmed = true },
                new User { UserName = "marek", Email = "marek@example.com", Name = "Marek", Surname = "Wolski", EmailConfirmed = true },
                new User { UserName = "ania", Email = "ania@example.com", Name = "Ania", Surname = "Zalewska", EmailConfirmed = true },
                new User { UserName = "piotr", Email = "piotr@example.com", Name = "Piotr", Surname = "Jankowski", EmailConfirmed = true }
            };

            foreach (var u in users)
            {
                var pw = string.Equals(u.UserName, "Wojtek", StringComparison.OrdinalIgnoreCase) ? "Secret123" : "Password1";
                var res = await _userManager.CreateAsync(u, pw);
                if (!res.Succeeded)
                {
                    throw new Exception($"Failed to create user {u.UserName}: {string.Join(',', res.Errors.Select(e => e.Description))}");
                }
            }

            // reload saved users with ids
            var dbUsers = await _db.Users.ToListAsync();

            // Make everyone friends with everyone (undirected; store UserAId < UserBId to avoid duplicates)
            var friends = new List<Models.Friend.Friend>();
            for (int i = 0; i < dbUsers.Count; i++)
            {
                for (int j = i + 1; j < dbUsers.Count; j++)
                {
                    friends.Add(new Models.Friend.Friend { UserAId = dbUsers[i].Id, UserBId = dbUsers[j].Id });
                }
            }

            _db.Friends.AddRange(friends);
            await _db.SaveChangesAsync();

            // Create one shared set of goals (all users will be participants)
            var ranges = new[] { ActivityGoalRange.Daily, ActivityGoalRange.Weekly, ActivityGoalRange.Monthly, ActivityGoalRange.Yearly };

            // get all activity types to populate AcceptedActivityTypes and to pick random activity types
            var activityTypes = Enum.GetValues(typeof(ActivityType)).Cast<ActivityType>().ToList();

            // shared current time for seeding so goals and activities align
            var now = DateTimeOffset.UtcNow;

            // create shared goals list and save them
            var sharedGoals = new List<ActivityGoal>();
            foreach (var range in ranges)
            {
                var target = range switch
                {
                    ActivityGoalRange.Daily => 1,
                    ActivityGoalRange.Weekly => 1,
                    ActivityGoalRange.Monthly => 3, // ~3 per month
                    ActivityGoalRange.Yearly => 30, // ~30 per year
                    _ => 1
                };

                var goal = new ActivityGoal
                {
                    Name = $"{range} shared goal",
                    Description = "Auto-seeded shared goal",
                    Range = range,
                    TargetValue = target,
                    // accept only 2 random activity types for this goal
                    AcceptedActivityTypes = activityTypes.OrderBy(_ => _rng.Next()).Take(2).ToList(),
                    StartsAt = now.AddYears(-2),
                    EndsAt = now.AddYears(2)
                };

                _db.ActivityGoals.Add(goal);
                sharedGoals.Add(goal);
            }
            await _db.SaveChangesAsync();

            // add each user as participant to all shared goals; mark first user as owner
            for (int ui = 0; ui < dbUsers.Count; ui++)
            {
                var user = dbUsers[ui];
                foreach (var goal in sharedGoals)
                {
                    _db.ActivityGoalParticipants.Add(new ActivityGoalParticipant { ActivityGoalId = goal.Id, UserId = user.Id, IsOwner = ui == 0 });
                }
            }
            await _db.SaveChangesAsync();

            // For each user: create at least 1 activity per day for last 3 months and also 4 activities that match daily/weekly/monthly/yearly goals
            // reuse activityTypes defined above
            foreach (var user in dbUsers)
            {
                var activities = new List<Activity>();

                // create 4 specific activities spaced to match daily/weekly/monthly/yearly (approx)
                var specificDates = new[]
                {
                    now.AddDays(-1), // daily
                    now.AddDays(-7), // weekly
                    now.AddMonths(-1), // monthly
                    now.AddYears(-1) // yearly
                };

                for (int i = 0; i < specificDates.Length; i++)
                {
                    var dtSpec = DateTime.SpecifyKind(specificDates[i].UtcDateTime, DateTimeKind.Utc);
                    var a = new Activity
                    {
                        Name = specificDates[i].ToString("yyyy-MM-dd") + " sample",
                        Date = dtSpec,
                        Type = activityTypes[_rng.Next(activityTypes.Count)],
                        UserId = user.Id
                    };
                    activities.Add(a);
                }

                // ensure at least one activity per day for the last 90 days
                for (int d = 0; d < 90; d++)
                {
                    var day = now.AddDays(-d);
                    // random time during the day
                    var timeOfDay = TimeSpan.FromMinutes(_rng.Next(6 * 60, 22 * 60)); // between 6:00 and 22:00
                    var dt = DateTime.SpecifyKind(new DateTime(day.Year, day.Month, day.Day, timeOfDay.Hours, timeOfDay.Minutes, 0, DateTimeKind.Utc), DateTimeKind.Utc);
                    var a = new Activity
                    {
                        Name = $"Daily {day:yyyy-MM-dd}",
                        Date = dt,
                        Type = activityTypes[_rng.Next(activityTypes.Count)],
                        UserId = user.Id
                    };
                    activities.Add(a);
                }

                _db.Activities.AddRange(activities);
                await _db.SaveChangesAsync();

                // Add heart rate samples for each activity: generate ~1 hour of data, sample every 5 seconds (~720 points)
                var hrSamples = new List<HeartRateSample>();
                const int secondsPerActivity = 3600; // 1 hour
                const int sampleIntervalSeconds = 5; // sample every 5 seconds -> 720 samples
                var samplesPerActivity = secondsPerActivity / sampleIntervalSeconds;

                foreach (var act in activities)
                {
                    // use activity start time as the start of samples (assume act.Date is UTC)
                    var startOffset = new DateTimeOffset(act.Date, TimeSpan.Zero);

                    // base bpm between 90 and 140, then random walk to make it look realistic
                    int baseBpm = _rng.Next(90, 141);
                    int currentBpm = baseBpm;

                    for (int s = 0; s < samplesPerActivity; s++)
                    {
                        // small random walk step
                        int step = _rng.Next(-3, 4); // -3..3
                        currentBpm = Math.Clamp(currentBpm + step, 50, 200);

                        var timestamp = startOffset.AddSeconds(s * sampleIntervalSeconds);
                        hrSamples.Add(new HeartRateSample { ActivityId = act.Id, Timestamp = timestamp, Bpm = currentBpm });
                    }
                }

                _db.HeartRateSamples.AddRange(hrSamples);
                await _db.SaveChangesAsync();

                // For activities that are within goals, add ActivityGoalPoints linking some activities to the user's corresponding goal (simple mapping by range)
                var userGoals = await _db.ActivityGoalParticipants.Where(p => p.UserId == user.Id).Select(p => p.ActivityGoal).ToListAsync();
                foreach (var act in activities)
                {
                    // naive association: if activity date is within last month assign to monthly goal, etc.
                    ActivityGoal? selected = null;
                    if (act.Date >= now.AddDays(-2).UtcDateTime) selected = userGoals.FirstOrDefault(g => g.Range == ActivityGoalRange.Daily);
                    else if (act.Date >= now.AddDays(-10).UtcDateTime) selected = userGoals.FirstOrDefault(g => g.Range == ActivityGoalRange.Weekly);
                    else if (act.Date >= now.AddMonths(-2).UtcDateTime) selected = userGoals.FirstOrDefault(g => g.Range == ActivityGoalRange.Monthly);
                    else selected = userGoals.FirstOrDefault(g => g.Range == ActivityGoalRange.Yearly);

                    if (selected != null)
                    {
                        // only add points if the activity's Type is accepted by the goal
                        if (selected.AcceptedActivityTypes == null || selected.AcceptedActivityTypes.Count == 0 || selected.AcceptedActivityTypes.Contains(act.Type))
                        {
                            _db.ActivityGoalPoints.Add(new ActivityGoalPoints { ActivityId = act.Id, ActivityGoalId = selected.Id, Points = _rng.Next(10, 500) });
                        }
                    }
                }

                await _db.SaveChangesAsync();
            }
        }

        public async Task NormalizeActivityDatesAsync()
        {
            // Update all Activity.Date values to have DateTimeKind.Utc by rewriting them.
            const int batchSize = 500;
            int updated = 0;

            while (true)
            {
                var batch = await _db.Activities.OrderBy(a => a.Id).Skip(updated).Take(batchSize).ToListAsync();
                if (batch.Count == 0) break;

                foreach (var a in batch)
                {
                    // rewrite Date as UTC kind
                    var utc = DateTime.SpecifyKind(a.Date, DateTimeKind.Utc);
                    if (a.Date != utc)
                    {
                        a.Date = utc;
                        _db.Activities.Update(a);
                    }
                }

                await _db.SaveChangesAsync();
                updated += batch.Count;
            }
        }

        public async Task UpdateGoalsToTwoRandomTypesAsync()
        {
            var activityTypes = Enum.GetValues(typeof(HbitBackend.Models.Activity.ActivityType)).Cast<HbitBackend.Models.Activity.ActivityType>().ToList();
            var goals = await _db.ActivityGoals.ToListAsync();
            foreach (var g in goals)
            {
                var selected = activityTypes.OrderBy(_ => _rng.Next()).Take(2).ToList();
                g.AcceptedActivityTypes = selected;
                _db.ActivityGoals.Update(g);
            }
            await _db.SaveChangesAsync();
        }
    }
}
