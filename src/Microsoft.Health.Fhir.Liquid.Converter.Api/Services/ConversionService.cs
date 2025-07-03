// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Liquid.Converter;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Models.Hl7v2;
using Microsoft.Health.Fhir.Liquid.Converter.Models.Json;
using Microsoft.Health.Fhir.Liquid.Converter.Processors;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;
using Microsoft.Health.Fhir.TemplateManagement.ArtifactProviders;
using Microsoft.Health.Fhir.TemplateManagement.Configurations;
using Microsoft.Health.Fhir.Liquid.Converter.Api.Models;

namespace Microsoft.Health.Fhir.Liquid.Converter.Api.Services
{
    /// <summary>
    /// Service implementation for FHIR conversion operations
    /// </summary>
    public class ConversionService : IConversionService
    {
        private readonly ILogger<ConversionService> _logger;
        private readonly IConvertDataTemplateCollectionProvider _templateCollectionProvider;
        private readonly ProcessorSettings _processorSettings;

        public ConversionService(
            ILogger<ConversionService> logger,
            IConvertDataTemplateCollectionProvider templateCollectionProvider,
            IOptions<ProcessorSettings> processorSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templateCollectionProvider = templateCollectionProvider ?? throw new ArgumentNullException(nameof(templateCollectionProvider));
            _processorSettings = processorSettings?.Value ?? new ProcessorSettings();
        }

        public async Task<ConversionResponse> ConvertHl7v2ToFhirAsync(ConversionRequest request, CancellationToken cancellationToken = default)
        {
            return await ConvertAsync(request, DataType.Hl7v2, cancellationToken);
        }

        public async Task<ConversionResponse> ConvertCcdaToFhirAsync(ConversionRequest request, CancellationToken cancellationToken = default)
        {
            return await ConvertAsync(request, DataType.Ccda, cancellationToken);
        }

        public async Task<ConversionResponse> ConvertJsonToFhirAsync(ConversionRequest request, CancellationToken cancellationToken = default)
        {
            return await ConvertAsync(request, DataType.Json, cancellationToken);
        }

        public async Task<ConversionResponse> ConvertFhirStu3ToR4Async(ConversionRequest request, CancellationToken cancellationToken = default)
        {
            return await ConvertAsync(request, DataType.Fhir, cancellationToken);
        }

        public async Task<ConversionResponse> ConvertFhirToHl7v2Async(ConversionRequest request, CancellationToken cancellationToken = default)
        {
            return await ConvertAsync(request, DataType.Fhir, cancellationToken);
        }

        private async Task<ConversionResponse> ConvertAsync(ConversionRequest request, DataType dataType, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting conversion from {DataType} to FHIR", dataType);

                // Create the appropriate processor
                var processor = CreateProcessor(dataType);

                // Determine the root template name
                var rootTemplate = string.IsNullOrEmpty(request.RootTemplateName)
                    ? GetDefaultTemplate(dataType)
                    : request.RootTemplateName;

                // Create trace info if requested
                var traceInfo = request.IncludeTraceInfo ? CreateTraceInfo(dataType) : null;

                // Get template collection and create template provider
                var templateCollection = await _templateCollectionProvider.GetTemplateCollectionAsync(cancellationToken);
                var templateProvider = new TemplateProvider(templateCollection, isDefaultTemplateProvider: _templateCollectionProvider is DefaultTemplateCollectionProvider);

                // Perform the conversion
                var result = await Task.Run(() =>
                    processor.Convert(request.InputDataString, rootTemplate, templateProvider, traceInfo),
                    cancellationToken);

                stopwatch.Stop();

                // Parse the result based on the conversion type
                var parsedResult = ParseResult(result, dataType);

                return new ConversionResponse
                {
                    Result = parsedResult,
                    TraceInfo = traceInfo,
                    Metadata = new ConversionMetadata
                    {
                        InputDataFormat = request.InputDataFormat,
                        TemplateUsed = rootTemplate,
                        ConversionTimestamp = DateTime.UtcNow,
                        ConversionDurationMs = stopwatch.ElapsedMilliseconds
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Conversion failed for {DataType} after {Duration}ms", dataType, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private IFhirConverter CreateProcessor(DataType dataType)
        {
            return dataType switch
            {
                DataType.Hl7v2 => new Hl7v2Processor(_processorSettings, FhirConverterLogging.CreateLogger<Hl7v2Processor>()),
                DataType.Ccda => new CcdaProcessor(_processorSettings, FhirConverterLogging.CreateLogger<CcdaProcessor>()),
                DataType.Json => new JsonProcessor(_processorSettings, FhirConverterLogging.CreateLogger<JsonProcessor>()),
                DataType.Fhir => new FhirToHl7v2Processor(_processorSettings, FhirConverterLogging.CreateLogger<FhirToHl7v2Processor>()),
                _ => throw new ArgumentException($"Unsupported data type: {dataType}")
            };
        }

        private string GetDefaultTemplate(DataType dataType)
        {
            return dataType switch
            {
                DataType.Hl7v2 => "ADT_A01",
                DataType.Ccda => "CCD",
                DataType.Json => "ExamplePatient",
                DataType.Fhir => "BundleToHL7v2",
                _ => throw new ArgumentException($"Unsupported data type: {dataType}")
            };
        }

        private TraceInfo CreateTraceInfo(DataType dataType)
        {
            return dataType switch
            {
                DataType.Hl7v2 => new Hl7v2TraceInfo(),
                DataType.Ccda => new TraceInfo(),
                DataType.Json => new JSchemaTraceInfo(),
                DataType.Fhir => new JSchemaTraceInfo(),
                _ => throw new ArgumentException($"Unsupported data type: {dataType}")
            };
        }

        private object ParseResult(string result, DataType dataType)
        {
            // For FHIR conversions, parse as JSON
            if (dataType != DataType.Fhir) // FHIR to HL7v2 conversion
            {
                try
                {
                    return JsonSerializer.Deserialize<object>(result) ?? result;
                }
                catch
                {
                    return result;
                }
            }

            // For HL7v2 output, return as string
            return result;
        }
    }
} 