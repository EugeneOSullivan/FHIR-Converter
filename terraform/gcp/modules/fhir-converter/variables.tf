variable "project_id" {
  description = "GCP Project ID"
  type        = string
}

variable "region" {
  description = "GCP Region"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "cloud_run_service_name" {
  description = "Cloud Run service name"
  type        = string
}

variable "cloud_run_service_account_email" {
  description = "Cloud Run service account email"
  type        = string
}

variable "cloud_run_cpu" {
  description = "Cloud Run CPU allocation"
  type        = string
}

variable "cloud_run_memory" {
  description = "Cloud Run memory allocation"
  type        = string
}

variable "cloud_run_min_instances" {
  description = "Cloud Run minimum instances"
  type        = number
}

variable "cloud_run_max_instances" {
  description = "Cloud Run maximum instances"
  type        = number
}

variable "fhir_converter_image" {
  description = "FHIR Converter Docker image"
  type        = string
}

variable "template_bucket_name" {
  description = "GCS bucket name for templates"
  type        = string
}

variable "template_bucket_location" {
  description = "GCS bucket location"
  type        = string
}

variable "vpc_connector_name" {
  description = "VPC connector name"
  type        = string
}

variable "enable_private_service" {
  description = "Enable private Cloud Run service"
  type        = bool
  default     = false
}

variable "enable_api_gateway" {
  description = "Enable API Gateway"
  type        = bool
  default     = true
}

variable "api_gateway_name" {
  description = "API Gateway name"
  type        = string
}

variable "allowed_ip_ranges" {
  description = "Allowed IP ranges for Cloud Armor"
  type        = list(string)
  default     = ["0.0.0.0/0"]
}

variable "labels" {
  description = "Resource labels"
  type        = map(string)
  default     = {}
} 