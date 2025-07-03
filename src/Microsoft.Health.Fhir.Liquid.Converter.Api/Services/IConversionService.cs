// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Liquid.Converter.Api.Models;

namespace Microsoft.Health.Fhir.Liquid.Converter.Api.Services
{
    /// <summary>
    /// Service interface for FHIR conversion operations
    /// </summary>
    public interface IConversionService
    {
        /// <summary>
        /// Converts HL7v2 data to FHIR R4
        /// </summary>
        /// <param name="request">The conversion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The conversion response</returns>
        Task<ConversionResponse> ConvertHl7v2ToFhirAsync(ConversionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts C-CDA data to FHIR R4
        /// </summary>
        /// <param name="request">The conversion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The conversion response</returns>
        Task<ConversionResponse> ConvertCcdaToFhirAsync(ConversionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts JSON data to FHIR R4
        /// </summary>
        /// <param name="request">The conversion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The conversion response</returns>
        Task<ConversionResponse> ConvertJsonToFhirAsync(ConversionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts FHIR STU3 data to FHIR R4
        /// </summary>
        /// <param name="request">The conversion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The conversion response</returns>
        Task<ConversionResponse> ConvertFhirStu3ToR4Async(ConversionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts FHIR R4 data to HL7v2
        /// </summary>
        /// <param name="request">The conversion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The conversion response</returns>
        Task<ConversionResponse> ConvertFhirToHl7v2Async(ConversionRequest request, CancellationToken cancellationToken = default);
    }
} 