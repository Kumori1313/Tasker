using Microsoft.Extensions.Logging;

namespace UniversalTasker.CLI;

public class ConsoleLogger : ILogger
{
    private readonly string _name;
    private readonly LogLevel _minLevel;
    private static readonly object Lock = new();

    public ConsoleLogger(string name, LogLevel minLevel = LogLevel.Information)
    {
        _name = name;
        _minLevel = minLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        lock (Lock)
        {
            var originalColor = Console.ForegroundColor;

            // Timestamp
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{timestamp}] ");

            // Log level
            Console.ForegroundColor = GetLogLevelColor(logLevel);
            Console.Write($"{GetLogLevelShort(logLevel)} ");

            // Message
            Console.ForegroundColor = originalColor;
            Console.WriteLine(message);

            // Exception
            if (exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exception.ToString());
                Console.ForegroundColor = originalColor;
            }
        }
    }

    private static ConsoleColor GetLogLevelColor(LogLevel level) => level switch
    {
        LogLevel.Trace => ConsoleColor.Gray,
        LogLevel.Debug => ConsoleColor.Gray,
        LogLevel.Information => ConsoleColor.Green,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Critical => ConsoleColor.DarkRed,
        _ => ConsoleColor.White
    };

    private static string GetLogLevelShort(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "???"
    };
}

public class ConsoleLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minLevel;

    public ConsoleLoggerProvider(LogLevel minLevel = LogLevel.Information)
    {
        _minLevel = minLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger(categoryName, _minLevel);
    }

    public void Dispose() { }
}
