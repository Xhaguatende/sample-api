// -------------------------------------------------------------------------------------
//  <copyright file="StorageController.cs" company="{Company Name}">
//    Copyright (c) {Company Name}. All rights reserved.
//  </copyright>
// -------------------------------------------------------------------------------------

namespace SampleSolution.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Services;

/// <summary>
/// Controller for file storage operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class StorageController : ControllerBase
{
    private readonly IAzureBlobService _blobService;
    private readonly ILogger<StorageController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageController" /> class.
    /// </summary>
    /// <param name="blobService">The Azure blob storage service</param>
    /// <param name="logger">The logger</param>
    public StorageController(IAzureBlobService blobService, ILogger<StorageController> logger)
    {
        _blobService = blobService;
        _logger = logger;
    }

    /// <summary>
    /// Deletes a file from Azure Blob Storage
    /// </summary>
    /// <param name="fileName">Name of the file to delete</param>
    /// <returns>Status of the delete operation</returns>
    [HttpDelete("delete/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteFile(string fileName)
    {
        var deleted = await _blobService.DeleteAsync(fileName);

        if (deleted)
        {
            _logger.LogInformation("File deleted: {FileName}", fileName);
            return Ok();
        }

        _logger.LogWarning("File not found for deletion: {FileName}", fileName);
        return NotFound($"File {fileName} not found");
    }

    /// <summary>
    /// Downloads a file from Azure Blob Storage
    /// </summary>
    /// <param name="fileName">Name of the file to download</param>
    /// <returns>File content</returns>
    [HttpGet("download/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadFile(string fileName)
    {
        var (content, contentType) = await _blobService.DownloadAsync(fileName);

        contentType = string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType;

        _logger.LogInformation("File downloaded: {FileName}", fileName);

        return File(content, contentType, fileName);
    }

    /// <summary>
    /// Uploads a file to Azure Blob Storage
    /// </summary>
    /// <returns>URL of the uploaded file</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file was provided or file is empty");
        }

        await using var stream = file.OpenReadStream();
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        await _blobService.UploadAsync(stream, fileName, file.ContentType);

        _logger.LogInformation("File uploaded successfully: {FileName}", fileName);

        return Ok(new { fileName });
    }
}