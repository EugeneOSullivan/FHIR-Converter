# Organization and Project Configuration
variable "organization_id" {
  description = "GCP Organization ID"
  type        = string
}

variable "project_id" {
  description = "GCP Project ID"
  type        = string
}

variable "project_name" {
  description = "GCP Project Name"
  type        = string
}

variable "billing_account" {
  description = "GCP Billing Account ID"
  type        = string
}

# Environment Configuration
variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

variable "environment_suffix" {
  description = "Environment suffix for resource naming"
  type        = string
  default     = ""
}

# Region and Zone Configuration
variable "region" {
  description = "GCP Region for resources"
  type        = string
  default     = "us-central1"
}

variable "zones" {
  description = "GCP Zones for multi-zone deployment"
  type        = list(string)
  default     = ["us-central1-a", "us-central1-b", "us-central1-c"]
}

# Network Configuration
variable "vpc_name" {
  description = "VPC name"
  type        = string
  default     = "fhir-converter-vpc"
}

variable "subnet_cidr" {
  description = "Subnet CIDR block"
  type        = string
  default     = "10.0.0.0/24"
}

# FHIR Converter Configuration
variable "fhir_converter_image" {
  description = "FHIR Converter Docker image"
  type        = string
  default     = "gcr.io/PROJECT_ID/fhir-converter:latest"
}

variable "fhir_converter_version" {
  description = "FHIR Converter version tag"
  type        = string
  default     = "latest"
}

# Cloud Run Configuration
variable "cloud_run_cpu" {
  description = "CPU allocation for Cloud Run service"
  type        = string
  default     = "1000m"
}

variable "cloud_run_memory" {
  description = "Memory allocation for Cloud Run service"
  type        = string
  default     = "2Gi"
}

variable "cloud_run_max_instances" {
  description = "Maximum number of Cloud Run instances"
  type        = number
  default     = 100
}

variable "cloud_run_min_instances" {
  description = "Minimum number of Cloud Run instances"
  type        = number
  default     = 0
}

# Storage Configuration
variable "template_bucket_name" {
  description = "GCS bucket name for FHIR templates"
  type        = string
  default     = ""
}

variable "template_bucket_location" {
  description = "GCS bucket location"
  type        = string
  default     = "US"
}

# Monitoring Configuration
variable "enable_monitoring" {
  description = "Enable Cloud Monitoring and Logging"
  type        = bool
  default     = true
}

variable "enable_alerting" {
  description = "Enable Cloud Monitoring alerting"
  type        = bool
  default     = true
}

# Security Configuration
variable "enable_vpc_connector" {
  description = "Enable VPC Connector for Cloud Run"
  type        = bool
  default     = true
}

variable "enable_private_service" {
  description = "Enable private Cloud Run service (no public access)"
  type        = bool
  default     = false
}

# API Management Configuration
variable "api_gateway_name" {
  description = "API Gateway name for external access"
  type        = string
  default     = "fhir-converter-gateway"
}

variable "enable_api_gateway" {
  description = "Enable API Gateway for external access"
  type        = bool
  default     = true
}

# Tags and Labels
variable "common_labels" {
  description = "Common labels for all resources"
  type        = map(string)
  default = {
    managed_by = "terraform"
    project    = "fhir-converter"
  }
} 