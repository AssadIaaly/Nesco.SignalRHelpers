using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;

namespace Nesco.SignalRUserManagement.Client.Handlers;

/// <summary>
/// An IMethodExecutor implementation that automatically discovers and invokes
/// handler methods using reflection. Similar to how Wolverine.FX handles message routing.
/// </summary>
public class ReflectionMethodExecutor : IMethodExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReflectionMethodExecutor> _logger;
    private readonly ConcurrentDictionary<string, HandlerMethodInfo> _methodCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Event raised when a method is invoked.
    /// Parameters: methodName, parameter
    /// </summary>
    public event Action<string, object?>? OnMethodInvoked;

    /// <summary>
    /// Event raised when a method execution completes.
    /// Parameters: methodName, result, duration, error
    /// </summary>
    public event Action<string, object?, TimeSpan, Exception?>? OnMethodCompleted;

    public ReflectionMethodExecutor(
        IServiceProvider serviceProvider,
        ILogger<ReflectionMethodExecutor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a handler type for method discovery.
    /// This is called during application startup.
    /// </summary>
    public void RegisterHandlerType(Type handlerType)
    {
        if (handlerType == null) throw new ArgumentNullException(nameof(handlerType));

        var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Where(m => m.GetCustomAttribute<SignalRIgnoreAttribute>() == null);

        foreach (var method in methods)
        {
            var methodNameAttr = method.GetCustomAttribute<SignalRMethodAttribute>();
            var methodName = methodNameAttr?.MethodName ?? method.Name;

            if (_methodCache.ContainsKey(methodName))
            {
                _logger.LogWarning("Method '{MethodName}' is already registered. Skipping duplicate from {HandlerType}",
                    methodName, handlerType.Name);
                continue;
            }

            var parameters = method.GetParameters();
            Type? parameterType = null;

            if (parameters.Length > 1)
            {
                _logger.LogWarning("Method '{MethodName}' in {HandlerType} has more than one parameter. Only the first parameter will be used.",
                    methodName, handlerType.Name);
            }

            if (parameters.Length >= 1)
            {
                parameterType = parameters[0].ParameterType;
            }

            var handlerInfo = new HandlerMethodInfo
            {
                HandlerType = handlerType,
                Method = method,
                ParameterType = parameterType,
                IsAsync = method.ReturnType == typeof(Task) ||
                          (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            };

            if (_methodCache.TryAdd(methodName, handlerInfo))
            {
                _logger.LogDebug("Registered handler method '{MethodName}' from {HandlerType} (parameter: {ParameterType})",
                    methodName, handlerType.Name, parameterType?.Name ?? "none");
            }
        }
    }

    /// <summary>
    /// Gets a list of all registered method names.
    /// </summary>
    public IReadOnlyCollection<string> GetRegisteredMethods()
    {
        return _methodCache.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Checks if a method is registered.
    /// </summary>
    public bool IsMethodRegistered(string methodName)
    {
        return _methodCache.ContainsKey(methodName);
    }

    public async Task<object?> ExecuteAsync(string methodName, object? parameter)
    {
        _logger.LogInformation("Executing method: {MethodName}", methodName);
        OnMethodInvoked?.Invoke(methodName, parameter);

        var startTime = DateTime.UtcNow;
        Exception? error = null;
        object? result = null;

        // Create a scope to resolve scoped services (handlers are typically scoped)
        using var scope = _serviceProvider.CreateScope();

        try
        {
            if (!_methodCache.TryGetValue(methodName, out var handlerInfo))
            {
                _logger.LogError("No handler found for method '{MethodName}'. Registered methods: {Methods}",
                    methodName, string.Join(", ", _methodCache.Keys));
                throw new NotSupportedException($"Method '{methodName}' is not supported. No handler registered.");
            }

            // Resolve the handler from the scoped service provider
            var handler = scope.ServiceProvider.GetRequiredService(handlerInfo.HandlerType);

            // Parse the parameter if needed
            object?[] args;
            if (handlerInfo.ParameterType != null)
            {
                var parsedParameter = ParseParameter(parameter, handlerInfo.ParameterType);
                args = new[] { parsedParameter };
            }
            else
            {
                args = Array.Empty<object?>();
            }

            // Invoke the method
            var invokeResult = handlerInfo.Method.Invoke(handler, args);

            // Handle async methods
            if (handlerInfo.IsAsync && invokeResult is Task task)
            {
                await task.ConfigureAwait(false);

                // Get result if Task<T>
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    var resultProperty = taskType.GetProperty("Result");
                    result = resultProperty?.GetValue(task);
                }
            }
            else
            {
                result = invokeResult;
            }

            return result;
        }
        catch (TargetInvocationException tie)
        {
            error = tie.InnerException ?? tie;
            _logger.LogError(error, "Error executing method {MethodName}", methodName);
            throw error;
        }
        catch (Exception ex)
        {
            error = ex;
            _logger.LogError(ex, "Error executing method {MethodName}", methodName);
            throw;
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            OnMethodCompleted?.Invoke(methodName, result, duration, error);
        }
    }

    private object? ParseParameter(object? parameter, Type targetType)
    {
        if (parameter == null)
        {
            // Return default value or new instance if possible
            if (targetType.IsValueType)
            {
                return Activator.CreateInstance(targetType);
            }

            // Try to create a new instance if there's a parameterless constructor
            var constructor = targetType.GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                return Activator.CreateInstance(targetType);
            }

            return null;
        }

        // If already the correct type, return as-is
        if (targetType.IsInstanceOfType(parameter))
        {
            return parameter;
        }

        // If it's a JsonElement, deserialize it
        if (parameter is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType, _jsonOptions);
        }

        // Try to serialize and deserialize to convert
        try
        {
            var json = JsonSerializer.Serialize(parameter, _jsonOptions);
            return JsonSerializer.Deserialize(json, targetType, _jsonOptions);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to convert parameter to type {targetType.Name}", ex);
        }
    }

    private class HandlerMethodInfo
    {
        public Type HandlerType { get; init; } = null!;
        public MethodInfo Method { get; init; } = null!;
        public Type? ParameterType { get; init; }
        public bool IsAsync { get; init; }
    }
}
