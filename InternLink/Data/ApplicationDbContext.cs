using InternLink.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InternLink.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Placement> Placements => Set<Placement>();
        public DbSet<LogbookEntry> LogbookEntries => Set<LogbookEntry>();
        public DbSet<Evaluation> Evaluations => Set<Evaluation>();
        public DbSet<Grade> Grades => Set<Grade>();
        public DbSet<RecruitmentApplication> RecruitmentApplications => Set<RecruitmentApplication>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Placement>()
                .HasOne(p => p.Student)
                .WithMany(u => u.PlacementsAsStudent)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Placement>()
                .HasOne(p => p.Coordinator)
                .WithMany(u => u.PlacementsAsCoordinator)
                .HasForeignKey(p => p.CoordinatorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Placement>()
                .HasOne(p => p.Evaluation)
                .WithOne(e => e.Placement!)
                .HasForeignKey<Evaluation>(e => e.PlacementId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Placement>()
                .HasOne(p => p.Grade)
                .WithOne(g => g.Placement!)
                .HasForeignKey<Grade>(g => g.PlacementId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Placement>()
                .HasMany(p => p.LogbookEntries)
                .WithOne(l => l.Placement!)
                .HasForeignKey(l => l.PlacementId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Evaluation>()
                .HasIndex(e => e.Token)
                .IsUnique();

            builder.Entity<RecruitmentApplication>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RecruitmentApplication>()
                .HasOne(a => a.Placement)
                .WithOne()
                .HasForeignKey<RecruitmentApplication>(a => a.PlacementId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
