using System.Collections.Concurrent;
using System.Text.Json;

namespace UserManagementAndControl.MAUI.Services;

public class MethodInvocationLogger
{
    private readonly ConcurrentQueue<MethodInvocationLog> _logs = new();
    private const int MaxLogEntries = 50;

    public event Action? OnLogAdded;

    public void LogInvocation(string methodName, object? parameter, object? result, TimeSpan duration, Exception? error = null)
    {
        var log = new MethodInvocationLog
        {
            Timestamp = DateTime.Now,
            MethodName = methodName,
            Parameter = parameter != null ? JsonSerializer.Serialize(parameter, new JsonSerializerOptions { WriteIndented = false }) : null,
            Result = result != null ? JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false }) : null,
            Duration = duration,
            Success = error == null,
            Error = error?.Message
        };

        _logs.Enqueue(log);

        while (_logs.Count > MaxLogEntries)
        {
            _logs.TryDequeue(out _);
        }

        OnLogAdded?.Invoke();
    }

    public IEnumerable<MethodInvocationLog> GetLogs() => _logs.Reverse();

    public void Clear()
    {
        _logs.Clear();
        OnLogAdded?.Invoke();
    }
}

public class MethodInvocationLog
{
    public DateTime Timestamp { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string? Parameter { get; set; }
    public string? Result { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
