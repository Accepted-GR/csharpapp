namespace CSharpApp.Core.Settings;

public sealed class PerformanceSettings
{
    // threshold in milliseconds to consider a request slow
    public int WarningThresholdMs { get; set; } = 500;

    // paths to exclude from performance logging
    public List<string> ExcludePaths { get; set; } = new List<string> { "/health", "/swagger", "/openapi", "/docs" };
}
