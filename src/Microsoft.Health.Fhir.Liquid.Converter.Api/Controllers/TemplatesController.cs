// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.TemplateManagement.ArtifactProviders;

namespace Microsoft.Health.Fhir.Liquid.Converter.Api.Controllers
{
    /// <summary>
    /// Controller for template operations
    /// </summary>
    [ApiController]
    [Route("api/v1/templates")]
    [Produces("application/json")]
    public class TemplatesController : ControllerBase
    {
        private readonly IConvertDataTemplateCollectionProvider _templateProvider;
        private readonly ILogger<TemplatesController> _logger;

        public TemplatesController(IConvertDataTemplateCollectionProvider templateProvider, ILogger<TemplatesController> logger)
        {
            _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the list of available templates
        /// </summary>
        /// <returns>List of available templates</returns>
        /// <response code="200">Templates retrieved successfully</response>
        /// <response code="500">Failed to retrieve templates</response>
        [HttpGet]
        [ProducesResponseType(typeof(TemplatesResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<TemplatesResponse>> GetTemplatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Template list requested");

                var templates = await _templateProvider.GetTemplateCollectionAsync(cancellationToken);

                var templateList = new List<TemplateInfo>();

                foreach (var templateCollection in templates)
                {
                    foreach (var template in templateCollection)
                    {
                        templateList.Add(new TemplateInfo
                        {
                            Name = template.Key,
                            Type = "Liquid",
                            Description = $"Template for {template.Key}",
                            LastModified = DateTime.UtcNow // Template provider doesn't expose modification time
                        });
                    }
                }

                var response = new TemplatesResponse
                {
                    Templates = templateList,
                    TotalCount = templateList.Count,
                    RetrievedAt = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve templates");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Failed to retrieve templates",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }
    }

    /// <summary>
    /// Response model for template list
    /// </summary>
    public class TemplatesResponse
    {
        /// <summary>
        /// List of available templates
        /// </summary>
        public List<TemplateInfo> Templates { get; set; } = new();

        /// <summary>
        /// Total number of templates
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// When the templates were retrieved
        /// </summary>
        public DateTime RetrievedAt { get; set; }
    }

    /// <summary>
    /// Information about a template
    /// </summary>
    public class TemplateInfo
    {
        /// <summary>
        /// Template name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Template type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Template description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// When the template was last modified
        /// </summary>
        public DateTime LastModified { get; set; }
    }
} 