// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Microsoft.Health.Fhir.Liquid.Converter.Api.Models
{
    /// <summary>
    /// Request model for FHIR conversion operations
    /// </summary>
    public class ConversionRequest
    {
        /// <summary>
        /// The format of the input data
        /// </summary>
        [Required]
        [JsonPropertyName("inputDataFormat")]
        public string InputDataFormat { get; set; } = string.Empty;

        /// <summary>
        /// The name of the root template to use for conversion
        /// </summary>
        [JsonPropertyName("rootTemplateName")]
        public string? RootTemplateName { get; set; }

        /// <summary>
        /// The input data content as a string
        /// </summary>
        [Required]
        [JsonPropertyName("inputDataString")]
        public string InputDataString { get; set; } = string.Empty;

        /// <summary>
        /// Whether to include trace information in the response
        /// </summary>
        [JsonPropertyName("includeTraceInfo")]
        public bool IncludeTraceInfo { get; set; } = false;
    }
} 