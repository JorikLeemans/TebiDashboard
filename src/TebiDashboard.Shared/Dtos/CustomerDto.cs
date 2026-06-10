namespace TebiDashboard.Shared.Dtos;

/// <summary>
/// Customer master data for dashboard and sales order views.
/// </summary>
public sealed class CustomerDto
{
    public string CustomerId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string ContactEmail { get; init; } = string.Empty;
}
