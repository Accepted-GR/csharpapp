namespace CSharpApp.Core.Models;

/// <summary>
/// Standardized error response model for API errors
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error messages (e.g., validation errors)
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Trace identifier for debugging
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
