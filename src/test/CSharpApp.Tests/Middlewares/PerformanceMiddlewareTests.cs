using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using CSharpApp.Api.Middleware;
using CSharpApp.Tests.Common;

namespace CSharpApp.Tests.Middlewares;

public class PerformanceMiddlewareTests
{
    [Fact]
    public async Task Middleware_LogsWarning_ForSlowRequest()
    {
        // Arrange
        var settings = Options.Create(new CSharpApp.Core.Settings.PerformanceSettings { WarningThresholdMs = 10, ExcludePaths = new System.Collections.Generic.List<string>() });
        var logger = new TestLogger<CSharpApp.Api.Middleware.PerformanceLoggingMiddleware>();

        RequestDelegate next = async ctx =>
        {
            // simulate work
            await Task.Delay(50);
            ctx.Response.StatusCode = 200;
        };

        var middleware = new CSharpApp.Api.Middleware.PerformanceLoggingMiddleware(next, logger, settings);
        var context = new DefaultHttpContext();
        context.Request.Path = "/slow";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - expecting at least one warning log
        Assert.Contains(logger.Entries, e => e.LogLevel == LogLevel.Warning && e.Message.Contains("SLOW REQUEST"));
    }

    [Fact]
    public async Task Middleware_DoesNotLog_WhenPathExcluded()
    {
        // Arrange
        var settings = Options.Create(new CSharpApp.Core.Settings.PerformanceSettings { WarningThresholdMs = 1, ExcludePaths = new System.Collections.Generic.List<string> { "/health" } });
        var logger = new TestLogger<CSharpApp.Api.Middleware.PerformanceLoggingMiddleware>();

        RequestDelegate next = async ctx =>
        {
            await Task.Delay(20);
            ctx.Response.StatusCode = 200;
        };

        var middleware = new CSharpApp.Api.Middleware.PerformanceLoggingMiddleware(next, logger, settings);
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/check";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - no logs recorded
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task HttpClientMetricsHandler_LogsWarning_ForSlowOutgoing()
    {
        // Arrange
        var settings = Options.Create(new CSharpApp.Core.Settings.PerformanceSettings { WarningThresholdMs = 10 });
        var logger = new TestLogger<CSharpApp.Infrastructure.Http.HttpClientMetricsHandler>();

        var inner = new DelegatingHandlerStub((req, ct) =>
        {
            // simulate slow outgoing call
            Thread.Sleep(50);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var handler = new CSharpApp.Infrastructure.Http.HttpClientMetricsHandler(logger, settings)
        {
            InnerHandler = inner
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test/");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(logger.Entries, e => e.LogLevel == LogLevel.Warning && e.Message.Contains("OUTGOING"));
    }
}


