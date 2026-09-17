using System.Globalization;
using System.Text;
using InternLink.Models;

namespace InternLink.Services;

/// <summary>
/// Creates a single-page, self-contained PDF clearance dossier without requiring a browser
/// print dialog or any third-party PDF library. Text is centered/positioned using real Helvetica
/// glyph metrics, and long values automatically shrink, truncate, or wrap so nothing spills past
/// the page margins.
///
/// Everything from spec 2.5 is fit onto one page: official header (Student Name, Student ID,
/// Department, Company, Tenure), Final Grade Certification (composite breakdown, letter grade,
/// GPA, clearance status, sign-off), then a two-column Detailed Timesheet Audit Table (left) and
/// Supervisor Evaluation Breakdown (right).
/// </summary>
public sealed class CertificatePdfService
{
    private const double PageWidth = 595;
    private const double PageHeight = 842;

    private const double BorderMargin = 36;
    private const double ContentPadding = 28;
    private const double ContentWidth = PageWidth - 2 * (BorderMargin + ContentPadding);
    private const double LeftX = BorderMargin + ContentPadding;
    private const double RightEdge = PageWidth - BorderMargin - ContentPadding;
    private const double CenterX = PageWidth / 2;
    private const double BottomLimit = BorderMargin + ContentPadding;

    // Standard Adobe AFM widths (per 1000 em units) for ASCII 32-126.
    private static readonly int[] HelveticaWidths =
    {
        278,278,355,556,556,889,667,191,333,333,389,584,278,333,278,278,
        556,556,556,556,556,556,556,556,556,556,278,278,584,584,584,556,
        1015,667,667,722,722,667,611,778,722,278,500,667,556,833,722,778,
        667,778,722,667,611,722,667,944,667,667,611,278,278,278,469,556,
        333,556,556,500,556,556,278,556,556,222,222,500,222,833,556,556,
        556,556,333,500,278,556,500,722,500,500,500,334,260,334,584
    };

    private static readonly int[] HelveticaBoldWidths =
    {
        278,333,474,556,556,889,722,238,333,333,389,584,278,333,278,278,
        556,556,556,556,556,556,556,556,556,556,333,333,584,584,584,611,
        975,722,722,722,722,667,611,778,722,278,556,722,611,833,722,778,
        667,778,722,667,611,722,667,944,667,667,611,333,278,333,584,556,
        333,556,611,556,611,556,333,611,611,278,278,556,278,889,611,611,
        611,611,389,556,333,611,556,778,556,556,500,389,280,389,584
    };

    public byte[] Generate(Placement placement)
    {
        var content = BuildOnePageCertificate(placement);
        return BuildPdf(content);
    }

