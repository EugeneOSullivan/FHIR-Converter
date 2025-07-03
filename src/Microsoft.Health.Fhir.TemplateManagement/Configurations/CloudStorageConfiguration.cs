// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Fhir.TemplateManagement.Configurations
{
    /// <summary>
    /// Cloud storage configuration supporting multiple cloud providers
    /// </summary>
    public class CloudStorageConfiguration
    {
        /// <summary>
        /// The cloud provider type (Azure, GCP, etc.)
        /// </summary>
        public CloudProviderType Provider { get; set; } = CloudProviderType.GCP;

        /// <summary>
        /// Azure-specific configuration
        /// </summary>
        public AzureStorageConfiguration Azure { get; set; }

        /// <summary>
        /// GCP-specific configuration
        /// </summary>
        public GcpStorageConfiguration Gcp { get; set; }

        /// <summary>
        /// Gets the appropriate storage configuration based on the provider
        /// </summary>
        /// <returns>The storage configuration for the selected provider</returns>
        public IStorageConfiguration GetProviderConfiguration()
        {
            return Provider switch
            {
                CloudProviderType.Azure => Azure,
                CloudProviderType.GCP => Gcp,
                _ => throw new ArgumentException($"Unsupported cloud provider: {Provider}")
            };
        }
    }

    /// <summary>
    /// Supported cloud provider types
    /// </summary>
    public enum CloudProviderType
    {
        Azure,
        GCP
    }

    /// <summary>
    /// Base interface for storage configurations
    /// </summary>
    public interface IStorageConfiguration
    {
        string ContainerName { get; set; }
        string GetContainerUrl();
    }

    /// <summary>
    /// Azure Storage configuration
    /// </summary>
    public class AzureStorageConfiguration : IStorageConfiguration
    {
        /// <summary>
        /// Azure Storage account name
        /// </summary>
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Container name within the storage account
        /// </summary>
        public string ContainerName { get; set; }

        /// <summary>
        /// Azure Storage endpoint suffix (default: blob.core.windows.net)
        /// </summary>
        public string EndpointSuffix { get; set; } = "blob.core.windows.net";

        /// <summary>
        /// Gets the full container URL for Azure Storage
        /// </summary>
        public string GetContainerUrl()
        {
            if (string.IsNullOrEmpty(StorageAccountName) || string.IsNullOrEmpty(ContainerName))
            {
                throw new InvalidOperationException("Azure Storage account name and container name must be specified");
            }

            return $"https://{StorageAccountName}.{EndpointSuffix}/{ContainerName}";
        }
    }

    /// <summary>
    /// GCP Storage configuration
    /// </summary>
    public class GcpStorageConfiguration : IStorageConfiguration
    {
        /// <summary>
        /// GCP project ID
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// GCS bucket name
        /// </summary>
        public string BucketName { get; set; }

        /// <summary>
        /// Container name within the bucket (optional, defaults to bucket root)
        /// </summary>
        public string ContainerName { get; set; } = string.Empty;

        /// <summary>
        /// GCS endpoint (default: storage.googleapis.com)
        /// </summary>
        public string Endpoint { get; set; } = "storage.googleapis.com";

        /// <summary>
        /// Gets the full container URL for GCP Storage
        /// </summary>
        public string GetContainerUrl()
        {
            if (string.IsNullOrEmpty(BucketName))
            {
                throw new InvalidOperationException("GCP bucket name must be specified");
            }

            var baseUrl = $"https://{Endpoint}/{BucketName}";
            return string.IsNullOrEmpty(ContainerName) ? baseUrl : $"{baseUrl}/{ContainerName}";
        }
    }
} 