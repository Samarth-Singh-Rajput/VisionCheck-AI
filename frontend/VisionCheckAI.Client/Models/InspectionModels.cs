namespace VisionCheckAI.Client.Models;

/// <summary>A single inspection record returned by the API.</summary>
public sealed class InspectionDto
{
    public string Id { get; set; } = string.Empty;
    public string? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }

    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }

    public DateTime? InspectedAtUtc { get; set; }
    public DateTime? CreatedAtUtc { get; set; }

    public string? OperatorName { get; set; }

    /// <summary>"Pass" or "Defective" as decided by the model.</summary>
    public string? Result { get; set; }

    /// <summary>Model confidence, accepted as either 0–1 or 0–100.</summary>
    public double? Confidence { get; set; }

    /// <summary>"Low" | "Medium" | "High", or null when the part passed.</summary>
    public string? Severity { get; set; }

    public List<DefectDto> Defects { get; set; } = new();

    public ReviewDto? Review { get; set; }

    public DateTime? Timestamp => InspectedAtUtc ?? CreatedAtUtc;

    public double ConfidencePercent => Percentages.Normalize(Confidence);

    public bool IsDefective => InspectionResults.IsDefective(Result);

    public string ProductLabel =>
        !string.IsNullOrWhiteSpace(ProductName) ? ProductName
        : !string.IsNullOrWhiteSpace(ProductCode) ? ProductCode!
        : "—";
}

public sealed class DefectDto
{
    public string? Category { get; set; }
    public string? Severity { get; set; }
    public double? Confidence { get; set; }
    public BoundingBoxDto? BoundingBox { get; set; }

    public double ConfidencePercent => Percentages.Normalize(Confidence);
}

/// <summary>
/// Detection box. Accepted either normalised (0–1) or in pixels; the view converts
/// to percentages against ImageWidth/ImageHeight when the values exceed 1.
/// </summary>
public sealed class BoundingBoxDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public bool IsNormalised => X <= 1 && Y <= 1 && Width <= 1 && Height <= 1;
}

public sealed class ReviewDto
{
    /// <summary>"Pending" | "Confirmed" | "Overridden".</summary>
    public string? Status { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? CorrectedResult { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Payload for POST /api/inspections/{id}/review.</summary>
public sealed class ReviewRequest
{
    public bool IsConfirmed { get; set; }
    public string? CorrectedResult { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Query for GET /api/inspections.</summary>
public sealed class InspectionQuery
{
    public string? ProductId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? DefectCategory { get; set; }
    public string? Severity { get; set; }
    public string? Result { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public int FirstRowNumber => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastRowNumber => Math.Min(Page * PageSize, TotalCount);
}

public static class InspectionResults
{
    public const string Pass = "Pass";
    public const string Defective = "Defective";

    public static bool IsDefective(string? result) =>
        !string.IsNullOrWhiteSpace(result) &&
        (result.Equals(Defective, StringComparison.OrdinalIgnoreCase) ||
         result.Equals("Fail", StringComparison.OrdinalIgnoreCase) ||
         result.Equals("Reject", StringComparison.OrdinalIgnoreCase));
}

public static class Severities
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";

    public static readonly string[] All = { Low, Medium, High };
}

/// <summary>
/// Surface-condition classes produced by the inspection model. "Excellent" is the
/// pass class, the remaining four are the defect categories used in filters.
/// </summary>
public static class DefectCategories
{
    public const string Deformation = "Deformation";
    public const string Fracture = "Fracture";
    public const string Rusting = "Rusting";
    public const string Scratches = "Scratches";

    public static readonly string[] All = { Deformation, Fracture, Rusting, Scratches };
}

internal static class Percentages
{
    /// <summary>Accepts a fraction (0–1) or an already-scaled percentage (0–100).</summary>
    public static double Normalize(double? value)
    {
        if (value is null) return 0;
        var v = value.Value;
        if (double.IsNaN(v) || double.IsInfinity(v)) return 0;
        return v <= 1.0 ? Math.Clamp(v * 100.0, 0, 100) : Math.Clamp(v, 0, 100);
    }
}
