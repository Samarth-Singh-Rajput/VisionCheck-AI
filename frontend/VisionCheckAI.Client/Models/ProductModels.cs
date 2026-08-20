namespace VisionCheckAI.Client.Models;

/// <summary>An inspectable part, from GET /api/products.</summary>
public sealed class ProductDto
{
    public string Id { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;

    public string Label => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} — {Name}";
}