    private string BuildOnePageCertificate(Placement placement)
    {
        var studentName = string.IsNullOrWhiteSpace(placement.Student?.FullName) ? "Student" : placement.Student!.FullName;
        var studentId = string.IsNullOrWhiteSpace(placement.Student?.StudentIdNumber) ? "N/A" : placement.Student!.StudentIdNumber!;
        var department = string.IsNullOrWhiteSpace(placement.Student?.Department) ? "N/A" : placement.Student!.Department!;
        var gradedBy = string.IsNullOrWhiteSpace(placement.Grade?.GradedByFaculty?.FullName) ? "Faculty" : placement.Grade!.GradedByFaculty!.FullName;
        var grade = string.IsNullOrWhiteSpace(placement.Grade?.LetterGrade) ? "-" : placement.Grade!.LetterGrade;
        var score = (placement.Grade?.FinalScore ?? 0).ToString("0.0", CultureInfo.InvariantCulture);
        var gradePoint = (placement.Grade?.GradePoint ?? 0).ToString("0.00", CultureInfo.InvariantCulture);
        var clearanceStatus = string.IsNullOrWhiteSpace(placement.Grade?.ClearanceStatus) ? "" : placement.Grade!.ClearanceStatus;
        var supervisorScore = (placement.Grade?.SupervisorScore ?? 0).ToString("0.0", CultureInfo.InvariantCulture);
        var logbookScore = (placement.Grade?.LogbookScore ?? 0).ToString("0.0", CultureInfo.InvariantCulture);
        var defenseScore = (placement.Grade?.DefenseScore ?? 0).ToString("0.0", CultureInfo.InvariantCulture);
        var dateRange = $"{placement.StartDate:MMM d, yyyy} - {placement.EndDate:MMM d, yyyy}";
        var hoursLine = $"{dateRange}  |  {placement.ApprovedHours:0.#} approved hours";
        var internship = $"{placement.Position} at {placement.CompanyName}";
        var gradedLine = placement.Grade?.GradedDate is { } gradedDate
            ? $"Graded on {gradedDate:MMM d, yyyy} by {gradedBy}"
            : $"Graded by {gradedBy}";

        var content = new StringBuilder();
        content.AppendLine("q");
        content.AppendLine("0.08 0.70 0.62 RG 1.4 w");
        content.AppendLine(FormattableString.Invariant(
            $"{BorderMargin:0.##} {BorderMargin:0.##} {PageWidth - 2 * BorderMargin:0.##} {PageHeight - 2 * BorderMargin:0.##} re S"));

        var y = PageHeight - BorderMargin - 34;

        // ---- Header ----
        y = DrawCentered(content, "InternLink - Academic Placement Office", 10, bold: true, gray: 0.2, y: y, minSize: 8);
        y -= 17;
        y = DrawCentered(content, "Certificate of Internship Clearance", 16, bold: true, gray: 0.05, y: y, minSize: 11);
        y -= 17;
        y = DrawCentered(content, "This certifies that", 9, bold: false, gray: 0.45, y: y);
        y -= 15;
        y = DrawCentered(content, studentName, 14, bold: true, gray: 0.05, y: y, minSize: 10);
        y -= 13;
        y = DrawCentered(content, $"Student ID: {studentId}   |   Department: {department}", 8.5, bold: false, gray: 0.4, y: y, minSize: 7);
        y -= 15;
        y = DrawCentered(content, "has successfully completed an internship as", 9, bold: false, gray: 0.45, y: y);
        y -= 13;
        y = DrawCentered(content, internship, 12, bold: true, gray: 0.05, y: y, minSize: 9);
        y -= 14;
        y = DrawCentered(content, hoursLine, 8.5, bold: false, gray: 0.45, y: y, minSize: 7);
        y -= 22;

        // ---- Final Grade Certification ----
        y = DrawCentered(content, grade, 26, bold: true, gray: 0.05, y: y, minSize: 18);
        y -= 18;
        y = DrawCentered(content, $"GPA {gradePoint} / 4.00  -  {clearanceStatus}", 10, bold: true, gray: 0.15, y: y, minSize: 8);
        y -= 14;
        y = DrawCentered(content, $"Final composite score: {score} / 100", 9.5, bold: false, gray: 0.3, y: y);
        y -= 12;
        y = DrawCentered(content, $"Supervisor {supervisorScore}/100 (40%)  +  Logbook {logbookScore}/100 (30%)  +  Defense {defenseScore}/100 (30%)", 8, bold: false, gray: 0.45, y: y, minSize: 6.5);
        y -= 16;
        y = DrawCentered(content, gradedLine, 8, bold: false, gray: 0.5, y: y, minSize: 7);
        y -= 8;

        content.AppendLine("0.75 0.75 0.75 RG 0.5 w");
        content.AppendLine(FormattableString.Invariant($"{LeftX:0.##} {y:0.##} m {RightEdge:0.##} {y:0.##} l S"));
        y -= 16;

        // ---- Two-column body: Timesheet (left) | Supervisor Evaluation (right) ----
        var colGap = 18.0;
        var colWidth = (ContentWidth - colGap) / 2;
        var leftColX = LeftX;
        var rightColX = LeftX + colWidth + colGap;

        DrawTimesheetColumn(content, placement, leftColX, colWidth, y);
        DrawEvaluationColumn(content, placement, rightColX, colWidth, y);

        // ---- Sign-off, anchored to the bottom so it never collides with the columns above ----
        var signOffY = BottomLimit + 26;
        content.AppendLine("0.75 0.75 0.75 RG 0.5 w");
        content.AppendLine(FormattableString.Invariant($"{LeftX:0.##} {signOffY + 18:0.##} m {RightEdge:0.##} {signOffY + 18:0.##} l S"));
        DrawCenteredLine(content, "____________________________", 10, false, 0.3, signOffY);
        DrawCenteredLine(content, "Academic Clearance Sign-off", 8, false, 0.45, signOffY - 12);

        content.AppendLine("Q");
        return content.ToString();
    }

