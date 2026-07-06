using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CSharpApp.Core.Settings;

namespace CSharpApp.Api.Middleware;

public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;
    private readonly PerformanceSettings _settings;

    public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger, IOptions<PerformanceSettings> options)
    {
        _next = next;
        _logger = logger;
        _settings = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (_settings.ExcludePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        try
        {
            await _next(context);
            sw.Stop();

            var status = context.Response?.StatusCode ?? 0;
            var ms = sw.ElapsedMilliseconds;
            if (ms >= _settings.WarningThresholdMs)
            {
                _logger.LogWarning("SLOW REQUEST {Method} {Path} responded {StatusCode} in {Duration}ms | TraceId={TraceId}", context.Request.Method, path, status, ms, traceId);
            }
            else
            {
                _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Duration}ms | TraceId={TraceId}", context.Request.Method, path, status, ms, traceId);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ERROR REQUEST {Method} {Path} in {Duration}ms | TraceId={TraceId}", context.Request.Method, path, sw.ElapsedMilliseconds, traceId);
            throw;
        }
    }
}
