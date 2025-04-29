// -------------------------------------------------------------------------------------
//  <copyright file="ImageController.cs" company="{Company Name}">
//    Copyright (c) {Company Name}. All rights reserved.
//  </copyright>
// -------------------------------------------------------------------------------------

namespace SampleSolution.Api.Controllers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services;

[Route("api/[controller]")]
[ApiController]
public class ImageController : ControllerBase
{
    private readonly IAzureBlobService _blobService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImageController> _logger;

    public ImageController(
        IHttpClientFactory httpClientFactory,
        IAzureBlobService blobService,
        ILogger<ImageController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _blobService = blobService;
        _logger = logger;
    }

    /// <summary>
    /// Downloads images from a collection of URLs and saves them to Azure Blob Storage
    /// </summary>
    /// <param name="imageUrls">Collection of full image URLs to download</param>
    /// <returns>Success response with number of images processed and their blob URLs</returns>
    [HttpPost("download")]
    public async Task<IActionResult> DownloadImages([FromBody] List<string>? imageUrls)
    {
        if (imageUrls == null || imageUrls.Count == 0)
        {
            return BadRequest("No image URLs provided");
        }

        using var semaphore = new SemaphoreSlim(5);

        var downloadTasks = imageUrls.Select(
            imageUrl => ProcessSingleImageAsync(imageUrl, semaphore)).ToList();

        var results = await Task.WhenAll(downloadTasks);

        var successfulDownloads = results.Count(r => string.IsNullOrEmpty(r.Error));

        var successfulUploads = results.Where(r => string.IsNullOrEmpty(r.Error)).ToList();
        var failedDownloads = results.Where(r => !string.IsNullOrEmpty(r.Error)).ToList();

        return Ok(
            new
            {
                SuccessCount = successfulDownloads,
                FailureCount = failedDownloads.Count,
                TotalCount = imageUrls.Count,
                SuccessfulUploads = successfulUploads.Select(s => new { s.Url }).Take(10),
                FailedUrls = failedDownloads.Select(f => new { f.Url, f.Error }).Take(10),
                Message = $"Successfully processed {successfulDownloads} out of {imageUrls.Count} images"
            });
    }

    /// <summary>
    /// Downloads images from a collection of URLs and saves them to Azure Blob Storage
    /// </summary>
    /// <param name="imageUrls">Collection of full image URLs to download</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success response with number of images processed and their blob URLs</returns>
    [HttpPost("download-simple")]
    public async Task<IActionResult> DownloadImagesSimple([FromBody] List<string>? imageUrls, CancellationToken cancellationToken)
    {
        if (imageUrls == null || imageUrls.Count == 0)
        {
            return BadRequest("No image URLs provided");
        }

        var results = new List<(string Url, string BlobUrl, string Error)>();
        var httpClient = CreateConfiguredHttpClient();

        foreach (var imageUrl in imageUrls)
        {
            try
            {
                // Create request with timeout
                using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);

                // Download the image
                using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed to download from {Url} - HTTP status: {Status}",
                        imageUrl,
                        response.StatusCode);

                    results.Add((imageUrl, string.Empty, $"HTTP error: {response.StatusCode}"));
                    continue;
                }

                await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var fileName = GenerateUniqueFileName(imageUrl);
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                using var memoryStream = new MemoryStream();
                await contentStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

                var blobUrl = await _blobService.UploadAsync(memoryStream, newFileName, contentType);

                _logger.LogInformation(
                    "Successfully downloaded image from {Url} and saved to blob storage as {FileName}",
                    imageUrl,
                    fileName);

