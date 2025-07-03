# FHIR-Converter Cloud Storage Configuration

This document explains how to configure the FHIR-Converter API to use cloud storage for templates, supporting both Google Cloud Platform (GCP) and Azure Storage.

## Overview

The FHIR-Converter now supports cloud-agnostic storage with the following providers:
- **GCP Cloud Storage** (Primary/Default)
- **Azure Blob Storage** (Legacy support maintained)

The system automatically detects the configured provider and uses the appropriate storage client.

## Configuration Options

### 1. Environment Variables (Recommended)

Set environment variables to configure cloud storage:

```bash
# GCP Configuration (Primary)
export GCP_PROJECT_ID="your-gcp-project-id"
export GCP_BUCKET_NAME="fhir-converter-templates"
export GCP_CONTAINER_NAME="templates"

# Azure Configuration (Alternative)
export AZURE_STORAGE_ACCOUNT="yourstorageaccount"
export AZURE_CONTAINER_NAME="templates"

# Provider Selection
export TEMPLATE_HOSTING__CLOUDSTORAGECONFIGURATION__PROVIDER="GCP"
```

### 2. Configuration Files

#### appsettings.json
```json
{
  "TemplateHosting": {
    "CloudStorageConfiguration": {
      "Provider": "GCP",
      "Gcp": {
        "ProjectId": "your-gcp-project-id",
        "BucketName": "fhir-converter-templates",
        "ContainerName": "templates"
      },
      "Azure": {
        "StorageAccountName": "yourstorageaccount",
        "ContainerName": "templates",
        "EndpointSuffix": "blob.core.windows.net"
      }
    }
  }
}
```

#### appsettings.Production.json
```json
{
  "TemplateHosting": {
    "CloudStorageConfiguration": {
      "Provider": "GCP",
      "Gcp": {
        "ProjectId": "",
        "BucketName": "",
        "ContainerName": "templates"
      }
    }
  }
}
```

### 3. Docker Compose

Use the provided `env.example` file:

```bash
# Copy and configure environment variables
cp env.example .env

# Edit .env with your values
nano .env
```

Then start with Docker Compose:
```bash
docker-compose up
```

## GCP Cloud Storage Setup

### 1. Create a GCS Bucket

```bash
# Using gcloud CLI
gcloud storage buckets create gs://fhir-converter-templates \
  --project=your-project-id \
  --location=us-central1 \
  --uniform-bucket-level-access
```

### 2. Upload Templates

```bash
# Upload template files to the bucket
gsutil cp -r data/Templates/* gs://fhir-converter-templates/templates/
```

### 3. Configure Authentication

#### Option A: Service Account (Recommended for Production)
```bash
# Create service account
gcloud iam service-accounts create fhir-converter-sa \
  --display-name="FHIR Converter Service Account"

# Grant Storage Object Viewer role
gcloud projects add-iam-policy-binding your-project-id \
  --member="serviceAccount:fhir-converter-sa@your-project-id.iam.gserviceaccount.com" \
  --role="roles/storage.objectViewer"

# Download key file
gcloud iam service-accounts keys create key.json \
  --iam-account=fhir-converter-sa@your-project-id.iam.gserviceaccount.com

# Set environment variable
export GOOGLE_APPLICATION_CREDENTIALS="key.json"
```

#### Option B: Application Default Credentials (Development)
```bash
# Authenticate with gcloud
gcloud auth application-default login
```

### 4. Configure the API

Set environment variables:
```bash
export GCP_PROJECT_ID="your-project-id"
export GCP_BUCKET_NAME="fhir-converter-templates"
export GCP_CONTAINER_NAME="templates"
```

## Azure Blob Storage Setup

### 1. Create Storage Account

```bash
# Using Azure CLI
az storage account create \
  --name yourstorageaccount \
  --resource-group your-resource-group \
  --location eastus \
  --sku Standard_LRS

# Create container
az storage container create \
  --name templates \
  --account-name yourstorageaccount
```

### 2. Upload Templates

```bash
# Upload template files
az storage blob upload-batch \
  --source data/Templates \
  --destination templates \
  --account-name yourstorageaccount
```

### 3. Configure Authentication

#### Option A: Managed Identity (Recommended for Production)
```bash
# Enable managed identity on your service
az webapp identity assign --name your-app-name --resource-group your-resource-group

# Grant Storage Blob Data Reader role
az role assignment create \
  --assignee <principal-id> \
  --role "Storage Blob Data Reader" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.Storage/storageAccounts/yourstorageaccount"
```

#### Option B: Connection String (Development)
```bash
# Get connection string
az storage account show-connection-string \
  --name yourstorageaccount \
  --resource-group your-resource-group

# Set environment variable
export AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
```

### 4. Configure the API

Set environment variables:
```bash
export TEMPLATE_HOSTING__CLOUDSTORAGECONFIGURATION__PROVIDER="Azure"
export AZURE_STORAGE_ACCOUNT="yourstorageaccount"
export AZURE_CONTAINER_NAME="templates"
```

## Template Structure

Templates should be organized in the cloud storage as follows:

