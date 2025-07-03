// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotLiquid;
using EnsureThat;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;
using Microsoft.Health.Fhir.TemplateManagement.Configurations;
using Microsoft.Health.Fhir.TemplateManagement.Exceptions;
using Microsoft.Health.Fhir.TemplateManagement.Models;
using Polly;
using Polly.Retry;

namespace Microsoft.Health.Fhir.TemplateManagement.ArtifactProviders
{
    /// <summary>
    /// GCP Storage template collection provider for retrieving templates from Google Cloud Storage
    /// </summary>
    public class GcpStorageTemplateCollectionProvider : IConvertDataTemplateCollectionProvider
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly string _containerName;
        private readonly IMemoryCache _templateCache;
        private readonly TemplateCollectionConfiguration _templateCollectionConfiguration;
        private readonly string _gcpTemplateCacheKey = "cached-gcp-templates";
        private readonly int _segmentSize = 100;
        private readonly AsyncRetryPolicy _downloadRetryPolicy;
        private readonly int _maxParallelism = 50;
        private readonly int _maxTemplateCollectionSizeInBytes;

        public GcpStorageTemplateCollectionProvider(
            StorageClient storageClient,
            string bucketName,
            string containerName,
            IMemoryCache templateCache,
            TemplateCollectionConfiguration templateConfiguration)
        {
            _storageClient = EnsureArg.IsNotNull(storageClient, nameof(storageClient));
            _bucketName = EnsureArg.IsNotNullOrWhiteSpace(bucketName, nameof(bucketName));
            _containerName = containerName;
            _templateCache = EnsureArg.IsNotNull(templateCache, nameof(templateCache));
            _templateCollectionConfiguration = EnsureArg.IsNotNull(templateConfiguration, nameof(templateConfiguration));

            _downloadRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(10));

            _maxTemplateCollectionSizeInBytes = _templateCollectionConfiguration.TemplateCollectionSizeLimitMegabytes * 1024 * 1024;
        }

        public async Task<List<Dictionary<string, Template>>> GetTemplateCollectionAsync(CancellationToken cancellationToken = default)
        {
            if (_templateCache.TryGetValue(_gcpTemplateCacheKey, out var cachedTemplates))
            {
                return (List<Dictionary<string, Template>>)cachedTemplates;
            }

            var templateCollection = new List<Dictionary<string, Template>>();
            var templateDictionary = new Dictionary<string, Template>();
            var totalSize = 0L;

            try
            {
                // List all objects in the bucket/container
                var objects = _storageClient.ListObjects(_bucketName, _containerName);
                var objectList = objects.ToList();

                if (!objectList.Any())
                {
                    _templateCache.Set(_gcpTemplateCacheKey, templateCollection);
                    return templateCollection;
                }

                // Process objects in parallel with retry policy
                var semaphore = new SemaphoreSlim(_maxParallelism);
                var tasks = new List<Task>();

                foreach (var storageObject in objectList)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    tasks.Add(ProcessStorageObjectAsync(storageObject, templateDictionary, semaphore, cancellationToken));
                }

                await Task.WhenAll(tasks);

                // Check total size
                if (totalSize > _maxTemplateCollectionSizeInBytes)
                {
                    throw new TemplateCollectionExceedsSizeLimitException(
                        TemplateManagementErrorCode.GcpTemplateCollectionTooLarge,
                        $"Template collection size ({totalSize} bytes) exceeds the limit ({_maxTemplateCollectionSizeInBytes} bytes)");
                }

                templateCollection.Add(templateDictionary);
                _templateCache.Set(_gcpTemplateCacheKey, templateCollection);
                return templateCollection;
            }
            catch (Exception ex) when (ex is not TemplateCollectionExceedsSizeLimitException)
            {
                throw new TemplateManagementException(
                    TemplateManagementErrorCode.GcpTemplateCollectionError,
                    $"Failed to retrieve templates from GCP Storage bucket '{_bucketName}'",
                    ex);
            }
        }

        private async Task ProcessStorageObjectAsync(
            Google.Apis.Storage.v1.Data.Object storageObject,
            Dictionary<string, Template> templateDictionary,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await _downloadRetryPolicy.ExecuteAsync(async () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    // Skip if not a template file
                    if (!IsTemplateFile(storageObject.Name))
                    {
                        return;
                    }

                    // Download the object content
                    using var stream = new MemoryStream();
                    await _storageClient.DownloadObjectAsync(_bucketName, storageObject.Name, stream, cancellationToken: cancellationToken);

                    stream.Position = 0;
                    var content = Encoding.UTF8.GetString(stream.ToArray());

                    // Create template
                    var templateName = GetTemplateName(storageObject.Name);
                    var template = Template.Parse(content);
                    templateDictionary[templateName] = template;
                });
            }
            finally
            {
                semaphore.Release();
            }
        }

        private bool IsTemplateFile(string objectName)
        {
            return objectName.EndsWith(".liquid", StringComparison.OrdinalIgnoreCase);
        }

        private string GetTemplateName(string objectName)
        {
            // Remove container prefix if present
            var templateName = objectName;
            if (!string.IsNullOrEmpty(_containerName) && objectName.StartsWith(_containerName))
            {
                templateName = objectName.Substring(_containerName.Length).TrimStart('/');
            }

            // Remove .liquid extension
            if (templateName.EndsWith(".liquid", StringComparison.OrdinalIgnoreCase))
            {
                templateName = templateName.Substring(0, templateName.Length - 7);
            }

            return templateName;
        }
    }
} 