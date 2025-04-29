// -------------------------------------------------------------------------------------
//  <copyright file="IAzureBlobService.cs" company="{Company Name}">
//    Copyright (c) {Company Name}. All rights reserved.
//  </copyright>
// -------------------------------------------------------------------------------------

namespace SampleSolution.Api.Services;

/// <summary>
/// Interface for Azure Blob Storage operations
/// </summary>
public interface IAzureBlobService
{
    /// <summary>
    /// Deletes a file from Azure Blob Storage
    /// </summary>
    /// <param name="fileName">Name of the file to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteAsync(string fileName);

    /// <summary>
    /// Downloads a file from Azure Blob Storage
    /// </summary>
    /// <param name="fileName">Name of the file to download</param>
    /// <returns>A tuple containing the file stream and its content type</returns>
    Task<(Stream Content, string ContentType)> DownloadAsync(string fileName);

    /// <summary>
    /// Gets the count of items in the blob container
    /// </summary>
    /// <returns>The number of items in the container</returns>
    Task<int> GetItemCountAsync();

    /// <summary>
    /// Checks if the container has more than the specified number of items
    /// </summary>
    /// <param name="limit">The maximum number of items</param>
    /// <returns>True if the container has more than the specified number of items, false otherwise</returns>
    Task<bool> HasMoreItemsThanAsync(int limit);

    /// <summary>
    /// Checks if adding specified number of items would exceed the storage limit
    /// </summary>
    /// <param name="limit">The maximum number of items allowed</param>
    /// <param name="itemsToAdd">Number of items to be added</param>
    /// <returns>Tuple containing: whether limit would be exceeded, and the current item count</returns>
    Task<(bool WouldExceedLimit, int CurrentCount)> WouldExceedLimitAsync(int limit, int itemsToAdd);

    /// <summary>
    /// Uploads a file to Azure Blob Storage
    /// </summary>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentType">Content type of the file</param>
    /// <returns>URL of the uploaded blob</returns>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
}