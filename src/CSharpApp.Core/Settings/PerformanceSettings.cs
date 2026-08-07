namespace CSharpApp.Core.Settings;

public sealed class PerformanceSettings
{
    /// <summary>
    /// Requests at or above this duration are logged at Warning rather than
    /// Information.
    /// </summary>
    /// <remarks>
    /// The point of a threshold is that "slow" is a deployment-specific judgement.
    /// Logging every request at the same level makes the slow ones invisible in
    /// aggregate, which defeats the purpose of measuring them.
    /// </remarks>
    [Range(1, 60_000)]
    public int SlowRequestThresholdMs { get; set; } = 500;

    /// <summary>
    /// Emits an <c>X-Response-Time-Ms</c> response header.
    /// </summary>
    /// <remarks>
    /// Useful when diagnosing from the client side, but it advertises internal
    /// timing to callers, so it is opt-out for environments where that is not
    /// wanted.
    /// </remarks>
    public bool IncludeResponseTimeHeader { get; set; } = true;

    /// <summary>
    /// Correlation id header read from the request and echoed on the response.
    /// </summary>
    [Required]
    public string CorrelationIdHeader { get; set; } = "X-Correlation-Id";
}
