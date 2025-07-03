// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Microsoft.Health.Fhir.Liquid.Converter.Api.Models
{
    /// <summary>
    /// Health check response model
    /// </summary>
    public class HealthResponse
    {
        /// <summary>
        /// Overall health status
        /// </summary>
        [JsonPropertyName("overallStatus")]
        public string OverallStatus { get; set; } = string.Empty;

        /// <summary>
        /// Health check details
        /// </summary>
        [JsonPropertyName("details")]
        public List<HealthCheckDetail> Details { get; set; } = new();

        /// <summary>
        /// Timestamp of the health check
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Individual health check detail
    /// </summary>
    public class HealthCheckDetail
    {
        /// <summary>
        /// Name of the health check
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Status of the health check
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Description of the health check
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Additional data for the health check
        /// </summary>
        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
} 