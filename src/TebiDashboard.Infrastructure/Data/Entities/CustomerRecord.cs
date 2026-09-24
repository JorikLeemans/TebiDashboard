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

    /// <summary>
    /// NetSuite customer category display name (BUILTIN.DF of category), never the numeric id.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// NetSuite custentity_tebi_klant_subcategorie display name, when the custom field is set.
    /// </summary>
    public string Subcategory { get; set; } = string.Empty;
}
