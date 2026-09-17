namespace InternLink.Models
{
    public enum PlacementStatus
    {
        Pending = 0,
        Active = 1,
        Completed = 2,
        Failed = 3
    }

    // Spec 2.2 lifecycle: DRAFT -> SUBMITTED -> APPROVED or FLAGGED (revision needed).
    // Only Approved entries are locked against student edits; Draft/Submitted/Flagged remain editable.
    public enum LogbookStatus
    {
        Draft = 0,
        Submitted = 1,
        Approved = 2,
        Flagged = 3
    }

    // Spec 2.1 "Automated Trigger": stages of the (out-of-scope) pre-placement recruitment
    // pipeline. This is a minimal placeholder pipeline just to demonstrate the trigger -
    // a real ATS (job circulars, resume screening, interview scheduling) is a separate subsystem.
    public enum FunnelStatus
    {
        Applied = 0,
        Shortlisted = 1,
        Interviewed = 2,
        Offered = 3,
        Accepted = 4,
        Rejected = 5
    }

    public static class Roles
    {
        public const string Student = "Student";
        public const string Coordinator = "Coordinator";
        public const string Supervisor = "Supervisor";
        public const string Faculty = "Faculty";

        public static readonly string[] All = { Student, Coordinator, Supervisor, Faculty };

        // Roles a person can pick for themselves on the public registration form.
        // Supervisors never log in - they use a one-time token link - so they are excluded.
        public static readonly string[] SelfRegisterable = { Student, Coordinator, Faculty };
    }
}
