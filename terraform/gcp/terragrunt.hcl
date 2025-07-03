# Terragrunt configuration for FHIR Converter GCP deployment
# This file enables easy management of multiple environments

# Remote state configuration
remote_state {
  backend = "gcs"
  config = {
    bucket = "fhir-converter-terraform-state"
    prefix = "${path_relative_to_include()}"
  }
  generate = {
    path      = "backend.tf"
    if_exists = "overwrite_terragrunt"
  }
}

# Provider configuration
generate "providers" {
  path      = "providers.tf"
  if_exists = "overwrite_terragrunt"
  contents  = <<EOF
terraform {
  required_version = ">= 1.0"
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
    google-beta = {
      source  = "hashicorp/google-beta"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
    null = {
      source  = "hashicorp/null"
      version = "~> 3.0"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

provider "google-beta" {
  project = var.project_id
  region  = var.region
}
EOF
}

# Common inputs for all environments
inputs = {
  # Common configuration
  billing_account = "your-billing-account"
  region          = "us-central1"
  
  # Enable required features
  enable_vpc_connector = true
  enable_api_gateway   = true
  
  # Common labels
  common_labels = {
    managed_by = "terragrunt"
    project    = "fhir-converter"
  }
} 