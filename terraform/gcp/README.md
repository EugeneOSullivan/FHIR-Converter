# FHIR Converter GCP Terraform Deployment

This directory contains the Terraform configuration for deploying the FHIR Converter to Google Cloud Platform (GCP) with enterprise-grade infrastructure, multi-environment support, and comprehensive monitoring.

## Architecture Overview

The deployment creates a robust, scalable infrastructure with the following components:

### Core Infrastructure
- **Cloud Run Service**: Serverless container hosting the FHIR Converter API
- **GCS Bucket**: Template storage with versioning and lifecycle policies
- **VPC Network**: Private network with VPC connector for Cloud Run
- **API Gateway**: External access point with OpenAPI specification

### Security & Compliance
- **Service Accounts**: Least-privilege access for Cloud Run and monitoring
- **IAM Policies**: Role-based access control
- **Cloud Armor**: DDoS protection and security policies (production)
- **Private Service**: Network isolation (production)

### Monitoring & Observability
- **Cloud Monitoring**: Custom dashboards and alerting
- **Cloud Logging**: Centralized logging with BigQuery integration
- **Uptime Checks**: Health monitoring
- **Alert Policies**: Automated notifications for issues

### Multi-Environment Support
- **Development**: Lightweight configuration for testing
- **Staging**: Pre-production validation
- **Production**: Enterprise-grade with full security

## Directory Structure

```
terraform/gcp/
├── main.tf                 # Main Terraform configuration
├── variables.tf            # Variable definitions
├── locals.tf              # Computed values and environment configs
├── outputs.tf             # Output values
├── versions.tf            # Provider versions
├── terragrunt.hcl         # Terragrunt root configuration
├── deploy.sh              # Automated deployment script
├── environments/
│   ├── dev/
│   │   └── terragrunt.hcl # Development environment
│   ├── staging/
│   │   └── terragrunt.hcl # Staging environment
│   ├── prod/
│   │   └── terragrunt.hcl # Production environment
│   ├── dev.tfvars         # Development variables
│   ├── staging.tfvars     # Staging variables
│   └── prod.tfvars        # Production variables
└── modules/
    ├── networking/        # VPC, subnets, VPC connector
    ├── security/          # Service accounts, IAM
    ├── fhir-converter/    # Cloud Run, GCS, API Gateway
    └── monitoring/        # Monitoring, logging, alerting
```

## Prerequisites

### GCP Setup
1. **GCP Project**: Create separate projects for each environment
2. **Billing Account**: Enable billing for all projects
3. **Organization**: Set up GCP organization (recommended)
4. **Service Accounts**: Create deployment service accounts with required permissions

### Required Permissions
The deployment service account needs the following roles:
- `roles/owner` (for initial setup)
- `roles/storage.admin`
- `roles/run.admin`
- `roles/compute.networkAdmin`
- `roles/iam.serviceAccountAdmin`
- `roles/monitoring.admin`
- `roles/logging.admin`
- `roles/apigateway.admin`

### Tools Installation
```bash
# Install Terraform
curl -fsSL https://apt.releases.hashicorp.com/gpg | sudo apt-key add -
sudo apt-add-repository "deb [arch=amd64] https://apt.releases.hashicorp.com $(lsb_release -cs)"
sudo apt-get update && sudo apt-get install terraform

# Install Terragrunt
curl -fsSL https://github.com/gruntwork-io/terragrunt/releases/download/v0.45.0/terragrunt_linux_amd64 -o terragrunt
chmod +x terragrunt
sudo mv terragrunt /usr/local/bin/

# Install Google Cloud CLI
curl https://sdk.cloud.google.com | bash
exec -l $SHELL
gcloud init
```

## Quick Start

### 1. Configure Environment Variables

Update the environment-specific files with your values:

```bash
# Edit development configuration
nano terraform/gcp/environments/dev/terragrunt.hcl

# Update these values:
# - organization_id
# - project_id
# - billing_account
# - allowed_ip_ranges
```

### 2. Set Up Remote State

Create a GCS bucket for Terraform state:

```bash
gsutil mb gs://fhir-converter-terraform-state
gsutil versioning set on gs://fhir-converter-terraform-state
```

### 3. Authenticate with GCP

```bash
# Set up service account key
export GOOGLE_APPLICATION_CREDENTIALS="path/to/service-account-key.json"

# Or use gcloud auth
gcloud auth application-default login
```

### 4. Deploy Development Environment

```bash
cd terraform/gcp/environments/dev
terragrunt init
terragrunt plan
terragrunt apply
```

### 5. Deploy Other Environments

```bash
# Staging
cd terraform/gcp/environments/staging
terragrunt apply

# Production
cd terraform/gcp/environments/prod
terragrunt apply
```

## Environment Configurations

### Development
- **CPU**: 500m
- **Memory**: 1Gi
- **Max Instances**: 10
- **Min Instances**: 0
- **Monitoring**: Basic
- **Alerting**: Disabled
- **Access**: Public

