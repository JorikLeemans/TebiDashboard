namespace TebbiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Sales or revenue goal persisted for progress tracking.
/// </summary>
public sealed class GoalRecord
{
    public string Id { get; set; } = string.Empty;

    public int Year { get; set; }

    public string Period { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? ItemId { get; set; }

    public string? ItemName { get; set; }

    public decimal TargetValue { get; set; }

    public decimal ActualValue { get; set; }

    public string Status { get; set; } = string.Empty;
}
