# FHIR Converter API

This project provides a RESTful API for converting healthcare data between HL7v2, C-CDA, JSON, FHIR STU3, and FHIR R4 formats using Liquid templates. It is containerized for easy deployment and local development with cloud-agnostic template storage.

---

## Architecture

### Core Components
- **ASP.NET Core 9.0 Web API**: Modern, high-performance API framework
- **Liquid Template Engine**: Flexible data transformation templates
- **Cloud-Agnostic Storage**: GCP GCS, Azure Blob Storage, or local templates
- **Health Checks**: Built-in health monitoring and readiness probes
- **Metrics**: Prometheus-compatible metrics endpoint
- **Containerization**: Multi-stage Docker builds with security scanning

### Supported Conversions
- **HL7v2** → FHIR R4
- **C-CDA** → FHIR R4  
- **JSON** → FHIR R4
- **FHIR STU3** → FHIR R4
- **FHIR R4** → HL7v2

---

## Project Structure

```
Microsoft.Health.Fhir.Liquid.Converter.Api/
├── Controllers/           # API endpoints
│   ├── ConvertController.cs      # Conversion operations
│   └── HealthController.cs       # Health checks
├── Models/                # Request/response models
│   ├── ConversionRequest.cs      # Conversion input models
│   └── ConversionResponse.cs     # Conversion output models
├── Services/              # Business logic
│   └── ConversionService.cs      # Core conversion service
├── Program.cs             # Application entry point
├── appsettings.json       # Configuration
├── Dockerfile             # Multi-stage container build
└── .dockerignore          # Docker build exclusions
```

---

## Quick Start

### Prerequisites
- [Docker](https://www.docker.com/products/docker-desktop) installed (optional)
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (for local development)

### Local Development (No Cloud Required)

#### Option 1: .NET CLI
```bash
# Set environment to development (uses local templates)
export ASPNETCORE_ENVIRONMENT=Development

# Run the API
dotnet run --project src/Microsoft.Health.Fhir.Liquid.Converter.Api
```

#### Option 2: Docker Compose
```bash
# Run with monitoring stack
docker-compose up fhir-converter-api

# Or run just the API
docker-compose up fhir-converter-api
```

#### Option 3: Docker Build
```bash
# Build the image
docker build -f src/Microsoft.Health.Fhir.Liquid.Converter.Api/Dockerfile -t fhir-converter-api .

# Run the container
docker run -p 8080:8080 --rm fhir-converter-api
```

### API Endpoints

#### Health Check
```bash
curl http://localhost:5000/health
```

#### Convert HL7v2 to FHIR
```bash
curl -X POST http://localhost:5000/api/convert/hl7v2 \
  -H "Content-Type: application/json" \
  -d '{
    "message": "MSH|^~\\&|SENDING_APP|SENDING_FACILITY|RECEIVING_APP|RECEIVING_FACILITY|20231201120000||ADT^A01|MSG00001|P|2.5",
    "template_collection": "Hl7v2"
  }'
```

#### Convert C-CDA to FHIR
```bash
curl -X POST http://localhost:5000/api/convert/ccda \
  -H "Content-Type: application/json" \
  -d '{
    "document": "<ClinicalDocument>...</ClinicalDocument>",
    "template_collection": "Ccda"
  }'
```

#### Convert JSON to FHIR
```bash
curl -X POST http://localhost:5000/api/convert/json \
  -H "Content-Type: application/json" \
  -d '{
    "data": {"patient": {"id": "123", "name": "John Doe"}},
    "template_collection": "Json"
  }'
```

#### Convert FHIR STU3 to R4
```bash
curl -X POST http://localhost:5000/api/convert/stu3-to-r4 \
  -H "Content-Type: application/json" \
  -d '{
    "stu3_resource": {
      "resourceType": "Patient",
      "id": "123",
      "name": [{"text": "John Doe"}]
    }
  }'
```

#### Convert FHIR to HL7v2
```bash
curl -X POST http://localhost:5000/api/convert/fhir-to-hl7v2 \
  -H "Content-Type: application/json" \
  -d '{
    "fhir_resource": {
      "resourceType": "Patient",
      "id": "123",
      "name": [{"text": "John Doe"}]
    },
    "template_collection": "Hl7v2"
  }'
```

#### Metrics (Prometheus)
```bash
curl http://localhost:5000/metrics
```

---

## Configuration

### Environment Variables

#### Cloud Storage Configuration
```bash
# Disable cloud storage (use local templates)
CloudStorageConfiguration=null

# GCP Configuration
CloudStorageConfiguration__Provider=GCP
CloudStorageConfiguration__GcpBucketName=your-bucket-name
CloudStorageConfiguration__GcpProjectId=your-project-id

# Azure Configuration
CloudStorageConfiguration__Provider=Azure
CloudStorageConfiguration__AzureConnectionString=your-connection-string
CloudStorageConfiguration__AzureContainerName=your-container-name
```

#### API Configuration
```bash
# Environment
ASPNETCORE_ENVIRONMENT=Development  # Development, Staging, Production

# Logging
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft=Warning

# Health Checks
HealthChecks__Enabled=true
HealthChecks__Timeout=30
```

### Configuration Files

#### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  },
  "CloudStorageConfiguration": null,
  "HealthChecks": {
    "Enabled": true,
    "Timeout": 30
  }
}
```

#### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "CloudStorageConfiguration": {
    "Provider": "GCP",
    "GcpBucketName": "your-production-bucket",
    "GcpProjectId": "your-production-project"
  },
  "HealthChecks": {
    "Enabled": true,
    "Timeout": 30
  }
}
```

