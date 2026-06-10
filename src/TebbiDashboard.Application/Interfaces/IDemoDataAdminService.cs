namespace TebbiDashboard.Application.Interfaces;

/// <summary>
/// Reloads persisted demo dashboard datasets for testing.
/// </summary>
public interface IDemoDataAdminService
{
    bool IsAvailable { get; }

    Task ReseedAsync(CancellationToken cancellationToken = default);
}
