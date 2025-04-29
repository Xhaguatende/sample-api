// -------------------------------------------------------------------------------------
//  <copyright file="AzureBlobSettings.cs" company="{Company Name}">
//    Copyright (c) {Company Name}. All rights reserved.
//  </copyright>
// -------------------------------------------------------------------------------------

namespace SampleSolution.Api.Settings;

/// <summary>
/// Settings for Azure Blob Storage
/// </summary>
public class AzureBlobSettings
{
    /// <summary>
    /// Name of the configuration section for Azure Blob Storage settings
    /// </summary>
    public const string AzureBlobSettingsName = "AzureBlobSettings";

    /// <summary>
    /// Gets or sets the connection string for Azure Blob Storage
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the container in Azure Blob Storage
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;
}