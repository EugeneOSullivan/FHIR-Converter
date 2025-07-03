variable "project_id" {
  description = "GCP Project ID"
  type        = string
}

variable "organization_id" {
  description = "GCP Organization ID"
  type        = string
}

variable "cloud_run_sa_name" {
  description = "Cloud Run Service Account name"
  type        = string
}

variable "monitoring_sa_name" {
  description = "Monitoring Service Account name"
  type        = string
}

variable "template_bucket_name" {
  description = "GCS bucket name for templates"
  type        = string
}

variable "enable_monitoring" {
  description = "Enable monitoring"
  type        = bool
  default     = true
}

variable "enable_vpc_connector" {
  description = "Enable VPC connector"
  type        = bool
  default     = true
}

variable "enable_workload_identity" {
  description = "Enable Workload Identity"
  type        = bool
  default     = false
}

variable "enable_organization_policies" {
  description = "Enable organization policies"
  type        = bool
  default     = false
}

variable "enable_vpc_service_controls" {
  description = "Enable VPC Service Controls"
  type        = bool
  default     = false
}

variable "labels" {
  description = "Resource labels"
  type        = map(string)
  default     = {}
} 