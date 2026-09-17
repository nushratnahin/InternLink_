using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternLink.Models
{
    // Minimal placeholder for the "main recruitment pipeline" spec 2.1 refers to
    // (job circulars, resume screening, interview scheduling). This is NOT that full
    // system - it exists only so the "Automated Trigger" (funnel reaches Offered/Accepted
    // => auto-create a Placement) has something real to trigger from.
    public class RecruitmentApplication
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;
        [ForeignKey(nameof(StudentId))]
        public ApplicationUser? Student { get; set; }

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

        public FunnelStatus Status { get; set; } = FunnelStatus.Applied;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? StatusUpdatedDate { get; set; }

        // Set once the Offered/Accepted trigger fires and a Placement has been auto-created.
        public int? PlacementId { get; set; }
        [ForeignKey(nameof(PlacementId))]
        public Placement? Placement { get; set; }
    }
}
