// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Fhir.TemplateManagement.Configurations
{
    /// <summary>
    /// Configuration for cloud storage providers
    /// </summary>
    public class CloudStorageConfiguration
    {
        /// <summary>
        /// The cloud storage provider to use
        /// </summary>
        public CloudStorageProvider Provider { get; set; } = CloudStorageProvider.Local;

        /// <summary>
        /// Azure Storage configuration
        /// </summary>
        public AzureStorageConfiguration Azure { get; set; } = new();

        /// <summary>
        /// GCP Storage configuration
        /// </summary>
        public GcpStorageConfiguration Gcp { get; set; } = new();

        /// <summary>
        /// Gets the appropriate storage configuration based on the provider
        /// </summary>
        /// <returns>The storage configuration for the selected provider</returns>
        public IStorageConfiguration GetProviderConfiguration()
        {
            return Provider switch
            {
                CloudStorageProvider.Local => throw new InvalidOperationException("Local provider does not require storage configuration"),
                CloudStorageProvider.Azure => Azure,
                CloudStorageProvider.Gcp => Gcp,
                _ => throw new ArgumentException($"Unsupported cloud provider: {Provider}")
            };
        }
    }

    /// <summary>
    /// Azure Storage configuration
    /// </summary>
    public class AzureStorageConfiguration : IStorageConfiguration
    {
        /// <summary>
        /// Storage account name
        /// </summary>
        public string StorageAccountName { get; set; } = string.Empty;

        /// <summary>
        /// Container name
        /// </summary>
        public string ContainerName { get; set; } = string.Empty;

        /// <summary>
        /// Endpoint suffix (e.g., blob.core.windows.net)
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
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// GCS bucket name
        /// </summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// Container/prefix name within the bucket
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