using System.Diagnostics;

namespace CSharpApp.Api.Middleware;

// Measures how long each request takes end-to-end and logs it (structured, so Serilog captures the fields).
public class RequestTimingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<RequestTimingMiddleware> _logger;

	public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// GetTimestamp/GetElapsedTime avoids allocating a Stopwatch instance per request.
		var startTimestamp = Stopwatch.GetTimestamp();

		try
		{
			await _next(context);
		}
		finally
		{
			var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

			_logger.LogInformation(
				"HTTP {RequestMethod:l} {RequestPath:l} responded {StatusCode} in {ElapsedMilliseconds:0.000} ms",
				context.Request.Method,
				context.Request.Path.Value,
				context.Response.StatusCode,
				elapsed.TotalMilliseconds);
		}
	}
}

public static class RequestTimingMiddlewareExtensions
{
	public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder app)
		=> app.UseMiddleware<RequestTimingMiddleware>();
}
