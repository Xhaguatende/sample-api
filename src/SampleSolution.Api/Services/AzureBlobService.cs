// -------------------------------------------------------------------------------------
//  <copyright file="AzureBlobService.cs" company="{Company Name}">
//    Copyright (c) {Company Name}. All rights reserved.
//  </copyright>
// -------------------------------------------------------------------------------------

namespace SampleSolution.Api.Services;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Settings;

/// <summary>
/// Implementation of Azure Blob Storage service
/// </summary>
public class AzureBlobService : IAzureBlobService
{
    private readonly BlobContainerClient _containerClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobService" /> class.
    /// </summary>
    /// <param name="blobSettings">The Azure Blob Storage settings</param>
    public AzureBlobService(IOptions<AzureBlobSettings> blobSettings)
    {
        var settings = blobSettings.Value;
        var connectionString = settings.ConnectionString;
        var containerName = settings.ContainerName;

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(containerName))
        {
            throw new ArgumentException(
                "Azure Blob Storage connection string and container name must be configured in appsettings.json");
        }

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync())
        {
            return false;
        }

        await blobClient.DeleteAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task<(Stream Content, string ContentType)> DownloadAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"File {fileName} not found in blob storage");
        }

        var downloadInfo = await blobClient.DownloadAsync();

        var memoryStream = new MemoryStream();
        await downloadInfo.Value.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return (memoryStream, downloadInfo.Value.ContentType);
    }

    /// <inheritdoc />
    public async Task<int> GetItemCountAsync()
    {
        int count = 0;
        await foreach (var _ in _containerClient.GetBlobsAsync())
        {
            count++;
        }
        return count;
    }

    /// <inheritdoc />
    public async Task<bool> HasMoreItemsThanAsync(int limit)
    {
        int count = 0;
        await foreach (var _ in _containerClient.GetBlobsAsync())
        {
            count++;
            if (count > limit)
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);

        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });

        return blobClient.Uri.ToString();
    }

    /// <inheritdoc />
    public async Task<(bool WouldExceedLimit, int CurrentCount)> WouldExceedLimitAsync(int limit, int itemsToAdd)
    {
        var count = 0;

        await foreach (var _ in _containerClient.GetBlobsAsync())
        {
            count++;
            if (count + itemsToAdd > limit)
            {
                return (true, count);
            }
        }

        return (false, count);
    }
}