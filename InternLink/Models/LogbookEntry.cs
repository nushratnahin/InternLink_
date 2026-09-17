using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternLink.Models
{
    public class LogbookEntry
    {
        public int Id { get; set; }

        [Required]
        public int PlacementId { get; set; }
        [ForeignKey(nameof(PlacementId))]
        public Placement? Placement { get; set; }

        // Spec 2.2: WeekNumber (1 to 12) and corresponding Date Range.
        [Range(1, 12)]
        public int WeekNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime WeekStartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime WeekEndDate { get; set; }

        // Spec 2.2: Minimum 1.0 hr, Maximum 60.0 hrs per week.
        [Range(1.0, 60.0)]
        public double HoursLogged { get; set; }

        [StringLength(2000)]
        public string TasksCompleted { get; set; } = string.Empty;

        [StringLength(500)]
        public string SkillsApplied { get; set; } = string.Empty;

        [StringLength(2000)]
        public string ChallengesAndLearnings { get; set; } = string.Empty;

        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

        public LogbookStatus Status { get; set; } = LogbookStatus.Draft;

        // Feedback attached when a coordinator flags an entry for revision (or any review note).
        [StringLength(1000)]
        public string? CoordinatorComment { get; set; }

        public DateTime? ReviewedDate { get; set; }

        public DateTime? ApprovedAt { get; set; }
    }
}
