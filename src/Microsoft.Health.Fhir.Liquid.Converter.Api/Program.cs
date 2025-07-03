// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Health.Fhir.Liquid.Converter.Api.Services;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.TemplateManagement.ArtifactProviders;
using Microsoft.Health.Fhir.TemplateManagement.Configurations;

namespace Microsoft.Health.Fhir.Liquid.Converter.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Performance optimizations for streaming scenarios
            ConfigurePerformanceSettings();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            // Add response compression for better performance
            builder.Services.AddResponseCompression();

            // Configure memory cache with optimized settings
            builder.Services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1024; // 1GB limit
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
            });

            // Add health checks
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy());

            // Add Swagger/OpenAPI support
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "FHIR Converter API",
                    Version = "v1",
                    Description = "High-performance FHIR data format conversion API"
                });

                // Include XML comments if available
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            // Configure services
            builder.Services.Configure<ProcessorSettings>(
                builder.Configuration.GetSection("ProcessorSettings"));

            builder.Services.Configure<TemplateCollectionConfiguration>(
                builder.Configuration.GetSection("TemplateCollectionConfiguration"));

            builder.Services.Configure<TemplateHostingConfiguration>(
                builder.Configuration.GetSection("TemplateHosting"));

            // Register template collection provider
            builder.Services.AddSingleton<IConvertDataTemplateCollectionProvider, DefaultTemplateCollectionProvider>();

            // Register conversion service
            builder.Services.AddScoped<IConversionService, ConversionService>();

            // Configure HTTP client with optimized settings
            builder.Services.AddHttpClient("TemplateClient")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    MaxConnectionsPerServer = 100,
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
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

            // Enable response compression
            app.UseResponseCompression();

            app.UseHttpsRedirection();
            app.UseAuthorization();

            // Configure endpoints
            app.MapControllers();
            app.MapHealthChecks("/health/check");

            // Add performance monitoring endpoint
            app.MapGet("/metrics", () =>
            {
                var metrics = new
                {
                    timestamp = DateTime.UtcNow,
                    uptime = Environment.TickCount64,
                    memory = GC.GetTotalMemory(false),
                    gc_collections = new
                    {
                        gen0 = GC.CollectionCount(0),
                        gen1 = GC.CollectionCount(1),
                        gen2 = GC.CollectionCount(2)
                    },
                    threads = System.Diagnostics.Process.GetCurrentProcess().Threads.Count
                };
                return Results.Json(metrics);
            });

            app.Run();
        }

        private static void ConfigurePerformanceSettings()
        {
            // Optimize thread pool for high concurrency
            ThreadPool.SetMinThreads(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2);
            ThreadPool.SetMaxThreads(Environment.ProcessorCount * 8, Environment.ProcessorCount * 8);

            // Optimize GC for high-throughput scenarios
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            
            // Set GC latency mode for better response times
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            }
        }
    }
} 