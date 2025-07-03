// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Microsoft.Health.Fhir.Liquid.Converter.Api.Models
{
    /// <summary>
    /// Response model for FHIR conversion operations
    /// </summary>
    public class ConversionResponse
    {
        /// <summary>
        /// The converted result
        /// </summary>
        [JsonPropertyName("result")]
        public object Result { get; set; } = new();

        /// <summary>
        /// Trace information if requested
        /// </summary>
        [JsonPropertyName("traceInfo")]
        public object? TraceInfo { get; set; }

        /// <summary>
        /// Conversion metadata
        /// </summary>
        [JsonPropertyName("metadata")]
        public ConversionMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Metadata about the conversion operation
    /// </summary>
    public class ConversionMetadata
    {
        /// <summary>
        /// The input data format that was converted
        /// </summary>
        [JsonPropertyName("inputDataFormat")]
        public string InputDataFormat { get; set; } = string.Empty;

        /// <summary>
        /// The template used for conversion
        /// </summary>
        [JsonPropertyName("templateUsed")]
        public string TemplateUsed { get; set; } = string.Empty;

        /// <summary>
        /// The timestamp when the conversion was performed
        /// </summary>
        [JsonPropertyName("conversionTimestamp")]
        public DateTime ConversionTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The duration of the conversion in milliseconds
        /// </summary>
        [JsonPropertyName("conversionDurationMs")]
        public long ConversionDurationMs { get; set; }
    }
} 