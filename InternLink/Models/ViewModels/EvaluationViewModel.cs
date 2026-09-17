using System.ComponentModel.DataAnnotations;

namespace InternLink.Models.ViewModels
{
    public class SubmitEvaluationViewModel
    {
        public string Token { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Please rate this competency.")]
        public int TechnicalCompetencyScore { get; set; }

        [Range(1, 5, ErrorMessage = "Please rate this competency.")]
        public int ProblemSolvingAutonomyScore { get; set; }

        [Range(1, 5, ErrorMessage = "Please rate this competency.")]
        public int TeamCollaborationScore { get; set; }

        [Range(1, 5, ErrorMessage = "Please rate this competency.")]
        public int PunctualityReliabilityScore { get; set; }

        [Range(1, 5, ErrorMessage = "Please rate this competency.")]
        public int AdaptabilityLearningScore { get; set; }

        [Range(1, 5, ErrorMessage = "Please rate this competency.")]
        public int ProfessionalConductScore { get; set; }

        [Required, StringLength(2000)]
        public string KeyStrengths { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string AreasForImprovement { get; set; } = string.Empty;

        [Required]
        public string Recommendation { get; set; } = "Potential Hire";
    }
}
