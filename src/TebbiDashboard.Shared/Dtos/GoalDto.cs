namespace TebbiDashboard.Shared.Dtos;

/// <summary>
/// Goal versus actual progress for a period or item.
/// </summary>
public sealed record GoalDto
{
    public string Id { get; init; } = string.Empty;
    public int Year { get; init; }
    public string Period { get; init; } = string.Empty;
    public string Type { get; init; } = "Revenue";
    public string? ItemId { get; init; }
    public string? ItemName { get; init; }
    public decimal TargetValue { get; init; }
    public decimal ActualValue { get; init; }
    public decimal Percentage { get; init; }
    public string Status { get; init; } = "on-track";
}
