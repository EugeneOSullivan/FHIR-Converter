// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Liquid.Converter.Api.Models;

namespace Microsoft.Health.Fhir.Liquid.Converter.Api.Controllers
{
    /// <summary>
    /// Controller for health check operations
    /// </summary>
    [ApiController]
    [Route("api/v1/health")]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the health status of the service
        /// </summary>
        /// <returns>The health status</returns>
        /// <response code="200">Service is healthy</response>
        /// <response code="503">Service is unhealthy</response>
        [HttpGet("check")]
        [ProducesResponseType(typeof(HealthResponse), 200)]
        [ProducesResponseType(typeof(HealthResponse), 503)]
        public async Task<ActionResult<HealthResponse>> GetHealthAsync()
        {
            try
            {
                _logger.LogDebug("Health check requested");

                var healthReport = await _healthCheckService.CheckHealthAsync();

                var response = new HealthResponse
                {
                    OverallStatus = healthReport.Status.ToString(),
                    Details = healthReport.Entries.Select(entry => new HealthCheckDetail
                    {
                        Name = entry.Key,
                        Status = entry.Value.Status.ToString(),
                        Description = entry.Value.Description ?? string.Empty,
                        Data = entry.Value.Data
                    }).ToList(),
                    Timestamp = DateTime.UtcNow
                };

                return healthReport.Status == HealthStatus.Healthy ? Ok(response) : StatusCode(503, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                
                var response = new HealthResponse
                {
                    OverallStatus = "Unhealthy",
                    Details = new List<HealthCheckDetail>
                    {
                        new HealthCheckDetail
                        {
                            Name = "HealthCheck",
                            Status = "Unhealthy",
                            Description = "Health check service failed",
                            Data = new { Error = ex.Message }
                        }
                    },
                    Timestamp = DateTime.UtcNow
                };

                return StatusCode(503, response);
            }
        }
    }
} 