namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Batch payload for creating or updating goal targets for one calendar year.
/// </summary>
public sealed class SaveGoalsRequestDto
{
    public int Year { get; init; }

    /// <summary>
    /// Target values to upsert. Empty targets are ignored.
    /// </summary>
    public IReadOnlyList<GoalTargetEntryDto> Targets { get; init; } = [];
}

/// <summary>
/// One editable goal target line on the settings page.
/// </summary>
public sealed class GoalTargetEntryDto
{
    public string Period { get; init; } = string.Empty;

    /// <summary>
    /// When null, the target applies to total company revenue.
    /// </summary>
    public string? ItemId { get; init; }

    public decimal TargetValue { get; init; }
}
