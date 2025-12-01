using System.Text.Json;

namespace Nesco.SignalRCommunicator.Client.Utilities;

/// <summary>
/// Utility class for parsing and deserializing parameters received from SignalR method invocations.
/// </summary>
public static class ParameterParser
{
    /// <summary>
    /// Parses a parameter object to the specified type.
    /// Handles JsonElement, already typed objects, and serialization/deserialization conversion.
    /// </summary>
    /// <typeparam name="T">The target type to parse to. Must have a parameterless constructor.</typeparam>
    /// <param name="parameter">The parameter object to parse.</param>
    /// <returns>The parsed parameter of type T, or a new instance if parameter is null.</returns>
    /// <exception cref="ArgumentException">Thrown when the parameter cannot be parsed to the target type.</exception>
    /// <example>
    /// <code>
    /// public async Task&lt;object?&gt; ExecuteAsync(string methodName, object? parameter)
    /// {
    ///     return methodName switch
    ///     {
    ///         "Calculate" => Calculate(ParameterParser.Parse&lt;CalculationRequest&gt;(parameter)),
    ///         "ProcessData" => await ProcessDataAsync(ParameterParser.Parse&lt;ProcessRequest&gt;(parameter)),
    ///         _ => throw new NotSupportedException($"Method '{methodName}' is not supported")
    ///     };
    /// }
    /// </code>
    /// </example>
    public static T Parse<T>(object? parameter) where T : new()
    {
        if (parameter == null)
        {
            return new T();
        }

        try
        {
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };
            
            // If parameter is already a JsonElement, deserialize it
            if (parameter is JsonElement jsonElement)
            {
               
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(),options) ?? new T();
            }

            // If it's already the correct type, return it
            if (parameter is T typedParam)
            {
                return typedParam;
            }

            // Try to serialize and deserialize to convert
            var json = JsonSerializer.Serialize(parameter);
            return JsonSerializer.Deserialize<T>(json,options) ?? new T();
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to parse parameter to type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Parses a parameter object to the specified type with custom JSON options.
    /// </summary>
    /// <typeparam name="T">The target type to parse to. Must have a parameterless constructor.</typeparam>
    /// <param name="parameter">The parameter object to parse.</param>
    /// <param name="options">The JSON serializer options to use.</param>
    /// <returns>The parsed parameter of type T, or a new instance if parameter is null.</returns>
    /// <exception cref="ArgumentException">Thrown when the parameter cannot be parsed to the target type.</exception>
    public static T Parse<T>(object? parameter, JsonSerializerOptions options) where T : new()
    {
        if (parameter == null)
        {
            return new T();
        }

        try
        {
            // If parameter is already a JsonElement, deserialize it
            if (parameter is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), options) ?? new T();
            }

            // If it's already the correct type, return it
            if (parameter is T typedParam)
            {
                return typedParam;
            }

            // Try to serialize and deserialize to convert
            var json = JsonSerializer.Serialize(parameter, options);
            return JsonSerializer.Deserialize<T>(json, options) ?? new T();
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to parse parameter to type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Tries to parse a parameter object to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to parse to. Must have a parameterless constructor.</typeparam>
    /// <param name="parameter">The parameter object to parse.</param>
    /// <param name="result">The parsed parameter if successful, otherwise the default value.</param>
    /// <returns>True if parsing was successful, false otherwise.</returns>
    public static bool TryParse<T>(object? parameter, out T result) where T : new()
    {
        try
        {
            result = Parse<T>(parameter);
            return true;
        }
        catch
        {
            result = new T();
            return false;
        }
    }
}

