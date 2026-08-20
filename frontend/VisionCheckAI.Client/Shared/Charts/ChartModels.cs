namespace VisionCheckAI.Client.Shared.Charts;

/// <summary>One row in a horizontal bar list.</summary>
public sealed record BarItem(string Label, double Value, string? ModifierClass = null);
