using Microsoft.Extensions.Logging;
using Nesco.SignalRUserManagement.Core.Interfaces;

namespace Nesco.SignalRUserManagement.Server.Services;

/// <summary>
/// Default implementation of <see cref="IFileReaderService"/> that reads files from the web root directory.
/// </summary>
public class DefaultFileReaderService : IFileReaderService
{
    private readonly ILogger<DefaultFileReaderService> _logger;
    private readonly string _webRootPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultFileReaderService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="webRootPath">The web root path (typically wwwroot). If null, uses current directory + wwwroot.</param>
    public DefaultFileReaderService(
        ILogger<DefaultFileReaderService> logger,
        string? webRootPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webRootPath = webRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }

    /// <inheritdoc/>
    public async Task<string> ReadFileAsync(string filePath)
    {
        _logger.LogInformation("=== DefaultFileReaderService.ReadFileAsync START ===");
        _logger.LogInformation("Input filePath: {FilePath}", filePath);
        _logger.LogInformation("WebRootPath: {WebRootPath}", _webRootPath);

        var fullPath = GetFullPath(filePath);
        _logger.LogInformation("Computed fullPath: {FullPath}", fullPath);

        if (!File.Exists(fullPath))
        {
            _logger.LogError("FILE NOT FOUND at fullPath: {FullPath}", fullPath);
            _logger.LogError("Directory exists: {Exists}", Directory.Exists(Path.GetDirectoryName(fullPath)));

            // List files in directory for debugging
            var dir = Path.GetDirectoryName(fullPath);
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir);
                _logger.LogError("Files in directory: {Files}", string.Join(", ", files.Select(Path.GetFileName)));
            }

            throw new FileNotFoundException($"File not found: {fullPath}", filePath);
        }

        try
        {
            _logger.LogInformation("File exists, reading content...");
            var content = await File.ReadAllTextAsync(fullPath);
            _logger.LogInformation("File read successfully, content length: {Length} chars", content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> FileExistsAsync(string filePath)
    {
        var fullPath = GetFullPath(filePath);
        var exists = File.Exists(fullPath);
        _logger.LogDebug("File exists check for {FilePath}: {Exists}", filePath, exists);
        return Task.FromResult(exists);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteFileAsync(string filePath)
    {
        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Cannot delete file - file not found: {FilePath}", filePath);
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file: {FilePath}", filePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    private string GetFullPath(string filePath)
    {
        // Handle both absolute and relative paths
        if (Path.IsPathRooted(filePath))
        {
            return filePath;
        }

        // Combine with web root and trim leading slashes
        return Path.Combine(_webRootPath, filePath.TrimStart('/', '\\'));
    }
}
