namespace CSharpApp.Core.Settings;

public sealed class HttpClientSettings
{
    /// <summary>
    /// Handler lifetime in minutes. Controls how often the underlying
    /// <see cref="HttpMessageHandler"/> is rotated, which is what allows a pooled
    /// client to pick up DNS changes.
    /// </summary>
    [Range(1, 60)]
    public int LifeTime { get; set; } = 5;

    /// <summary>Number of retry attempts for transient upstream failures.</summary>
    [Range(0, 10)]
    public int RetryCount { get; set; } = 3;

    /// <summary>Base delay in milliseconds between retry attempts.</summary>
    [Range(0, 10_000)]
    public int SleepDuration { get; set; } = 200;

    /// <summary>Per-attempt timeout in seconds.</summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 10;
}
