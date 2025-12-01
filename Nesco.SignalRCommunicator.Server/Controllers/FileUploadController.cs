using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nesco.SignalRCommunicator.Core.Options;
using System.Text.RegularExpressions;

namespace Nesco.SignalRCommunicator.Server.Controllers;

/// <summary>
/// Default file upload controller for SignalR Communicator large data transfer.
/// This controller handles file uploads from clients when responses exceed the SignalR message size limit.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class FileUploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileUploadController> _logger;
    private readonly SignalRServerOptions _options;

    // Characters not allowed in file names
    private static readonly Regex InvalidFileNameChars = new(
        $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
        RegexOptions.Compiled);

    // Characters not allowed in folder names
    private static readonly Regex InvalidPathChars = new(
        $"[{Regex.Escape(new string(Path.GetInvalidPathChars()))}]",
        RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="FileUploadController"/> class.
    /// </summary>
    public FileUploadController(
        IWebHostEnvironment env,
        ILogger<FileUploadController> logger,
        IOptions<SignalRServerOptions> options)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Uploads a file. Folder and filename are taken from form fields.
    /// </summary>
    /// <returns>The file upload response containing the path to the saved file.</returns>
    [HttpPost]
    [RequestSizeLimit(104857600)] // 100MB default
    public async Task<ActionResult<FileUploadResponse>> Upload()
    {
        try
        {
            var files = HttpContext.Request.Form.Files;
            if (files.Count == 0)
            {
                _logger.LogWarning("No files received in upload request");
                return BadRequest(new { Error = "No files received" });
            }

            // Get folder and filename from form fields
            var folder = HttpContext.Request.Form["folder"].FirstOrDefault() ?? _options.TempFolder;
            var requestedFileName = HttpContext.Request.Form["fileName"].FirstOrDefault();

            // Sanitize folder name
            folder = SanitizeFolderName(folder);

            var file = files[0]; // Take the first file

            if (file.Length > _options.MaxFileSize)
            {
                _logger.LogWarning("File {FileName} exceeds maximum size of {MaxSize} bytes",
                    file.FileName, _options.MaxFileSize);
                return BadRequest(new { Error = $"File exceeds maximum size of {_options.MaxFileSize} bytes" });
            }

            var result = await SaveFileAsync(file, folder, requestedFileName);

            _logger.LogInformation("File uploaded successfully to {FilePath}", result.FilePath);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { Error = "Error uploading file" });
        }
    }

    /// <summary>
    /// Sanitizes a folder name by removing invalid characters.
    /// </summary>
    private string SanitizeFolderName(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return _options.TempFolder;
        }

        // Remove invalid path characters
        var sanitized = InvalidPathChars.Replace(folder, "_");

        // Remove any path traversal attempts
        sanitized = sanitized.Replace("..", "_");

        // Remove leading/trailing slashes and whitespace
        sanitized = sanitized.Trim('/', '\\', ' ');

        return string.IsNullOrWhiteSpace(sanitized) ? _options.TempFolder : sanitized;
    }

    /// <summary>
    /// Sanitizes a file name by removing invalid characters.
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Guid.NewGuid().ToString();
        }

        // Get extension first
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Remove invalid file name characters
        var sanitized = InvalidFileNameChars.Replace(nameWithoutExtension, "_");

        // Remove any remaining problematic characters
        sanitized = sanitized.Replace("..", "_");

        // Trim whitespace and dots
        sanitized = sanitized.Trim(' ', '.');

        // If nothing left, use GUID
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = Guid.NewGuid().ToString();
        }

        return sanitized + extension;
    }

    private async Task<FileUploadResponse> SaveFileAsync(IFormFile file, string folder, string? requestedFileName)
    {
        var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var folderPath = Path.Combine(webRootPath, folder);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            _logger.LogDebug("Created directory: {Folder}", folderPath);
        }

        // Determine the final file name
        string finalFileName;
        if (!string.IsNullOrWhiteSpace(requestedFileName))
        {
            // Use requested filename but sanitize it and keep original extension
            var extension = Path.GetExtension(file.FileName);
            var sanitizedName = SanitizeFileName(requestedFileName);

            // If sanitized name doesn't have extension, add original
            if (string.IsNullOrEmpty(Path.GetExtension(sanitizedName)))
            {
                finalFileName = sanitizedName + extension;
            }
            else
            {
                finalFileName = sanitizedName;
            }
        }
        else
        {
            // Generate unique filename with original extension
            var extension = Path.GetExtension(file.FileName);
            finalFileName = Guid.NewGuid() + extension;
        }

        var savingPath = Path.Combine(folderPath, finalFileName);

        // Delete existing file if it exists
        if (System.IO.File.Exists(savingPath))
        {
            System.IO.File.Delete(savingPath);
            _logger.LogDebug("Deleted existing file: {Path}", savingPath);
        }

        await using (var fileStream = new FileStream(savingPath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var relativePath = Path.Combine(folder, finalFileName).Replace('\\', '/');

        return new FileUploadResponse
        {
            FilePath = relativePath,
            FileName = finalFileName,
            FileSize = file.Length
        };
    }

}

/// <summary>
/// Response model for file upload operations.
/// </summary>
public class FileUploadResponse
{
    /// <summary>
    /// The relative path to the uploaded file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// The sanitized file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The size of the uploaded file in bytes.
    /// </summary>
    public long FileSize { get; set; }
}
