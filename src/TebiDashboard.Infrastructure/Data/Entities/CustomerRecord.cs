namespace TebiDashboard.Infrastructure.Data.Entities;

/// <summary>
/// Customer master record for sales orders and revenue reporting.
/// </summary>
public sealed class CustomerRecord
{
    public string CustomerId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;
}
