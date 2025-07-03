// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Liquid.Converter.Api.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Api.Services;

namespace Microsoft.Health.Fhir.Liquid.Converter.Api.Controllers
{
    /// <summary>
    /// Controller for FHIR conversion operations
    /// </summary>
    [ApiController]
    [Route("api/v1/convert")]
    [Produces("application/json")]
    public class ConvertController : ControllerBase
    {
        private readonly IConversionService _conversionService;
        private readonly ILogger<ConvertController> _logger;

        public ConvertController(IConversionService conversionService, ILogger<ConvertController> logger)
        {
            _conversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Converts HL7v2 data to FHIR R4
        /// </summary>
        /// <param name="request">The conversion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The converted FHIR R4 bundle</returns>
        /// <response code="200">Conversion successful</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Conversion failed</response>
        [HttpPost("hl7v2-to-fhir")]
        [ProducesResponseType(typeof(ConversionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<ConversionResponse>> ConvertHl7v2ToFhir(
            [FromBody] ConversionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Received HL7v2 to FHIR conversion request");

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _conversionService.ConvertHl7v2ToFhirAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HL7v2 to FHIR conversion failed");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Conversion failed",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        /// <summary>
        /// Converts C-CDA data to FHIR R4
        /// </summary>
        /// <param name="request">The conversion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The converted FHIR R4 bundle</returns>
        /// <response code="200">Conversion successful</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Conversion failed</response>
        [HttpPost("ccda-to-fhir")]
        [ProducesResponseType(typeof(ConversionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<ConversionResponse>> ConvertCcdaToFhir(
            [FromBody] ConversionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Received C-CDA to FHIR conversion request");

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _conversionService.ConvertCcdaToFhirAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "C-CDA to FHIR conversion failed");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Conversion failed",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        /// <summary>
        /// Converts JSON data to FHIR R4
        /// </summary>
        /// <param name="request">The conversion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The converted FHIR R4 bundle</returns>
        /// <response code="200">Conversion successful</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Conversion failed</response>
        [HttpPost("json-to-fhir")]
        [ProducesResponseType(typeof(ConversionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<ConversionResponse>> ConvertJsonToFhir(
            [FromBody] ConversionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Received JSON to FHIR conversion request");

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _conversionService.ConvertJsonToFhirAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON to FHIR conversion failed");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Conversion failed",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        /// <summary>
        /// Converts FHIR STU3 data to FHIR R4
        /// </summary>
        /// <param name="request">The conversion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The converted FHIR R4 bundle</returns>
        /// <response code="200">Conversion successful</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Conversion failed</response>
        [HttpPost("fhir-stu3-to-r4")]
        [ProducesResponseType(typeof(ConversionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<ConversionResponse>> ConvertFhirStu3ToR4(
            [FromBody] ConversionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Received FHIR STU3 to R4 conversion request");

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _conversionService.ConvertFhirStu3ToR4Async(request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FHIR STU3 to R4 conversion failed");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Conversion failed",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        /// <summary>
        /// Converts FHIR R4 data to HL7v2
        /// </summary>
        /// <param name="request">The conversion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The converted HL7v2 message</returns>
        /// <response code="200">Conversion successful</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Conversion failed</response>
        [HttpPost("fhir-to-hl7v2")]
        [ProducesResponseType(typeof(ConversionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<ConversionResponse>> ConvertFhirToHl7v2(
            [FromBody] ConversionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Received FHIR to HL7v2 conversion request");

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _conversionService.ConvertFhirToHl7v2Async(request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FHIR to HL7v2 conversion failed");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Conversion failed",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }
    }
} 