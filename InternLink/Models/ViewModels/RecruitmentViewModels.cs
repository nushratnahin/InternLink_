using System.ComponentModel.DataAnnotations;

namespace InternLink.Models.ViewModels
{
    public class CreateApplicationViewModel
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
}
