using Microsoft.Extensions.Logging;

namespace UniversalTasker.CLI;

public class FileLogger : ILogger, IDisposable
{
    private readonly string _name;
    private readonly LogLevel _minLevel;
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public FileLogger(string name, string filePath, LogLevel minLevel = LogLevel.Information)
    {
        _name = name;
        _minLevel = minLevel;
        _writer = new StreamWriter(filePath, append: true) { AutoFlush = true };
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
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelShort = logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "???"
        };

        lock (_lock)
        {
            _writer.WriteLine($"[{timestamp}] {levelShort} {message}");
            if (exception != null)
            {
                _writer.WriteLine(exception.ToString());
            }
        }
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}

public class CompositeLogger : ILogger, IDisposable
{
    private readonly ILogger[] _loggers;

    public CompositeLogger(params ILogger[] loggers)
    {
        _loggers = loggers;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        foreach (var logger in _loggers)
        {
            if (logger.IsEnabled(logLevel)) return true;
        }
        return false;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        foreach (var logger in _loggers)
        {
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    public void Dispose()
    {
        foreach (var logger in _loggers)
        {
            if (logger is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