                results.Add((imageUrl, blobUrl, string.Empty));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image from {Url}: {Message}", imageUrl, ex.Message);
                results.Add((imageUrl, string.Empty, $"Error: {ex.Message}"));
            }
        }

        var successfulDownloads = results.Count(r => string.IsNullOrEmpty(r.Error));
        var successfulUploads = results.Where(r => string.IsNullOrEmpty(r.Error)).ToList();
        var failedDownloads = results.Where(r => !string.IsNullOrEmpty(r.Error)).ToList();

        return Ok(
            new
            {
                SuccessCount = successfulDownloads,
                FailureCount = failedDownloads.Count,
                TotalCount = imageUrls.Count,
                SuccessfulUploads = successfulUploads.Select(s => new { s.Url }).Take(10),
                FailedUrls = failedDownloads.Select(f => new { f.Url, f.Error }).Take(10),
                Message = $"Successfully processed {successfulDownloads} out of {imageUrls.Count} images"
            });
    }

    private async Task ApplyBackoffDelayAsync(int attempt, string url)
    {
        var delayMs = (int)Math.Pow(2, attempt) * 500;
        await Task.Delay(delayMs);
        _logger.LogInformation(
            "Retry attempt {Attempt} for {Url} after {Delay}ms",
            attempt,
            url,
            delayMs);
    }

    private HttpClient CreateConfiguredHttpClient()
    {
        var client = _httpClientFactory.CreateClient("ImageDownloader");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    private async Task<(bool Success, string BlobUrl, string ErrorMessage)> DownloadAndSaveImageAsync(string url)
    {
        var httpClient = CreateConfiguredHttpClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed to download from {Url} - HTTP status: {Status}",
                url,
                response.StatusCode);
            return (Success: false, BlobUrl: string.Empty, ErrorMessage: $"HTTP error: {response.StatusCode}");
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cts.Token);
        var fileName = GenerateUniqueFileName(url);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return await SaveImageToBlobStorageAsync(contentStream, fileName, contentType, url);
    }

    private async Task<(bool Success, string BlobUrl, string ErrorMessage)> DownloadWithRetryAsync(
        string url,
        int maxRetries = 3)
    {
        return await ExecuteWithRetryAsync(
            () => DownloadAndSaveImageAsync(url),
            maxRetries,
            url);
    }

    private async Task<(bool Success, string BlobUrl, string ErrorMessage)> ExecuteWithRetryAsync(
        Func<Task<(bool Success, string BlobUrl, string ErrorMessage)>> operation,
        int maxRetries,
        string url)
    {
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    await ApplyBackoffDelayAsync(attempt, url);
                }

                return await operation();
            }
            catch (Exception ex)
            {
                var (shouldRetry, errorMessage) = ShouldRetryOnException(ex, attempt, maxRetries);
                if (!shouldRetry)
                {
                    return (Success: false, BlobUrl: string.Empty, ErrorMessage: errorMessage);
                }
            }
        }

        return (Success: false, BlobUrl: string.Empty, ErrorMessage: $"Failed after {maxRetries} attempts");
    }

    private string GenerateUniqueFileName(string url)
    {
        var originalFileName = Path.GetFileName(new Uri(url).LocalPath);

        if (string.IsNullOrEmpty(originalFileName) || originalFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            var extension = "";
            var lastDotIndex = originalFileName.LastIndexOf('.');
            if (lastDotIndex >= 0) extension = originalFileName.Substring(lastDotIndex);

            originalFileName = $"image_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";
        }
        else
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);
            originalFileName =
                $"{nameWithoutExtension}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
        }

        return originalFileName;
    }

    private async Task<(string Url, string BlobUrl, string Error)> ProcessSingleImageAsync(
                                string imageUrl,
        SemaphoreSlim semaphore)
    {
        try
        {
            await semaphore.WaitAsync();

            try
            {
                var result = await DownloadWithRetryAsync(imageUrl);

                return result.Success
                    ? (Url: imageUrl, result.BlobUrl, Error: string.Empty)
                    : (Url: imageUrl, BlobUrl: string.Empty, Error: result.ErrorMessage);
            }
            finally
            {
                semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading from {Url}", imageUrl);
            return (Url: imageUrl, BlobUrl: string.Empty, Error: $"Unexpected error: {ex.Message}");
        }
    }

    private async Task<(bool Success, string BlobUrl, string ErrorMessage)> SaveImageToBlobStorageAsync(
        Stream contentStream,
        string fileName,
        string contentType,
        string sourceUrl)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await contentStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

            var blobUrl = await _blobService.UploadAsync(memoryStream, newFileName, contentType);

            _logger.LogInformation(
                "Successfully downloaded image from {Url} and saved to blob storage as {FileName}",
                sourceUrl,
                fileName);

            return (Success: true, BlobUrl: blobUrl, ErrorMessage: string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving image to blob storage: {Url}, {FileName}", sourceUrl, fileName);
            return (Success: false, BlobUrl: string.Empty, ErrorMessage: $"Error saving to blob storage: {ex.Message}");
        }
    }

    private (bool ShouldRetry, string ErrorMessage) ShouldRetryOnException(
        Exception ex,
        int currentAttempt,
        int maxRetries)
    {
        // Handle timeout exceptions
        if (ex is not TaskCanceledException canceledException)
        {
            if (ex is HttpRequestException requestException &&
                (requestException.Message.Contains("SSL connection", StringComparison.OrdinalIgnoreCase) ||
                 requestException.Message.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("SSL connection issue: {Message}", ex.Message);

                return currentAttempt >= maxRetries
                    ? (false, $"SSL connection issue: {ex.Message}")
                    : (true, string.Empty);
            }

            _logger.LogError(ex, "Error during download operation");

            return currentAttempt >= maxRetries
                ? (false, $"Error after {maxRetries} retries: {ex.Message}")
                : (true, string.Empty);
        }

        _logger.LogWarning("Request timeout: {Message}", ex.Message);

        if (currentAttempt >= maxRetries || canceledException.CancellationToken.IsCancellationRequested)
        {
            return (false, $"Request timed out after {maxRetries} retries");
        }

        return (true, string.Empty);
    }
}