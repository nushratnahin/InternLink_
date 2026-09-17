using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternLink.Models
{
    public class Grade
    {
        public int Id { get; set; }

        [Required]
        public int PlacementId { get; set; }
        [ForeignKey(nameof(PlacementId))]
        public Placement? Placement { get; set; }

        public string? GradedByFacultyId { get; set; }
        [ForeignKey(nameof(GradedByFacultyId))]
        public ApplicationUser? GradedByFaculty { get; set; }

        [Range(0, 100)]
        public double DefenseScore { get; set; }

        // Snapshot values captured at grading time (so the certificate/report stays
        // stable even if logbook or evaluation data is edited afterwards).
        public double LogbookScore { get; set; }
        public double SupervisorScore { get; set; }

        public double FinalScore { get; set; }

        [StringLength(2)]
        public string LetterGrade { get; set; } = string.Empty;

        // CGPA-scale grade point (0.00 - 4.00), per the accreditation matrix.
        public double GradePoint { get; set; }

        // e.g. "Passed with Distinction", "Passed (Good)", "Failed (Requirement Not Met)".
        [StringLength(60)]
        public string ClearanceStatus { get; set; } = string.Empty;

        public DateTime GradedDate { get; set; } = DateTime.UtcNow;

        // Weighting used for the final composite score, per spec section 2.4:
        // Final Composite Score (%) = (Supervisor x 40%) + (Logbook x 30%) + (Faculty Defense x 30%)
        public const double SupervisorWeight = 0.40;
        public const double LogbookWeight = 0.30;
        public const double DefenseWeight = 0.30;

        // Anything scoring below this band is "F" / not cleared.
        public const double PassingScore = 50.0;

        /// <summary>
        /// Standard Grading Conversion Matrix (spec section 2.4). The source table explicitly
        /// anchors 80-100=A+/4.00 down to 60.0-64.9=B/3.00, then jumps straight to &lt;50.0%=F/0.00,
        /// leaving the 50.0-59.9 band undocumented. That gap is filled here by continuing the same
        /// 5-point-band / 0.25-grade-point progression (B- and C+) rather than leaving it undefined;
        /// adjust GradeBands if your institution's actual policy for that range differs.
        /// </summary>
        public static readonly (double MinScore, string Letter, double GradePoint, string ClearanceStatus)[] GradeBands =
        {
            (80.0, "A+", 4.00, "Passed with Distinction"),
            (75.0, "A",  3.75, "Passed (Excellent)"),
            (70.0, "A-", 3.50, "Passed (Very Good)"),
            (65.0, "B+", 3.25, "Passed (Good)"),
            (60.0, "B",  3.00, "Passed (Satisfactory)"),
            (55.0, "B-", 2.75, "Passed (Acceptable)"),   // gap fill - not explicit in source table
            (50.0, "C+", 2.50, "Passed (Conditional)"),  // gap fill - not explicit in source table
            (double.MinValue, "F", 0.00, "Failed (Requirement Not Met)"),
        };

        public static (string Letter, double GradePoint, string ClearanceStatus) Evaluate(double finalScore)
        {
            foreach (var band in GradeBands)
            {
                if (finalScore >= band.MinScore)
                {
                    return (band.Letter, band.GradePoint, band.ClearanceStatus);
                }
            }

            return ("F", 0.00, "Failed (Requirement Not Met)");
        }

        // Kept for backward compatibility with any existing callers - prefer Evaluate(...) which
        // also returns the grade point and clearance status required by the spec.
        public static string CalculateLetterGrade(double finalScore) => Evaluate(finalScore).Letter;
    }
}
