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
    /// Uploads a file to Azure Blob Storage
    /// </summary>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentType">Content type of the file</param>
    /// <returns>URL of the uploaded blob</returns>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
}