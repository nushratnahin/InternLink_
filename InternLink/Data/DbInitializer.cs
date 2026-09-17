using InternLink.Models;
using Microsoft.AspNetCore.Identity;

namespace InternLink.Data
{
    // Creates the Identity roles and a handful of demo accounts/records so the app
    // has something to show the first time it runs. Safe to call every startup -
    // it checks for existing data before inserting anything.
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.EnsureCreatedAsync();

            foreach (var role in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            if (userManager.Users.Any())
            {
                return; // Already seeded.
            }

            var student = await CreateUserAsync(userManager, "student@internlink.demo", "Sarah Chen", Roles.Student, "Computer Science", studentId: "CSE-2022-0148");
            var coordinator = await CreateUserAsync(userManager, "coordinator@internlink.demo", "John Smith", Roles.Coordinator, "Career Services Office");
            var faculty = await CreateUserAsync(userManager, "faculty@internlink.demo", "Dr. James Wilson", Roles.Faculty, "Faculty of Computing");

            if (student is null || coordinator is null)
            {
                return;
            }

            var placement = new Placement
            {
                StudentId = student.Id,
                CoordinatorId = coordinator.Id,
                CompanyName = "Bright Horizon Tech",
                Position = "Software Engineering Intern",
                SupervisorName = "Lisa Park",
                SupervisorEmail = "lisa.park@brighthorizon.example",
                SupervisorPhone = "555-0142",
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(1),
                RequiredHours = 300,
                Status = PlacementStatus.Active,
                CreatedDate = DateTime.UtcNow.AddMonths(-2)
            };
            context.Placements.Add(placement);
            await context.SaveChangesAsync();

            var week1Start = DateTime.UtcNow.AddMonths(-2);

            context.LogbookEntries.AddRange(
                new LogbookEntry
                {
                    PlacementId = placement.Id,
                    WeekNumber = 1,
                    WeekStartDate = week1Start,
                    WeekEndDate = week1Start.AddDays(6),
                    HoursLogged = 25,
                    TasksCompleted = "Onboarding, dev environment setup, and first sprint planning.",
                    SkillsApplied = "C#, Git, Docker",
                    ChallengesAndLearnings = "Getting familiar with the team's branching strategy and CI pipeline.",
                    Status = LogbookStatus.Approved,
                    SubmittedDate = week1Start.AddDays(7),
                    ReviewedDate = week1Start.AddDays(8),
                    ApprovedAt = week1Start.AddDays(8)
                },
                new LogbookEntry
                {
                    PlacementId = placement.Id,
                    WeekNumber = 2,
                    WeekStartDate = week1Start.AddDays(7),
                    WeekEndDate = week1Start.AddDays(13),
                    HoursLogged = 30,
                    TasksCompleted = "Built the first internal dashboard feature and wrote unit tests.",
                    SkillsApplied = "C#, EF Core, xUnit",
                    ChallengesAndLearnings = "Learned how the team structures integration tests around EF Core.",
                    Status = LogbookStatus.Approved,
                    SubmittedDate = week1Start.AddDays(14),
                    ReviewedDate = week1Start.AddDays(15),
                    ApprovedAt = week1Start.AddDays(15)
                },
                new LogbookEntry
                {
                    PlacementId = placement.Id,
                    WeekNumber = 6,
                    WeekStartDate = week1Start.AddDays(35),
                    WeekEndDate = week1Start.AddDays(41),
                    HoursLogged = 28,
                    TasksCompleted = "Fixed production bugs and paired with senior engineer on API design.",
                    SkillsApplied = "PostgreSQL, REST API design",
                    ChallengesAndLearnings = "Debugging a race condition in the queue consumer was the biggest challenge this week.",
                    Status = LogbookStatus.Submitted,
                    SubmittedDate = DateTime.UtcNow.AddDays(-2)
                }
            );

            context.Evaluations.Add(new Evaluation
            {
                PlacementId = placement.Id,
                Token = Evaluation.GenerateToken(),
                IsSubmitted = false
            });

            // A second application still moving through the (placeholder) recruitment funnel,
            // so the Pipeline page has something to demonstrate the Offered/Accepted auto-trigger with.
            context.RecruitmentApplications.Add(new RecruitmentApplication
            {
                StudentId = student.Id,
                CompanyName = "Nimbus Cloud Systems",
                Position = "Backend Engineering Intern",
                SupervisorName = "Omar Reyes",
                SupervisorEmail = "omar.reyes@nimbuscloud.example",
                StartDate = DateTime.UtcNow.AddDays(21),
                EndDate = DateTime.UtcNow.AddMonths(4),
                RequiredHours = 300,
                Status = FunnelStatus.Interviewed
            });

            await context.SaveChangesAsync();
        }

        private static async Task<ApplicationUser?> CreateUserAsync(
            UserManager<ApplicationUser> userManager, string email, string fullName, string role, string department, string? studentId = null)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Department = department,
                StudentIdNumber = studentId
            };

            var result = await userManager.CreateAsync(user, "Demo!Pass123");
            if (!result.Succeeded)
            {
                return null;
            }

            await userManager.AddToRoleAsync(user, role);
            return user;
        }
    }
}
