using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nesco.SignalRCommunicator.Core.Interfaces;
using Nesco.SignalRCommunicator.Core.Options;
using System.Net.Http.Json;

namespace Nesco.SignalRCommunicator.Client.Services;

/// <summary>
/// Default implementation of <see cref="IFileUploadService"/> that uploads files via HTTP.
/// </summary>
public class DefaultFileUploadService : IFileUploadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DefaultFileUploadService> _logger;
    private readonly string _uploadRoute;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultFileUploadService"/> class.
    /// </summary>
    public DefaultFileUploadService(
        HttpClient httpClient,
        ILogger<DefaultFileUploadService> logger,
        IOptions<SignalRClientOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uploadRoute = options?.Value.FileUploadRoute ?? "api/FileUpload";
    }

    /// <inheritdoc/>
    public async Task<string> UploadFileAsync(byte[] fileData, string fileName, string folder = "signalr-temp")
    {
        try
        {
            using var content = new MultipartFormDataContent();

            // Add the file
            using var fileContent = new ByteArrayContent(fileData);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", fileName);

            // Add folder and filename as form fields
            content.Add(new StringContent(folder), "folder");
            content.Add(new StringContent(fileName), "fileName");

            var response = await _httpClient.PostAsync(_uploadRoute, content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FileUploadResponse>();
                if (result != null)
                {
                    _logger.LogInformation("File uploaded successfully: {FilePath}", result.FilePath);
                    return result.FilePath;
                }

                // Fallback for plain text response
                var filePath = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("File uploaded successfully: {FilePath}", filePath);
                return filePath.Trim('"');
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("File upload failed with status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                throw new Exception($"File upload failed: {response.StatusCode} - {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> UploadStreamAsync(Stream stream, string fileName, string folder = "signalr-temp")
    {
        try
        {
            using var content = new MultipartFormDataContent();

            // Add the file
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", fileName);

            // Add folder and filename as form fields
            content.Add(new StringContent(folder), "folder");
            content.Add(new StringContent(fileName), "fileName");

            var response = await _httpClient.PostAsync(_uploadRoute, content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FileUploadResponse>();
                if (result != null)
                {
                    _logger.LogInformation("Stream uploaded successfully: {FilePath}", result.FilePath);
                    return result.FilePath;
                }

                // Fallback for plain text response
                var filePath = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Stream uploaded successfully: {FilePath}", filePath);
                return filePath.Trim('"');
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Stream upload failed with status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                throw new Exception($"Stream upload failed: {response.StatusCode} - {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading stream");
            throw;
        }
    }

}

/// <summary>
/// Response model for file upload operations.
/// </summary>
internal class FileUploadResponse
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
