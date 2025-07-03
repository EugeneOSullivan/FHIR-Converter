// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Health.Fhir.Liquid.Converter.Api.Services;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.TemplateManagement.ArtifactProviders;
using Microsoft.Health.Fhir.TemplateManagement.Configurations;
using Microsoft.Health.Fhir.TemplateManagement.Factory;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FHIR Converter API",
        Version = "v1",
        Description = "API for converting healthcare data between different formats using FHIR standards",
        Contact = new OpenApiContact
        {
            Name = "FHIR Converter Team",
            Email = "fhir-converter@microsoft.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add API version parameter
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key for authentication"
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "self" });

// Add memory cache
builder.Services.AddMemoryCache();

// Configure options
builder.Services.Configure<ProcessorSettings>(builder.Configuration.GetSection("ProcessorSettings"));
builder.Services.Configure<TemplateCollectionConfiguration>(builder.Configuration.GetSection("TemplateCollectionConfiguration"));

// Add template provider factory
builder.Services.AddSingleton<ConvertDataTemplateCollectionProviderFactory>();

// Add template provider
builder.Services.AddSingleton<IConvertDataTemplateCollectionProvider>(provider =>
{
    var factory = provider.GetRequiredService<ConvertDataTemplateCollectionProviderFactory>();
    return factory.CreateTemplateCollectionProvider();
});

// Add conversion service
builder.Services.AddScoped<IConversionService, ConversionService>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FHIR Converter API v1");
        c.RoutePrefix = "api/v1/swagger";
    });
}

// Add CORS
app.UseCors();

// Add routing
app.UseRouting();

// Add health checks endpoint
app.MapHealthChecks("/health/check");

// Add controllers
app.MapControllers();

// Add global exception handler
app.UseExceptionHandler("/error");

app.Run(); 