---

## Testing

### Unit Tests
```bash
dotnet test src/Microsoft.Health.Fhir.Liquid.Converter.Api.Tests/
```

### Integration Tests
```bash
# Test health endpoint
curl -f http://localhost:8080/health

# Test conversion with sample data
curl -X POST http://localhost:8080/api/convert/hl7v2 \
  -H "Content-Type: application/json" \
  -d @data/SampleData/Hl7v2/ADT-A01-01.hl7
```

### Load Testing
```bash
# Using Apache Bench
ab -n 1000 -c 10 http://localhost:8080/health

# Using Artillery
artillery quick --count 100 --num 10 http://localhost:8080/health
```

---

## Development

### Building Locally
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the API
dotnet run
```

### Docker Development
```bash
# Build with development configuration
docker build -f src/Microsoft.Health.Fhir.Liquid.Converter.Api/Dockerfile \
  --build-arg BUILD_CONFIGURATION=Debug \
  -t fhir-converter-api:dev .

# Run with volume mount for development
docker run -p 8080:8080 \
  -v $(pwd)/data/Templates:/app/templates \
  --rm fhir-converter-api:dev
```

### Debugging
```bash
# Run with debugging enabled
dotnet run --configuration Debug

# Or use VS Code launch configuration
code . && code --open-uri http://localhost:8080/health
```

---

## Monitoring

### Health Checks
- **Liveness**: `/health` - Service is running
- **Readiness**: `/health/ready` - Service is ready to accept requests
- **Startup**: `/health/startup` - Service has started successfully

### Metrics
- **Prometheus**: `/metrics` - Application metrics
- **Custom Metrics**: Request count, latency, error rates
- **System Metrics**: CPU, memory, GC statistics

### Logging
- **Structured Logging**: JSON format with correlation IDs
- **Log Levels**: Debug, Information, Warning, Error
- **Cloud Integration**: GCP Cloud Logging, Azure Application Insights

---

## Security

### Container Security
- **Non-root User**: Container runs as non-root user
- **Multi-stage Build**: Minimal attack surface
- **Security Scanning**: Automated vulnerability scanning
- **Secrets Management**: Environment-based configuration

### API Security
- **Input Validation**: Request validation and sanitization
- **Rate Limiting**: Configurable rate limiting
- **CORS**: Cross-origin resource sharing configuration
- **Authentication**: Ready for OAuth/JWT integration

---

## Deployment

### Docker Deployment
```bash
# Build production image
docker build -f src/Microsoft.Health.Fhir.Liquid.Converter.Api/Dockerfile \
  --build-arg BUILD_CONFIGURATION=Release \
  -t fhir-converter-api:latest .

# Run in production
docker run -d \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e CloudStorageConfiguration__Provider=GCP \
  -e CloudStorageConfiguration__GcpBucketName=prod-templates \
  --name fhir-converter-api \
  fhir-converter-api:latest
```

### Cloud Deployment
- **GCP**: Use Cloud Run with Terraform deployment
- **Azure**: Use Container Instances or App Service
- **Kubernetes**: Use provided Helm charts or manifests

### Production Considerations
- **Scaling**: Horizontal pod autoscaling
- **Monitoring**: Cloud-native monitoring integration
- **Security**: Network policies and security groups
- **Backup**: Template and configuration backup strategies

---

## Troubleshooting

### Common Issues

#### Service Won't Start
```bash
# Check logs
docker logs fhir-converter-api

# Check configuration
docker exec fhir-converter-api cat /app/appsettings.json
```

#### Template Loading Issues
```bash
# Verify template location
ls -la data/Templates/

# Check cloud storage configuration
echo $CloudStorageConfiguration__Provider
```

#### Performance Issues
```bash
# Check metrics
curl http://localhost:8080/metrics

# Monitor resource usage
docker stats fhir-converter-api
```

### Debug Commands
```bash
# Health check
curl -v http://localhost:8080/health

# Template list
curl http://localhost:8080/api/templates

# Version info
curl http://localhost:8080/api/version
```

---

## Documentation

### Related Documentation
- [Main Project README](../../README.md) - Project overview and setup
- [Cloud Storage Setup](../../CLOUD_STORAGE_README.md) - Cloud storage configuration
- [Docker Compose Guide](../../DOCKER_COMPOSE_README.md) - Local development with monitoring
- [Terraform Deployment](../../terraform/gcp/README.md) - Infrastructure deployment

### API Reference
- **OpenAPI Spec**: Available at `/swagger` when enabled
- **Health Checks**: Built-in health monitoring
- **Metrics**: Prometheus-compatible metrics
- **Error Handling**: Standardized error responses

---

## Contributing

### Development Workflow
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests and documentation
5. Submit a pull request

### Code Standards
- Follow .NET coding conventions
- Add unit tests for new features
- Update API documentation
- Use conventional commit messages

---

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

---

*This API provides a modern, cloud-native interface for healthcare data conversion with enterprise-grade features and monitoring capabilities.* 