### Staging
- **CPU**: 1000m
- **Memory**: 2Gi
- **Max Instances**: 50
- **Min Instances**: 1
- **Monitoring**: Full
- **Alerting**: Enabled
- **Access**: Restricted IPs

### Production
- **CPU**: 2000m
- **Memory**: 4Gi
- **Max Instances**: 200
- **Min Instances**: 5
- **Monitoring**: Enterprise
- **Alerting**: Full
- **Access**: Private + VPN

## CI/CD Integration

### Azure DevOps
1. Create variable groups with GCP credentials
2. Set up environments (development, staging, production)
3. Configure service connections
4. Run the pipeline: `azure-pipelines.yml` (in project root)

### GitHub Actions
1. Add repository secrets:
   - `GCP_SA_KEY`: Service account key
   - `GCP_ORG_ID`: Organization ID
   - `GCP_PROJECT_ID`: Project IDs for each environment
2. Set up environments in GitHub
3. Push to trigger deployment via `.github/workflows/terraform.yml`

## Monitoring & Alerting

### Cloud Monitoring Dashboard
Access the dashboard at: `https://console.cloud.google.com/monitoring/dashboards`

### Alert Policies
- **High Error Rate**: >5% errors over 5 minutes
- **High Latency**: >10s 95th percentile
- **Service Unavailable**: No requests in 1 minute

### Logging
- **Cloud Run Logs**: Application and access logs
- **BigQuery Integration**: Structured log analysis
- **Log Sinks**: Centralized log collection

## Security Features

### Network Security
- **VPC**: Private network isolation
- **VPC Connector**: Secure Cloud Run connectivity
- **Cloud NAT**: Outbound internet access
- **Firewall Rules**: Restricted access

### Access Control
- **Service Accounts**: Least-privilege access
- **IAM Roles**: Role-based permissions
- **API Gateway**: Controlled external access
- **Cloud Armor**: DDoS protection (production)

### Data Protection
- **GCS Encryption**: At-rest encryption
- **TLS**: In-transit encryption
- **Private Service**: Network isolation
- **Audit Logging**: Comprehensive audit trails

## Scaling Configuration

### Auto-scaling
- **Min Instances**: Prevents cold starts
- **Max Instances**: Cost control
- **CPU/Memory**: Resource allocation
- **Concurrency**: Request handling

### Environment-specific Scaling
```hcl
# Development
cloud_run_min_instances = 0
cloud_run_max_instances = 10

# Staging
cloud_run_min_instances = 1
cloud_run_max_instances = 50

# Production
cloud_run_min_instances = 5
cloud_run_max_instances = 200
```

## Cost Optimization

### Resource Optimization
- **Development**: Minimal resources, auto-scaling to zero
- **Staging**: Moderate resources, limited scaling
- **Production**: Optimized for performance and reliability

### Monitoring Costs
- **Cloud Monitoring**: Free tier + usage-based pricing
- **Logging**: Free tier + storage costs
- **BigQuery**: Pay-per-use for log analysis

### Cost Estimation
```bash
# Estimate costs for each environment
cd terraform/gcp/environments/dev
terragrunt plan -out=tfplan
terraform show -json tfplan | jq '.planned_values.root_module.resources[] | select(.type == "google_cloud_run_v2_service")'
```

## Troubleshooting

### Common Issues

#### Authentication Errors
```bash
# Verify service account permissions
gcloud projects get-iam-policy PROJECT_ID

# Check service account key
gcloud auth activate-service-account --key-file=key.json
```

#### API Enablement Issues
```bash
# Enable required APIs manually
gcloud services enable cloudrun.googleapis.com
gcloud services enable vpcaccess.googleapis.com
gcloud services enable apigateway.googleapis.com
```

#### State Lock Issues
```bash
# Force unlock (use with caution)
terragrunt force-unlock LOCK_ID
```

#### Resource Creation Failures
```bash
# Check resource quotas
gcloud compute regions describe us-central1

# Verify billing is enabled
gcloud billing projects describe PROJECT_ID
```

### Debug Commands
```bash
# Validate configuration
terragrunt validate

# Check plan
terragrunt plan -detailed-exitcode

# View logs
gcloud logging read "resource.type=cloud_run_revision"

# Check service status
gcloud run services describe SERVICE_NAME --region=us-central1
```

## Maintenance

### Regular Tasks
1. **Terraform Updates**: Keep providers and modules updated
2. **Security Patches**: Monitor for security advisories
3. **Cost Review**: Monthly cost analysis and optimization
4. **Performance Monitoring**: Review metrics and scaling

### Backup & Recovery
- **State Backup**: GCS bucket with versioning
- **Configuration**: Version control with Git
- **Templates**: GCS bucket with versioning
- **Monitoring Data**: Cloud Monitoring retention policies

## Support

For issues and questions:
1. Check the troubleshooting section
2. Review Cloud Monitoring logs
3. Consult GCP documentation
4. Contact the operations team

## Contributing

When modifying the Terraform configuration:
1. Follow the existing module structure
2. Update documentation
3. Test in development first
4. Use consistent naming conventions
5. Add appropriate labels and tags 