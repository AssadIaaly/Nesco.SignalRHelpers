using System.Text.Json.Serialization;

namespace Nesco.SignalRUserManagement.Core.Models;

/// <summary>
/// Defines the type of response from a SignalR method invocation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SignalRResponseType
{
    /// <summary>
    /// Response contains JSON data directly.
    /// </summary>
    JsonObject = 0,

    /// <summary>
    /// Response contains a file path where large data has been uploaded.
    /// </summary>
    FilePath = 1,

    /// <summary>
    /// Response is null or void.
    /// </summary>
    Null = 2,

    /// <summary>
    /// An error occurred during method execution.
    /// </summary>
    Error = 3
}
