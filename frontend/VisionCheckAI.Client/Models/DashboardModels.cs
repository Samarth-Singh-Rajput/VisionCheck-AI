namespace VisionCheckAI.Client.Models;

/// <summary>Response from GET /api/dashboard/summary.</summary>
public sealed class DashboardSummaryDto
{
    public int TotalInspections { get; set; }
    public int PassCount { get; set; }
    public int DefectiveCount { get; set; }

    /// <summary>Accepted as either 0–1 or 0–100; falls back to a computed value.</summary>
    public double? DefectRate { get; set; }

    public List<CategoryCountDto> DefectsByCategory { get; set; } = new();
    public List<SeverityCountDto> DefectsBySeverity { get; set; } = new();
    public List<DailyTrendPointDto> DailyTrend { get; set; } = new();

    public double DefectRatePercent
    {
        get
        {
            if (DefectRate is not null) return Percentages.Normalize(DefectRate);
            return TotalInspections == 0 ? 0 : DefectiveCount / (double)TotalInspections * 100.0;
        }
    }
}

public sealed class CategoryCountDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class SeverityCountDto
{
    public string Severity { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class DailyTrendPointDto
{
    public DateTime Date { get; set; }
    public int Total { get; set; }
    public int Pass { get; set; }
    public int Defective { get; set; }
}
