// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using EnsureThat;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.TemplateManagement.ArtifactProviders;
using Microsoft.Health.Fhir.TemplateManagement.Configurations;

namespace Microsoft.Health.Fhir.TemplateManagement.Factory
{
    /// <summary>
    /// Factory for creating template collection providers based on configuration
    /// </summary>
    public class ConvertDataTemplateCollectionProviderFactory
    {
        private readonly TemplateHostingConfiguration _templateHostingConfiguration;
        private readonly TemplateCollectionConfiguration _templateCollectionConfiguration;
        private readonly IMemoryCache _templateCache;

        public ConvertDataTemplateCollectionProviderFactory(
            IOptions<TemplateHostingConfiguration> templateHostingConfiguration,
            IOptions<TemplateCollectionConfiguration> templateCollectionConfiguration,
            IMemoryCache templateCache)
        {
            _templateHostingConfiguration = EnsureArg.IsNotNull(templateHostingConfiguration?.Value, nameof(templateHostingConfiguration));
            _templateCollectionConfiguration = EnsureArg.IsNotNull(templateCollectionConfiguration?.Value, nameof(templateCollectionConfiguration));
            _templateCache = EnsureArg.IsNotNull(templateCache, nameof(templateCache));
        }

        public IConvertDataTemplateCollectionProvider CreateTemplateCollectionProvider()
        {
            var cloudStorageConfig = _templateHostingConfiguration.CloudStorageConfiguration;

            return cloudStorageConfig.Provider switch
            {
                CloudStorageProvider.Local => CreateLocalProvider(),
                CloudStorageProvider.Azure => CreateAzureProvider(cloudStorageConfig.Azure),
                CloudStorageProvider.Gcp => CreateGcpProvider(cloudStorageConfig.Gcp),
                _ => throw new ArgumentException($"Unsupported cloud storage provider: {cloudStorageConfig.Provider}")
            };
        }

        private IConvertDataTemplateCollectionProvider CreateLocalProvider()
        {
            return new DefaultTemplateCollectionProvider(_templateCache, _templateCollectionConfiguration);
        }

        private IConvertDataTemplateCollectionProvider CreateAzureProvider(AzureStorageConfiguration azureConfig)
        {
            EnsureArg.IsNotNull(azureConfig, nameof(azureConfig));

            var blobServiceClient = new BlobServiceClient(
                new Uri($"https://{azureConfig.StorageAccountName}.{azureConfig.EndpointSuffix}"),
                new DefaultAzureCredential());

            var containerClient = blobServiceClient.GetBlobContainerClient(azureConfig.ContainerName);

            return new BlobTemplateCollectionProvider(containerClient, _templateCache, _templateCollectionConfiguration);
        }

        private IConvertDataTemplateCollectionProvider CreateGcpProvider(GcpStorageConfiguration gcpConfig)
        {
            EnsureArg.IsNotNull(gcpConfig, nameof(gcpConfig));

            // For testing, we'll skip creating a real StorageClient if credentials aren't available
            StorageClient storageClient;
            try
            {
                storageClient = StorageClient.Create();
            }
            catch (InvalidOperationException)
            {
                // If no credentials are available, throw a more descriptive error
                throw new InvalidOperationException(
                    "GCP credentials not found. Please set up Application Default Credentials or use a different provider.");
            }

            return new GcpStorageTemplateCollectionProvider(
                storageClient,
                gcpConfig.BucketName,
                gcpConfig.ContainerName,
                _templateCache,
                _templateCollectionConfiguration);
        }
    }
}
