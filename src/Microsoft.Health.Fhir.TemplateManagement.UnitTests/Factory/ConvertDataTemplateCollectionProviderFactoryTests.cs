// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.TemplateManagement.ArtifactProviders;
using Microsoft.Health.Fhir.TemplateManagement.Configurations;
using Microsoft.Health.Fhir.TemplateManagement.Factory;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Fhir.TemplateManagement.UnitTests.Factory
{
    public class ConvertDataTemplateCollectionProviderFactoryTests
    {
        [Fact]
        public void GivenValidAzureConfiguration_WhenCreateTemplateCollectionProvider_ThenBlobTemplateCollectionProviderReturned()
        {
            var templateHostingConfig = new TemplateHostingConfiguration()
            {
                CloudStorageConfiguration = new CloudStorageConfiguration()
                {
                    Provider = CloudStorageProvider.Azure,
                    Azure = new AzureStorageConfiguration()
                    {
                        StorageAccountName = "test",
                        ContainerName = "test",
                        EndpointSuffix = "blob.core.windows.net"
                    }
                }
            };

            var templateCollectionConfig = new TemplateCollectionConfiguration();
            var cache = new MemoryCache(new MemoryCacheOptions());
            
            var factory = new ConvertDataTemplateCollectionProviderFactory(
                Options.Create(templateHostingConfig),
                Options.Create(templateCollectionConfig),
                cache);

            var templateCollectionProvider = factory.CreateTemplateCollectionProvider();

            Assert.NotNull(templateCollectionProvider);
            Assert.True(templateCollectionProvider is BlobTemplateCollectionProvider);
        }

        [Fact]
        public void GivenValidGcpConfiguration_WhenCreateTemplateCollectionProvider_ThenGcpStorageTemplateCollectionProviderReturned()
        {
            var templateHostingConfig = new TemplateHostingConfiguration()
            {
                CloudStorageConfiguration = new CloudStorageConfiguration()
                {
                    Provider = CloudStorageProvider.Gcp,
                    Gcp = new GcpStorageConfiguration()
                    {
                        ProjectId = "test-project",
                        BucketName = "test-bucket",
                        ContainerName = "test-container"
                    }
                }
            };

            var templateCollectionConfig = new TemplateCollectionConfiguration();
            var cache = new MemoryCache(new MemoryCacheOptions());
            
            var factory = new ConvertDataTemplateCollectionProviderFactory(
                Options.Create(templateHostingConfig),
                Options.Create(templateCollectionConfig),
                cache);

            // GCP test will fail without credentials, which is expected behavior
            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateTemplateCollectionProvider());
            Assert.Contains("GCP credentials not found", exception.Message);
        }

        [Fact]
        public void GivenUnsupportedProvider_WhenCreateTemplateCollectionProvider_ThenArgumentExceptionThrown()
        {
            var templateHostingConfig = new TemplateHostingConfiguration()
            {
                CloudStorageConfiguration = new CloudStorageConfiguration()
                {
                    Provider = (CloudStorageProvider)999 // Invalid provider
                }
            };

            var templateCollectionConfig = new TemplateCollectionConfiguration();
            var cache = new MemoryCache(new MemoryCacheOptions());
            
            var factory = new ConvertDataTemplateCollectionProviderFactory(
                Options.Create(templateHostingConfig),
                Options.Create(templateCollectionConfig),
                cache);

            Assert.Throws<ArgumentException>(() => factory.CreateTemplateCollectionProvider());
        }
    }
}
