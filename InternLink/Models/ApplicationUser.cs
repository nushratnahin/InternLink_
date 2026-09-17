using Microsoft.AspNetCore.Identity;

namespace InternLink.Models
{
    // Extends the built-in Identity user with the extra profile fields InternLink needs.
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        // Free-text department / faculty name (used for Coordinators and Faculty).
        public string? Department { get; set; }

        // Academic student ID / roll number - used for Students, printed on the clearance transcript.
        public string? StudentIdNumber { get; set; }

        // Convenience navigation: placements where this user is the student.
        public ICollection<Placement> PlacementsAsStudent { get; set; } = new List<Placement>();

        // Convenience navigation: placements this user (a coordinator) is managing.
        public ICollection<Placement> PlacementsAsCoordinator { get; set; } = new List<Placement>();
    }
}
