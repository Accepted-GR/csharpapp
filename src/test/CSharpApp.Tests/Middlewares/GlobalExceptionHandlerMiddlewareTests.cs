using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CSharpApp.Api.Middleware;
using CSharpApp.Core.Models;

namespace CSharpApp.Tests.Middlewares;

public class GlobalExceptionHandlerMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlerMiddleware>> _mockLogger;
    private readonly DefaultHttpContext _context;

    public GlobalExceptionHandlerMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task Middleware_ContinuesExecution_WhenNoExceptionThrown()
    {
        // Arrange
        var nextCalled = false;
        Task Next(HttpContext ctx)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new GlobalExceptionHandlerMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, _context.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_Returns400_WhenValidationExceptionThrown()
    {
        // Arrange
        var validationFailures = new[]
        {
            new ValidationFailure("Title", "Title is required"),
            new ValidationFailure("Price", "Price must be greater than zero")
        };
        var validationException = new ValidationException(validationFailures);

        Task Next(HttpContext ctx) => throw validationException;

        var middleware = new GlobalExceptionHandlerMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(400, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal(400, errorResponse!.StatusCode);
        Assert.Equal("One or more validation errors occurred.", errorResponse.Message);
        Assert.NotNull(errorResponse.Errors);
        Assert.Equal(2, errorResponse.Errors!.Count);
        Assert.Contains("Title is required", errorResponse.Errors);
        Assert.Contains("Price must be greater than zero", errorResponse.Errors);
        Assert.NotNull(errorResponse.TraceId);
    }

    [Fact]
    public async Task Middleware_Returns400_WhenArgumentExceptionThrown()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");
        Task Next(HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionHandlerMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(400, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal(400, errorResponse!.StatusCode);
        Assert.Equal("Invalid argument", errorResponse.Message);
        Assert.NotNull(errorResponse.TraceId);
    }

    [Fact]
    public async Task Middleware_Returns401_WhenUnauthorizedAccessExceptionThrown()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Unauthorized");
        Task Next(HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionHandlerMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(401, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal(401, errorResponse!.StatusCode);
        Assert.Equal("Unauthorized", errorResponse.Message);
    }

    [Fact]
    public async Task Middleware_Returns500_WhenGenericExceptionThrown()
    {
        // Arrange
        var exception = new Exception("Something went wrong");
        Task Next(HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionHandlerMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(500, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal(500, errorResponse!.StatusCode);
        // Should return generic message for security
        Assert.Equal("An internal server error occurred. Please try again later.", errorResponse.Message);
        Assert.NotNull(errorResponse.TraceId);
    }

    [Fact]
    public async Task Middleware_Returns501_WhenNotImplementedExceptionThrown()
    {
        // Arrange
        var exception = new NotImplementedException("Feature not implemented");
        Task Next(HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionHandlerMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal(501, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal(501, errorResponse!.StatusCode);
        Assert.Equal("Feature not implemented", errorResponse.Message);
    }

    [Fact]
    public async Task Middleware_LogsWarning_WhenValidationExceptionThrown()
    {
        // Arrange
        var validationFailures = new[]
        {
            new ValidationFailure("Title", "Title is required")
        };
        var validationException = new ValidationException(validationFailures);

        Task Next(HttpContext ctx) => throw validationException;

        var middleware = new GlobalExceptionHandlerMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Middleware_LogsError_WhenGenericExceptionThrown()
    {
        // Arrange
        var exception = new Exception("Something went wrong");
        Task Next(HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionHandlerMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled exception")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Middleware_IncludesTraceId_InErrorResponse()
    {
        // Arrange
        var exception = new ArgumentException("Test exception");
        Task Next(HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionHandlerMiddleware(Next, _mockLogger.Object);
        _context.TraceIdentifier = "test-trace-id-123";

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse!.TraceId);
        // TraceId should be either Activity.Current.Id or context.TraceIdentifier
        Assert.True(!string.IsNullOrEmpty(errorResponse.TraceId));
    }
}
