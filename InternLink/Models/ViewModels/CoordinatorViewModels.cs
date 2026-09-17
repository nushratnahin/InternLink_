namespace InternLink.Models.ViewModels
{
    public class CoordinatorDashboardViewModel
    {
        public List<Placement> AllPlacements { get; set; } = new();
        public List<LogbookEntry> PendingLogs { get; set; } = new();
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
        public int PendingLogCount { get; set; }
    }

    public class ReviewLogbookEntryViewModel
    {
        public int LogbookEntryId { get; set; }

        // "Approve" or "Flag" - a Flagged entry goes back to the student marked "revision needed".
        public string Decision { get; set; } = "Approve";
        public string? Comment { get; set; }
    }
}