    private void DrawTimesheetColumn(StringBuilder content, Placement placement, double x, double width, double startY)
    {
        var y = startY;
        DrawLine(content, "Detailed Timesheet Audit", 10.5, true, 0.05, x, y);
        y -= 12;
        DrawLine(content, $"Approved: {placement.ApprovedHours:0.#} / {placement.RequiredHours}h ({placement.ProgressPercent:0.#}%)", 7.5, false, 0.4, x, y);
        y -= 14;

        var weekColW = 22.0;
        var hoursColW = 32.0;
        var statusColW = 44.0;
        var tasksColX = x + weekColW + hoursColW + statusColW;
        var tasksColW = width - weekColW - hoursColW - statusColW;

        DrawLine(content, "Wk", 7.5, true, 0.15, x, y);
        DrawLine(content, "Hrs", 7.5, true, 0.15, x + weekColW, y);
        DrawLine(content, "Status", 7.5, true, 0.15, x + weekColW + hoursColW, y);
        DrawLine(content, "Tasks", 7.5, true, 0.15, tasksColX, y);
        y -= 5;
        content.AppendLine("0.8 0.8 0.8 RG 0.4 w");
        content.AppendLine(FormattableString.Invariant($"{x:0.##} {y:0.##} m {x + width:0.##} {y:0.##} l S"));
        y -= 11;

        var entries = placement.LogbookEntries.OrderBy(l => l.WeekNumber).ToList();
        if (!entries.Any())
        {
            DrawLine(content, "No log entries recorded.", 7.5, false, 0.4, x, y);
            return;
        }

        foreach (var log in entries)
        {
            if (y < BottomLimit + 55) break; // Never crowd the sign-off block at the bottom.

            DrawLine(content, $"W{log.WeekNumber}", 7.5, false, 0.2, x, y);
            DrawLine(content, $"{log.HoursLogged:0.#}", 7.5, false, 0.2, x + weekColW, y);
            DrawLine(content, TruncateToWidth(log.Status.ToString(), 7.5, false, statusColW - 2), 7.5, false, 0.2, x + weekColW + hoursColW, y);
            DrawLine(content, TruncateToWidth(log.TasksCompleted, 7.5, false, tasksColW), 7.5, false, 0.3, tasksColX, y);
            y -= 11;
        }
    }

    private void DrawEvaluationColumn(StringBuilder content, Placement placement, double x, double width, double startY)
    {
        var y = startY;
        DrawLine(content, "Supervisor Evaluation Breakdown", 10.5, true, 0.05, x, y);
        y -= 14;

        var evaluation = placement.Evaluation;
        if (evaluation is null || !evaluation.IsSubmitted)
        {
            DrawLine(content, "No supervisor evaluation was submitted.", 8, false, 0.4, x, y);
            return;
        }

        var rows = new (string Label, int Score)[]
        {
            ("Technical Competency", evaluation.TechnicalCompetencyScore),
            ("Problem Solving & Autonomy", evaluation.ProblemSolvingAutonomyScore),
            ("Team Collaboration", evaluation.TeamCollaborationScore),
            ("Punctuality & Reliability", evaluation.PunctualityReliabilityScore),
            ("Adaptability & Learning", evaluation.AdaptabilityLearningScore),
            ("Professional Conduct", evaluation.ProfessionalConductScore),
        };

        var scoreColX = x + width - 26;
        foreach (var row in rows)
        {
            DrawLine(content, TruncateToWidth(row.Label, 8, false, scoreColX - x - 4), 8, false, 0.2, x, y);
            DrawLine(content, $"{row.Score}/5", 8, true, 0.15, scoreColX, y);
            y -= 12;
        }

        y -= 3;
        DrawLine(content, $"Overall: {evaluation.ScoreOutOf100:0.0}/100", 8.5, true, 0.05, x, y);
        y -= 13;
        DrawLine(content, TruncateToWidth($"PPO: {evaluation.Recommendation}", 8.5, true, width), 8.5, true, 0.05, x, y);
        y -= 15;

        y = DrawWrappedParagraph(content, "Key Strengths", evaluation.KeyStrengths, x, width, y, maxLines: 3);
        y -= 6;
        DrawWrappedParagraph(content, "Areas for Growth", evaluation.AreasForImprovement, x, width, y, maxLines: 3);
    }

    private double DrawWrappedParagraph(StringBuilder content, string heading, string? body, double x, double width, double y, int maxLines)
    {
        DrawLine(content, heading, 8, true, 0.15, x, y);
        y -= 10.5;

        var text = string.IsNullOrWhiteSpace(body) ? "-" : body!;
        var lines = WrapText(text, 7.5, false, width).Take(maxLines).ToList();
        foreach (var line in lines)
        {
            DrawLine(content, line, 7.5, false, 0.3, x, y);
            y -= 10;
        }

        return y;
    }

    private static IEnumerable<string> WrapText(string text, double size, bool bold, double maxWidth)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();

