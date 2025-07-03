# Microsoft FHIR-Converter (Cloud-Agnostic Edition)

> **Based on the original [Microsoft FHIR-Converter](https://github.com/microsoft/FHIR-Converter)**

A modern, cloud-agnostic, high-performance healthcare data conversion engine supporting HL7v2, C-CDA, FHIR, and custom formats. Easily deployable on GCP, Azure, or locally, with robust API, Docker, monitoring, and enterprise-grade infrastructure as code.

---

## Quick Start

### Local Development (No Cloud Required)
```bash
# Clone the repository
git clone https://github.com/your-org/FHIR-Converter.git
cd FHIR-Converter

# Run the API locally
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/Microsoft.Health.Fhir.Liquid.Converter.Api

# Or use Docker Compose
docker-compose up
```

### Cloud Deployment
```bash
# GCP Deployment (with Terraform)
cd terraform/gcp
./deploy.sh install
./deploy.sh setup
./deploy.sh dev

# Or use CI/CD
# Push to develop branch for dev deployment
# Push to main branch for staging/prod deployment
```

---

## Architecture

### Core Components
- **API Service**: ASP.NET Core 8.0 Web API with health checks and monitoring
- **Template Engine**: Liquid templating for flexible data transformation
- **Cloud Storage**: GCP GCS or Azure Blob Storage for template management
- **Containerization**: Multi-stage Docker builds with security scanning
- **Infrastructure**: Terraform/Terragrunt for multi-environment deployment
- **Monitoring**: Cloud-native monitoring with dashboards and alerting

### Supported Formats
- **HL7v2** → FHIR R4
- **C-CDA** → FHIR R4
- **JSON** → FHIR R4
- **FHIR STU3** → FHIR R4
- **FHIR** → HL7v2

### Cloud Providers
- **GCP**: Cloud Run, GCS, Cloud Monitoring, API Gateway
- **Azure**: Container Instances, Blob Storage, Application Insights
- **Local**: Docker Compose with local templates

---

## Project Structure

```
FHIR-Converter/
├── src/
│   ├── Microsoft.Health.Fhir.Liquid.Converter.Api/     # Web API project
│   ├── Microsoft.Health.Fhir.Liquid.Converter/         # Core converter logic
│   └── Microsoft.Health.Fhir.TemplateManagement/       # Template management
├── data/
│   ├── Templates/                                       # FHIR templates
│   └── SampleData/                                      # Test data
├── terraform/
│   └── gcp/                                            # GCP infrastructure
│       ├── modules/                                     # Reusable modules
│       ├── environments/                                # Environment configs
│       └── deploy.sh                                   # Deployment script
├── docker-compose.yml                                  # Local development
├── Dockerfile                                          # Container build
├── azure-pipelines.yml                                 # Azure DevOps CI/CD
└── .github/workflows/                                  # GitHub Actions CI/CD
```

---

## Development

### Prerequisites
- .NET 9.0 SDK
- Docker & Docker Compose (optional)
- Terraform 1.5+ (for cloud deployment)
- Terragrunt 0.45+ (for multi-environment management)

### Local Development
```bash
# Run API with local templates
dotnet run --project src/Microsoft.Health.Fhir.Liquid.Converter.Api

# Run with Docker Compose (includes monitoring)
docker-compose up

# Run tests
dotnet test
```

### API Endpoints
```bash
# Health check
curl http://localhost:5000/api/v1/health/check

# Convert HL7v2 to FHIR
curl -X POST http://localhost:5000/api/v1/convert/hl7v2-to-fhir \
  -H "Content-Type: application/json" \
  -d '{
    "inputDataFormat": "Hl7v2",
    "inputDataString": "MSH|^~\\&|ADT1|GOOD HEALTH HOSPITAL|GHH LAB, INC.|GOOD HEALTH HOSPITAL|19880818112600+0700|SECURITY|ADT^A01^ADT_A01|MSG00001|P|2.5.1||\nEVN|A01|20070818112300+0700||\nPID|1||PATID1234^5^M11^ADT1^MR^GOOD HEALTH HOSPITAL~123456789^^^USSSA^SS||EVERYMAN^ADAM^A^III||19610615|M||C|2222 HOME STREET^^GREENSBORO^NC^27401-1020|GL|(555) 555-2004|(555)555-2004||S||PATID12345001^2^M10^ADT1^AN^A|444333333|987654^NC|\nNK1|1|NUCLEAR^NELDA^W|SPO^SPOUSE||||NK^NEXT OF KIN\nPV1|1|I|2000^2012^01||||004777^ATTEND^AARON^A|||SUR||||ADM|A0|"
  }'

# Convert C-CDA to FHIR
curl -X POST http://localhost:5000/api/v1/convert/ccda-to-fhir \
  -H "Content-Type: application/json" \
  -d '{
    "inputDataFormat": "Ccda",
    "inputDataString": "<ClinicalDocument>...</ClinicalDocument>"
  }'

# Convert FHIR STU3 to R4
curl -X POST http://localhost:5000/api/v1/convert/fhir-stu3-to-r4 \
  -H "Content-Type: application/json" \
  -d '{
    "inputDataFormat": "FhirStu3",
    "inputDataString": "{\"resourceType\": \"Patient\", \"id\": \"123\"}"
  }'

# Interactive API Documentation (Development mode)
open http://localhost:5000/api/v1/swagger
```

---

## Cloud Deployment

### GCP Deployment (Recommended)

#### Quick Deploy
```bash
cd terraform/gcp
./deploy.sh install    # Install tools
./deploy.sh setup      # Setup authentication
./deploy.sh dev        # Deploy development
```

#### Multi-Environment Deployment
```bash
# Deploy all environments
./deploy.sh deploy

# Or deploy specific environments
./deploy.sh staging
./deploy.sh prod
```

#### Infrastructure Features
- **Cloud Run**: Serverless container hosting with auto-scaling
- **GCS Bucket**: Template storage with versioning
- **VPC Network**: Private network with VPC connector
- **API Gateway**: External access with OpenAPI spec
- **Cloud Monitoring**: Dashboards, alerting, and logging
- **Cloud Armor**: DDoS protection (production)

### Azure Deployment
```bash
# Use Azure Container Instances or App Service
# See CLOUD_STORAGE_README.md for Azure setup
```

---

## Configuration

### Environment Variables
```bash
# Cloud Storage Configuration
CloudStorageConfiguration__Provider=GCP                    # GCP, Azure, or null for local
CloudStorageConfiguration__GcpBucketName=your-bucket      # GCS bucket name
CloudStorageConfiguration__GcpProjectId=your-project      # GCP project ID

# API Configuration
ASPNETCORE_ENVIRONMENT=Development                         # Development, Staging, Production
Logging__LogLevel__Default=Information                     # Logging level
```

### Template Storage
- **Local**: Uses `data/Templates/` directory
- **GCP**: GCS bucket with versioning and lifecycle policies
- **Azure**: Blob storage with managed identity
- **Hybrid**: Mix of local and cloud templates

---

## Monitoring & Observability

### Local Monitoring
```bash
# Prometheus metrics
curl http://localhost:5000/metrics

# Health check
curl http://localhost:5000/health

# Grafana dashboard
open http://localhost:3000
```

### Cloud Monitoring (GCP)
- **Cloud Monitoring Dashboard**: Custom metrics and visualizations
- **Alert Policies**: Error rate, latency, and availability monitoring
- **Logging**: Structured logs with BigQuery integration
- **Uptime Checks**: Automated health monitoring

### Metrics Available
- Request count and latency
- Error rates and status codes
- CPU and memory utilization
- Template cache hit rates
- Conversion success/failure rates

---

## Security

### Network Security
- **VPC**: Private network isolation
- **VPC Connector**: Secure Cloud Run connectivity
- **API Gateway**: Controlled external access
- **Cloud Armor**: DDoS protection (production)

### Access Control
- **Service Accounts**: Least-privilege access
- **IAM Roles**: Role-based permissions
- **Private Service**: Network isolation (production)
- **Audit Logging**: Comprehensive audit trails

### Data Protection
- **Encryption**: At-rest and in-transit encryption
- **TLS**: HTTPS everywhere
- **Secrets Management**: Secure credential handling
- **Compliance**: HIPAA-ready configurations

---

## CI/CD

### GitHub Actions
- **Triggers**: Push to main/develop branches
- **Environments**: Development, staging, production
- **Security**: Automated vulnerability scanning
- **Testing**: Health checks and integration tests

### Azure DevOps
- **Multi-stage**: Validate, deploy, test
- **Environments**: Approval gates for production
- **Monitoring**: Post-deployment health checks
- **Rollback**: Automated rollback on failures

### Deployment Flow
1. **Development**: Auto-deploy on push to develop
2. **Staging**: Auto-deploy on push to main
3. **Production**: Manual approval required
4. **Monitoring**: Automated health checks and alerts

---

## Testing

### Unit Tests
```bash
dotnet test src/Microsoft.Health.Fhir.Liquid.Converter.UnitTests/
dotnet test src/Microsoft.Health.Fhir.TemplateManagement.UnitTests/
```

### Integration Tests
```bash
dotnet test src/Microsoft.Health.Fhir.Liquid.Converter.FunctionalTests/
```

### API Tests
```bash
# Health check
curl -f http://localhost:5000/health

# Sample conversion
curl -X POST http://localhost:5000/api/convert/hl7v2 \
  -H "Content-Type: application/json" \
  -d @data/SampleData/Hl7v2/ADT-A01-01.hl7
```

### Load Testing
```bash
# Use tools like Apache Bench or Artillery
ab -n 1000 -c 10 http://localhost:5000/health
```

---

## Documentation

### Core Documentation
- [API Documentation](src/Microsoft.Health.Fhir.Liquid.Converter.Api/README.md) - API usage and endpoints
- [Cloud Storage Setup](CLOUD_STORAGE_README.md) - GCP/Azure configuration
- [Docker Compose Guide](DOCKER_COMPOSE_README.md) - Local development with monitoring
- [Terraform Deployment](terraform/gcp/README.md) - Infrastructure as code

### Architecture Guides
- [Template Management](docs/concepts/template-management.md) - How templates work
- [Conversion Process](docs/concepts/conversion-process.md) - Data transformation flow
- [Security Model](docs/concepts/security.md) - Security and compliance

### Migration Guides
- [From Original FHIR-Converter](docs/migration/from-original.md) - Migration from Microsoft's version
- [Cloud Provider Migration](docs/migration/cloud-migration.md) - Moving between GCP/Azure

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
- Update documentation
- Use conventional commit messages

### Testing Checklist
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] API tests pass
- [ ] Documentation updated
- [ ] Security scan clean

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### Acknowledgments
- Based on the original [Microsoft FHIR-Converter](https://github.com/microsoft/FHIR-Converter)
- Uses Liquid templating engine for data transformation
- Built with ASP.NET Core 9.0 and .NET 9.0

---

## Support

### Getting Help
1. Check the [documentation](#documentation)
2. Review [troubleshooting guides](docs/troubleshooting/)
3. Search [existing issues](https://github.com/your-org/FHIR-Converter/issues)
4. Create a [new issue](https://github.com/your-org/FHIR-Converter/issues/new)

### Community
- **Discussions**: [GitHub Discussions](https://github.com/your-org/FHIR-Converter/discussions)
- **Issues**: [GitHub Issues](https://github.com/your-org/FHIR-Converter/issues)
- **Releases**: [GitHub Releases](https://github.com/your-org/FHIR-Converter/releases)

---

## Roadmap

### Upcoming Features
- [ ] Additional FHIR versions support
- [ ] Custom template validation
- [ ] Advanced monitoring dashboards
- [ ] Multi-region deployment
- [ ] Performance optimizations

### Known Limitations
- Template caching in memory (planned: Redis integration)
- Single-region deployment (planned: multi-region)
- Limited custom validation (planned: extensible validation)

---

*This project extends the original Microsoft FHIR-Converter with modern cloud-native features, improved developer experience, and enterprise-grade deployment capabilities.*
