namespace CSharpApp.Tests.Fakes;

/// <summary>
/// An <see cref="ILogger"/> that records what was logged.
/// </summary>
/// <remarks>
/// Used where the log output <em>is</em> the behaviour under test. The performance
/// middleware's contract is that a slow request escalates to Warning, so asserting
/// on the level is asserting on the feature, not on an implementation detail.
/// </remarks>
public sealed class CapturingLoggerProvider
{
    public sealed class CapturingLogger : ILogger<RequestPerformanceMiddleware>
    {
        private readonly List<Entry> _entries = [];

        public IReadOnlyList<Entry> Entries => _entries;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _entries.Add(new Entry(logLevel, formatter(state, exception), exception));
        }

        public sealed record Entry(LogLevel Level, string Message, Exception? Exception);
    }
}
