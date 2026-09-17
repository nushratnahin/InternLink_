using System.ComponentModel.DataAnnotations;

namespace InternLink.Models.ViewModels
{
    public class FacultyDashboardViewModel
    {
        public List<Placement> ReadyToGrade { get; set; } = new();
        public List<Placement> Graded { get; set; } = new();
        public double AverageFinalScore { get; set; }
    }

    public class GradePlacementViewModel
    {
        public int PlacementId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        public double LogbookScore { get; set; }
        public double SupervisorScore { get; set; }
        public bool SupervisorEvaluationSubmitted { get; set; }

        [Range(0, 100, ErrorMessage = "Enter a score between 0 and 100.")]
        public double DefenseScore { get; set; }
    }
}
