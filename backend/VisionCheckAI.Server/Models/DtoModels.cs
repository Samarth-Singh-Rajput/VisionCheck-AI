using System.Text.Json.Serialization;

namespace VisionCheckAI.Server.Models;

// ==========================================
// Auth DTOs
// ==========================================
public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserDto User { get; set; } = new();
}

public sealed class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}


// ==========================================
// Product DTOs
// ==========================================
public sealed class ProductDto
{
    public string Id { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
}


// ==========================================
// Inspection DTOs
// ==========================================
public sealed class InspectionDto
{
    public string Id { get; set; } = string.Empty;
    public string? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }

    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int ImageWidth { get; set; } = 224;
    public int ImageHeight { get; set; } = 224;

    public DateTime InspectedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public string? OperatorName { get; set; }
    public string Result { get; set; } = "Pass"; // Pass | Defective
    public double Confidence { get; set; }
    public string? Severity { get; set; } // Low | Medium | High

    public List<DefectDto> Defects { get; set; } = new();
    public ReviewDto? Review { get; set; }
}

public sealed class DefectDto
{
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public BoundingBoxDto? BoundingBox { get; set; }
}

public sealed class BoundingBoxDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public sealed class ReviewDto
{
    public string Status { get; set; } = "Pending"; // Pending | Confirmed | Overridden
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? CorrectedResult { get; set; }
    public string? Notes { get; set; }
}

public sealed class ReviewRequest
{
    public bool IsConfirmed { get; set; }
    public string? CorrectedResult { get; set; }
    public string? Notes { get; set; }
}

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
}


// ==========================================
// Dashboard DTOs
// ==========================================
public sealed class DashboardSummaryDto
{
    public int TotalInspections { get; set; }
    public int PassCount { get; set; }
    public int DefectiveCount { get; set; }
    public double DefectRate { get; set; }

    public List<CategoryCountDto> DefectsByCategory { get; set; } = new();
    public List<SeverityCountDto> DefectsBySeverity { get; set; } = new();
    public List<DailyTrendPointDto> DailyTrend { get; set; } = new();
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
