using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternLink.Models
{
    public class Placement
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;
        [ForeignKey(nameof(StudentId))]
        public ApplicationUser? Student { get; set; }

        // Assigned once a coordinator picks up the placement for review.
        public string? CoordinatorId { get; set; }
        [ForeignKey(nameof(CoordinatorId))]
        public ApplicationUser? Coordinator { get; set; }

        [Required, StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Position { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string SupervisorName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string SupervisorEmail { get; set; } = string.Empty;

        [StringLength(30)]
        public string? SupervisorPhone { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Range(1, 2000)]
        public int RequiredHours { get; set; } = 300;

        public PlacementStatus Status { get; set; } = PlacementStatus.Pending;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<LogbookEntry> LogbookEntries { get; set; } = new List<LogbookEntry>();

        public Evaluation? Evaluation { get; set; }

        public Grade? Grade { get; set; }

        // Only APPROVED hours count toward progress - Draft/Submitted/Flagged contribute 0 (spec 2.2).
        [NotMapped]
        public double ApprovedHours => LogbookEntries
            .Where(l => l.Status == LogbookStatus.Approved)
            .Sum(l => l.HoursLogged);

        // Completion Progress (%) = Min(100, (Total Approved Hours / Target Hours) x 100) - spec 2.2.
        // Capped at 100% even if approved hours exceed the target (anti-inflation cap).
        [NotMapped]
        public double ProgressPercent => RequiredHours <= 0
            ? 0
            : Math.Min(100.0, Math.Round((ApprovedHours / (double)RequiredHours) * 100, 1));
    }
}
