// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
    public class ConvertDataTemplateCollectionProviderFactory : IConvertDataTemplateCollectionProviderFactory
    {
        private readonly IMemoryCache _memoryCache;
        private readonly TemplateCollectionConfiguration _templateCollectionConfiguration;

        private readonly string _defaultTemplateProviderCacheKey = "default-template-provider";
        private readonly string _storageTemplateProviderCachePrefix = "storage-template-provider-";

        public ConvertDataTemplateCollectionProviderFactory(IMemoryCache memoryCache, IOptions<TemplateCollectionConfiguration> templateCollectionConfiguration)
        {
            _memoryCache = EnsureArg.IsNotNull(memoryCache, nameof(memoryCache));
            _templateCollectionConfiguration = EnsureArg.IsNotNull(templateCollectionConfiguration?.Value, nameof(templateCollectionConfiguration));
        }

        /// <summary>
        /// Returns the appropriate template collection provider based on the configuration.
        /// Supports Azure Blob Storage, GCP Storage, and default templates.
        /// </summary>
        /// <returns>Returns a template collection provider based on the configuration.</returns>
        public IConvertDataTemplateCollectionProvider CreateTemplateCollectionProvider()
        {
            var templateHostingConfiguration = _templateCollectionConfiguration.TemplateHostingConfiguration;

            // Check for cloud storage configuration first (new approach)
            if (templateHostingConfiguration?.CloudStorageConfiguration != null)
            {
                var cloudConfig = templateHostingConfiguration.CloudStorageConfiguration;
                var providerConfig = cloudConfig.GetProviderConfiguration();

                return cloudConfig.Provider switch
                {
                    CloudProviderType.Azure => CreateBlobTemplateCollectionProvider(providerConfig),
                    CloudProviderType.GCP => CreateGcpStorageTemplateCollectionProvider(providerConfig),
                    _ => throw new ArgumentException($"Unsupported cloud provider: {cloudConfig.Provider}")
                };
            }

            // Fallback to legacy Azure configuration
            if (templateHostingConfiguration?.StorageAccountConfiguration?.ContainerUrl != null)
            {
                return CreateBlobTemplateCollectionProvider(templateHostingConfiguration.StorageAccountConfiguration);
            }

            return CreateDefaultTemplateCollectionProvider();
        }

        /// <summary>
        /// Returns the default template collection provider, i.e., template provider that references the default templates packaged within the project.
        /// </summary>
        /// <returns>Returns the default template collection provider, <see cref="DefaultTemplateCollectionProvider">.</returns>
        private IConvertDataTemplateCollectionProvider CreateDefaultTemplateCollectionProvider()
        {
            var cacheKey = _defaultTemplateProviderCacheKey;
            if (_memoryCache.TryGetValue(cacheKey, out var templateProviderCache))
            {
                return (IConvertDataTemplateCollectionProvider)templateProviderCache;
            }

            var templateProvider = new DefaultTemplateCollectionProvider(_memoryCache, _templateCollectionConfiguration);
            _memoryCache.Set(cacheKey, templateProvider);
            return templateProvider;
        }

        /// <summary>
        /// Returns a blob template collection provider for Azure Storage.
        /// </summary>
        /// <param name="storageConfiguration">Storage configuration containing information of the blob container to load the templates from.</param>
        /// <returns>Returns a blob template collection provider, <see cref="BlobTemplateCollectionProvider">.</returns>
        private IConvertDataTemplateCollectionProvider CreateBlobTemplateCollectionProvider(IStorageConfiguration storageConfiguration)
        {
            EnsureArg.IsNotNull(storageConfiguration, nameof(storageConfiguration));

            TokenCredential tokenCredential = new DefaultAzureCredential();
            var containerUrl = new Uri(storageConfiguration.GetContainerUrl());
            var blobContainerClient = new BlobContainerClient(containerUrl, tokenCredential);

            var cacheKey = _storageTemplateProviderCachePrefix + blobContainerClient.Name;
            if (_memoryCache.TryGetValue(cacheKey, out var templateProviderCache))
            {
                return (IConvertDataTemplateCollectionProvider)templateProviderCache;
            }

            var templateProvider = new BlobTemplateCollectionProvider(blobContainerClient, _memoryCache, _templateCollectionConfiguration);
            _memoryCache.Set(cacheKey, templateProvider);
            return templateProvider;
        }

        /// <summary>
        /// Returns a blob template collection provider for legacy Azure Storage configuration.
        /// </summary>
        /// <param name="storageAccountConfiguration">Storage account configuration containing information of the blob container to load the templates from.</param>
        /// <returns>Returns a blob template collection provider, <see cref="BlobTemplateCollectionProvider">.</returns>
        private IConvertDataTemplateCollectionProvider CreateBlobTemplateCollectionProvider(StorageAccountConfiguration storageAccountConfiguration)
        {
            EnsureArg.IsNotNull(storageAccountConfiguration, nameof(storageAccountConfiguration));
            EnsureArg.IsNotNull(storageAccountConfiguration.ContainerUrl, nameof(storageAccountConfiguration.ContainerUrl));

            TokenCredential tokenCredential = new DefaultAzureCredential();
            var blobContainerClient = new BlobContainerClient(storageAccountConfiguration.ContainerUrl, tokenCredential);

            var cacheKey = _storageTemplateProviderCachePrefix + blobContainerClient.Name;
            if (_memoryCache.TryGetValue(cacheKey, out var templateProviderCache))
            {
                return (IConvertDataTemplateCollectionProvider)templateProviderCache;
            }

            var templateProvider = new BlobTemplateCollectionProvider(blobContainerClient, _memoryCache, _templateCollectionConfiguration);
            _memoryCache.Set(cacheKey, templateProvider);
            return templateProvider;
        }

        /// <summary>
        /// Returns a GCP Storage template collection provider.
        /// </summary>
        /// <param name="storageConfiguration">Storage configuration containing information of the GCS bucket to load the templates from.</param>
        /// <returns>Returns a GCP Storage template collection provider, <see cref="GcpStorageTemplateCollectionProvider">.</returns>
        private IConvertDataTemplateCollectionProvider CreateGcpStorageTemplateCollectionProvider(IStorageConfiguration storageConfiguration)
        {
            EnsureArg.IsNotNull(storageConfiguration, nameof(storageConfiguration));

            var gcpConfig = storageConfiguration as GcpStorageConfiguration;
            if (gcpConfig == null)
            {
                throw new ArgumentException("Storage configuration must be of type GcpStorageConfiguration for GCP provider");
            }

            var storageClient = StorageClient.Create();
            var cacheKey = _storageTemplateProviderCachePrefix + gcpConfig.BucketName + gcpConfig.ContainerName;
            
            if (_memoryCache.TryGetValue(cacheKey, out var templateProviderCache))
            {
                return (IConvertDataTemplateCollectionProvider)templateProviderCache;
            }

            var templateProvider = new GcpStorageTemplateCollectionProvider(
                storageClient, 
                gcpConfig.BucketName, 
                gcpConfig.ContainerName, 
                _memoryCache, 
                _templateCollectionConfiguration);
            
            _memoryCache.Set(cacheKey, templateProvider);
            return templateProvider;
        }
    }
}
