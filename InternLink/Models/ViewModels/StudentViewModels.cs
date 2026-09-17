using System.ComponentModel.DataAnnotations;

namespace InternLink.Models.ViewModels
{
    public class CreatePlacementViewModel
    {
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

        [Required, DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required, DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(3);

        [Range(1, 2000)]
        public int RequiredHours { get; set; } = 300;
    }

    public class LogbookEntryFormViewModel
    {
        public int Id { get; set; }

        public int PlacementId { get; set; }

        [Range(1, 12)]
        public int WeekNumber { get; set; } = 1;

        [Required, DataType(DataType.Date)]
        public DateTime WeekStartDate { get; set; } = DateTime.Today;

        [Required, DataType(DataType.Date)]
        public DateTime WeekEndDate { get; set; } = DateTime.Today.AddDays(6);

        // Spec 2.2: Minimum 1.0 hr, Maximum 60.0 hrs per week.
        [Range(1.0, 60.0)]
        public double HoursLogged { get; set; }

        [StringLength(2000)]
        public string TasksCompleted { get; set; } = string.Empty;

        [StringLength(500)]
        public string SkillsApplied { get; set; } = string.Empty;

        [StringLength(2000)]
        public string ChallengesAndLearnings { get; set; } = string.Empty;

        // True when the student clicks "Save Draft" instead of "Submit for Review".
        // Drafts skip the required-field checks below so a student can save partial work.
        public bool IsDraft { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public List<Placement> Placements { get; set; } = new();
    }
}
