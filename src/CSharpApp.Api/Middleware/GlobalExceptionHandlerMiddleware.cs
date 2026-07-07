using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CSharpApp.Core.Models;

namespace CSharpApp.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches and transforms exceptions into standardized error responses
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException validationEx)
        {
            await HandleValidationExceptionAsync(context, validationEx);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        _logger.LogWarning(
            "Validation failed for request {Method} {Path} | TraceId={TraceId} | Errors={ErrorCount}",
            context.Request.Method,
            context.Request.Path,
            traceId,
            exception.Errors.Count());

        var errorResponse = new ErrorResponse
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = "One or more validation errors occurred.",
            Errors = exception.Errors.Select(e => e.ErrorMessage).ToList(),
            TraceId = traceId
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception occurred for request {Method} {Path} | TraceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            traceId);

        var statusCode = exception switch
        {
            ArgumentNullException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            NotImplementedException => HttpStatusCode.NotImplemented,
            _ => HttpStatusCode.InternalServerError
        };

        var errorResponse = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = statusCode == HttpStatusCode.InternalServerError
                ? "An internal server error occurred. Please try again later."
                : exception.Message,
            TraceId = traceId
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
