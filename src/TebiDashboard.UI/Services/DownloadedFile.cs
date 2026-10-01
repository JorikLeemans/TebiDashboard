namespace TebiDashboard.UI.Services;

/// <summary>
/// A file the API built for the browser to save.
/// </summary>
public sealed record DownloadedFile(byte[] Content, string ContentType, string FileName);

/// <summary>
/// The period export could not be downloaded. The message is safe to show on the filter bar.
/// </summary>
public sealed class PeriodExportException(string message) : Exception(message);