```
templates/
├── Hl7v2/
│   ├── ADT_A01.liquid
│   ├── ADT_A02.liquid
│   └── ...
├── Ccda/
│   ├── CCD.liquid
│   ├── ConsultationNote.liquid
│   └── ...
├── Json/
│   ├── ExamplePatient.liquid
│   └── ...
└── Stu3ToR4/
    ├── Patient.liquid
    ├── Observation.liquid
    └── ...
```

## Provider Selection Logic

The system uses the following priority order:

1. **Cloud Storage Configuration** (New approach)
   - Checks `TemplateHosting.CloudStorageConfiguration.Provider`
   - Uses GCP if `Provider = "GCP"`
   - Uses Azure if `Provider = "Azure"`

2. **Legacy Azure Configuration** (Fallback)
   - Checks `TemplateHosting.StorageAccountConfiguration.ContainerUrl`
   - Uses Azure Blob Storage if URL is provided

3. **Default Templates** (Final fallback)
   - Uses embedded templates if no cloud storage is configured

## Environment-Specific Configurations

### Development
- Uses local templates by default
- Set `TemplateHosting.CloudStorageConfiguration = null` in `appsettings.Development.json`

### Production
- Uses GCP by default
- Configure via environment variables or `appsettings.Production.json`

### Testing
- Can use either provider
- Use environment variables to switch between providers

## Docker Deployment

### GCP Deployment
```bash
# Set GCP environment variables
export GCP_PROJECT_ID="your-project-id"
export GCP_BUCKET_NAME="fhir-converter-templates"
export GCP_CONTAINER_NAME="templates"

# Start with Docker Compose
docker-compose up
```

### Azure Deployment
```bash
# Set Azure environment variables
export TEMPLATE_HOSTING__CLOUDSTORAGECONFIGURATION__PROVIDER="Azure"
export AZURE_STORAGE_ACCOUNT="yourstorageaccount"
export AZURE_CONTAINER_NAME="templates"

# Start with Docker Compose
docker-compose up
```

## Troubleshooting

### Common Issues

1. **Authentication Errors**
   ```bash
   # GCP: Check service account permissions
   gcloud auth list
   gcloud config get-value project
   
   # Azure: Check managed identity
   az identity show --name your-identity --resource-group your-resource-group
   ```

2. **Template Not Found**
   ```bash
   # GCP: List bucket contents
   gsutil ls gs://your-bucket-name/templates/
   
   # Azure: List container contents
   az storage blob list --container-name templates --account-name yourstorageaccount
   ```

3. **Configuration Issues**
   ```bash
   # Check environment variables
   env | grep -E "(GCP|AZURE|TEMPLATE)"
   
   # Check configuration binding
   dotnet run --environment Development
   ```

### Debug Logging

Enable debug logging to troubleshoot issues:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Health.Fhir.TemplateManagement": "Debug",
      "Microsoft.Health.Fhir.Liquid.Converter": "Debug"
    }
  }
}
```

## Migration Guide

### From Azure to GCP

1. **Upload templates to GCS**
   ```bash
   gsutil cp -r data/Templates/* gs://your-bucket-name/templates/
   ```

2. **Update configuration**
   ```bash
   export TEMPLATE_HOSTING__CLOUDSTORAGECONFIGURATION__PROVIDER="GCP"
   export GCP_PROJECT_ID="your-project-id"
   export GCP_BUCKET_NAME="your-bucket-name"
   ```

3. **Test the migration**
   ```bash
   curl -X POST http://localhost:8080/api/convert/hl7v2 \
     -H "Content-Type: application/json" \
     -d @data/SampleData/Hl7v2/ADT-A01-01.hl7
   ```

### From Local to Cloud

1. **Choose your provider** (GCP recommended)
2. **Set up authentication** (see provider-specific sections)
3. **Upload templates** to cloud storage
4. **Configure environment variables**
5. **Test the setup**

## Security Considerations

### GCP Security
- Use service accounts with minimal required permissions
- Enable bucket-level access control
- Use VPC Service Controls for additional security

### Azure Security
- Use managed identities when possible
- Enable storage account firewall rules
- Use private endpoints for enhanced security

### General Security
- Never commit credentials to source control
- Use environment variables or secure key management
- Regularly rotate access keys and service account keys
- Monitor access logs for suspicious activity

# Local Development Without a Cloud Bucket

You can run the FHIR-Converter API using only local templates, with no dependency on GCP or Azure storage. This is ideal for development and testing.

## How to Run Locally (No Cloud Storage)

1. **Ensure Cloud Storage is Disabled**
   - By default, `appsettings.Development.json` disables cloud storage:
     ```json
     {
       "TemplateHosting": {
         "CloudStorageConfiguration": null
       }
     }
     ```
   - No GCP or Azure environment variables are needed.

2. **Set the Environment**
   ```sh
   export ASPNETCORE_ENVIRONMENT=Development
   ```

3. **Run the API**
   ```sh
   dotnet run --project src/Microsoft.Health.Fhir.Liquid.Converter.Api
   # or with Docker Compose (uses local templates by default)
   docker-compose up fhir-converter-api
   ```

4. **Templates Location**
   - The API will use templates from `data/Templates` in your repo.

5. **Test the API**
   ```sh
   curl http://localhost:8080/health
   # or test a conversion endpoint as described below
   ```

--- 