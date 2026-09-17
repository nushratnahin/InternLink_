using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace InternLink.Models
{
    // One evaluation per placement, filled in by the host-company supervisor via a
    // token link. No login required, so the token itself is the access control.
    public class Evaluation
    {
        public int Id { get; set; }

        [Required]
        public int PlacementId { get; set; }
        [ForeignKey(nameof(PlacementId))]
        public Placement? Placement { get; set; }

        [Required, StringLength(100)]
        public string Token { get; set; } = GenerateToken();

        public DateTime LinkGeneratedDate { get; set; } = DateTime.UtcNow;

        // Spec 2.3: 30-day expiration window from generation.
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

        public bool IsSubmitted { get; set; }

        public DateTime? SubmittedDate { get; set; }

        // Six competencies from spec 2.3, each rated 1 (unsatisfactory) - 5 (exceptional).
        [Range(1, 5)]
        public int TechnicalCompetencyScore { get; set; }

        [Range(1, 5)]
        public int ProblemSolvingAutonomyScore { get; set; }

        [Range(1, 5)]
        public int TeamCollaborationScore { get; set; }

        [Range(1, 5)]
        public int PunctualityReliabilityScore { get; set; }

        [Range(1, 5)]
        public int AdaptabilityLearningScore { get; set; }

        [Range(1, 5)]
        public int ProfessionalConductScore { get; set; }

        [StringLength(2000)]
        public string? KeyStrengths { get; set; }

        [StringLength(2000)]
        public string? AreasForImprovement { get; set; }

        // Spec 2.3 PPO indicator: "Definite Hire", "Potential Hire", "Not Recommended".
        [StringLength(30)]
        public string? Recommendation { get; set; }

        [NotMapped]
        public double AverageScore =>
            new[]
            {
                TechnicalCompetencyScore, ProblemSolvingAutonomyScore, TeamCollaborationScore,
                PunctualityReliabilityScore, AdaptabilityLearningScore, ProfessionalConductScore
            }
                .Where(s => s > 0)
                .DefaultIfEmpty(0)
                .Average();

        // Spec 2.4: Scoresup = (Sum of 6 rubric scores / 30) x 100.
        [NotMapped]
        public double ScoreOutOf100 => Math.Round(AverageScore * 20, 1);

        // 64-character cryptographically random hex token per spec 2.3.
        public static string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }
}
