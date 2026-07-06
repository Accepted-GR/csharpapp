using System;
using Microsoft.Extensions.Logging;

namespace CSharpApp.Tests.Common;

// Simple test logger that captures log entries
internal class TestLogger<T> : ILogger<T>
{
    public System.Collections.Generic.List<LogEntry> Entries { get; } = new System.Collections.Generic.List<LogEntry>();

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        Entries.Add(new LogEntry { LogLevel = logLevel, Message = message, Exception = exception });
        // Also write to console to make logs visible when running tests
        try
        {
            Console.WriteLine($"[{typeof(T).Name}] {logLevel}: {message}");
            if (exception != null)
            {
                Console.WriteLine(exception.ToString());
            }
        }
        catch
        {
            // ignore any console errors during tests
        }
    }
}

internal class LogEntry
{
    public LogLevel LogLevel { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}

internal class NullScope : IDisposable
{
    public static NullScope Instance { get; } = new NullScope();
    public void Dispose() { }
}