        foreach (var word in words)
        {
            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (MeasureWidth(candidate, size, bold) > maxWidth && current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
                current.Append(word);
            }
            else
            {
                current.Clear();
                current.Append(candidate);
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    private static string TruncateToWidth(string text, double size, bool bold, double maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (MeasureWidth(text, size, bold) <= maxWidth) return text;

        var truncated = text;
        while (truncated.Length > 1 && MeasureWidth(truncated + "...", size, bold) > maxWidth)
        {
            truncated = truncated[..^1];
        }

        return truncated + "...";
    }

    /// <summary>
    /// Draws one line of text, centered within the content area. If it would overflow past the
    /// border at the requested size, the font shrinks down to <paramref name="minSize"/>; if it
    /// still doesn't fit, it wraps onto a second line instead of running outside the page.
    /// </summary>
    private static double DrawCentered(StringBuilder content, string text, double size, bool bold, double gray, double y, double minSize = 7)
    {
        if (string.IsNullOrWhiteSpace(text)) return y;

        var fitSize = size;
        var width = MeasureWidth(text, fitSize, bold);
        while (width > ContentWidth && fitSize > minSize)
        {
            fitSize -= 0.5;
            width = MeasureWidth(text, fitSize, bold);
        }

        if (width > ContentWidth)
        {
            var (first, second) = SplitToFit(text, fitSize, bold);
            if (second is not null)
            {
                DrawCenteredLine(content, first, fitSize, bold, gray, y);
                var nextY = y - (fitSize + 3);
                DrawCenteredLine(content, second, fitSize, bold, gray, nextY);
                return nextY;
            }
        }

        DrawCenteredLine(content, text, fitSize, bold, gray, y);
        return y;
    }

    private static void DrawCenteredLine(StringBuilder content, string text, double size, bool bold, double gray, double y)
    {
        var width = MeasureWidth(text, size, bold);
        var x = Math.Max(BorderMargin + 5, CenterX - width / 2);
        DrawLine(content, text, size, bold, gray, x, y);
    }

    private static void DrawLine(StringBuilder content, string text, double size, bool bold, double gray, double x, double y)
    {
        var font = bold ? "F2" : "F1";
        var g = gray.ToString("0.##", CultureInfo.InvariantCulture);

        content.AppendLine($"{g} {g} {g} rg");
        content.AppendLine("BT");
        content.AppendLine(FormattableString.Invariant($"/{font} {size:0.##} Tf"));
        content.AppendLine(FormattableString.Invariant($"1 0 0 1 {x:0.##} {y:0.##} Tm"));
        content.AppendLine($"({EscapePdf(text)}) Tj");
        content.AppendLine("ET");
    }

    /// <summary>Greedy word-wrap into (at most) two lines for text that still can't fit at the minimum size.</summary>
    private static (string First, string? Second) SplitToFit(string text, double size, bool bold)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return (text, null);

        var first = new StringBuilder();
        var i = 0;
        for (; i < words.Length; i++)
        {
            var candidate = first.Length == 0 ? words[i] : $"{first} {words[i]}";
            if (MeasureWidth(candidate, size, bold) > ContentWidth && first.Length > 0) break;
            first.Clear();
            first.Append(candidate);
        }

        var second = i < words.Length ? string.Join(' ', words.Skip(i)) : null;
        return (first.ToString(), second);
    }

    private static double MeasureWidth(string text, double size, bool bold)
    {
        var table = bold ? HelveticaBoldWidths : HelveticaWidths;
        double units = 0;
        foreach (var c in text)
        {
            var idx = c - 32;
            units += idx >= 0 && idx < table.Length ? table[idx] : 556;
        }
        return units / 1000.0 * size;
    }

    private static string EscapePdf(string value) => value
        .Replace("\\", "\\\\")
        .Replace("(", "\\(")
        .Replace(")", "\\)")
        .Replace("\r", " ")
        .Replace("\n", " ");

    private static byte[] BuildPdf(string pageContent)
    {
        // Object numbering: 1=Catalog, 2=Pages, 3=Page, 4=Font(regular), 5=Font(bold), 6=Content stream.
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(pageContent)} >>\nstream\n{pageContent}endstream",
        };

        using var ms = new MemoryStream();
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
        ms.Write(header, 0, header.Length);

        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            var bytes = Encoding.ASCII.GetBytes($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            ms.Write(bytes, 0, bytes.Length);
        }

        var xrefPos = ms.Position;
        var xref = new StringBuilder();
        xref.AppendLine("xref");
        xref.AppendLine($"0 {objects.Count + 1}");
        xref.AppendLine("0000000000 65535 f ");
        for (var i = 1; i < offsets.Count; i++)
            xref.AppendLine($"{offsets[i]:D10} 00000 n ");
        xref.AppendLine("trailer");
        xref.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        xref.AppendLine("startxref");
        xref.AppendLine(xrefPos.ToString(CultureInfo.InvariantCulture));
        xref.AppendLine("%%EOF");

        var tail = Encoding.ASCII.GetBytes(xref.ToString());
        ms.Write(tail, 0, tail.Length);
        return ms.ToArray();
    }
}
