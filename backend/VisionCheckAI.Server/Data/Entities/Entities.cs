using System.ComponentModel.DataAnnotations;

namespace VisionCheckAI.Server.Data.Entities;

public class UserEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Inspector"; // Inspector, Supervisor, Administrator
}

public class ProductEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? Code { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;
}

public class InspectionEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? ProductId { get; set; }

    public string? ImageUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int ImageWidth { get; set; } = 224;

    public int ImageHeight { get; set; } = 224;

    public DateTime InspectedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? OperatorName { get; set; }

    public string Result { get; set; } = "Pass"; // Pass, Defective

    public double Confidence { get; set; }

    public string? Severity { get; set; } // Low, Medium, High

    public string ReviewStatus { get; set; } = "Pending"; // Pending, Confirmed, Overridden

    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? CorrectedResult { get; set; }

    public string? ReviewNotes { get; set; }

    public List<DefectEntity> Defects { get; set; } = new();
}

public class DefectEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string InspectionId { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public double BboxX { get; set; }

    public double BboxY { get; set; }

    public double BboxWidth { get; set; }

    public double BboxHeight { get; set; }
}
