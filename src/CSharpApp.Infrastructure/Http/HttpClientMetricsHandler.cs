using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CSharpApp.Core.Settings;

namespace CSharpApp.Infrastructure.Http;

public class HttpClientMetricsHandler : DelegatingHandler
{
    private readonly ILogger<HttpClientMetricsHandler> _logger;
    private readonly PerformanceSettings _settings;

    public HttpClientMetricsHandler(ILogger<HttpClientMetricsHandler> logger, IOptions<PerformanceSettings> options)
    {
        _logger = logger;
        _settings = options.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var traceId = Activity.Current?.Id ?? string.Empty;

        var response = await base.SendAsync(request, cancellationToken);
        sw.Stop();

        var ms = sw.ElapsedMilliseconds;
        if (ms >= _settings.WarningThresholdMs)
        {
            _logger.LogWarning("OUTGOING {Method} {Uri} responded {StatusCode} in {Duration}ms | TraceId={TraceId}", request.Method, request.RequestUri, response.StatusCode, ms, traceId);
        }
        else
        {
            _logger.LogInformation("OUTGOING {Method} {Uri} responded {StatusCode} in {Duration}ms | TraceId={TraceId}", request.Method, request.RequestUri, response.StatusCode, ms, traceId);
        }

        return response;
    }
}
