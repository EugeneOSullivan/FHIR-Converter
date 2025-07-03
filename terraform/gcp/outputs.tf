output "cloud_run_service_url" {
  description = "Cloud Run service URL"
  value       = module.fhir_converter.cloud_run_service_url
}

output "cloud_run_service_name" {
  description = "Cloud Run service name"
  value       = module.fhir_converter.cloud_run_service_name
}

output "api_gateway_url" {
  description = "API Gateway URL"
  value       = module.fhir_converter.api_gateway_url
}

output "template_bucket_name" {
  description = "Template bucket name"
  value       = module.fhir_converter.template_bucket_name
}

output "template_bucket_url" {
  description = "Template bucket URL"
  value       = module.fhir_converter.template_bucket_url
}

output "vpc_name" {
  description = "VPC name"
  value       = module.networking.vpc_name
}

output "vpc_connector_name" {
  description = "VPC connector name"
  value       = module.networking.vpc_connector_name
}

output "cloud_run_service_account_email" {
  description = "Cloud Run service account email"
  value       = module.security.cloud_run_service_account_email
}

output "monitoring_dashboard_url" {
  description = "Cloud Monitoring dashboard URL"
  value       = module.monitoring.dashboard_url
}

output "uptime_check_url" {
  description = "Uptime check URL"
  value       = module.monitoring.uptime_check_url
}

output "logs_dataset_id" {
  description = "BigQuery logs dataset ID"
  value       = module.monitoring.logs_dataset_id
}

output "alert_policies" {
  description = "Alert policy names"
  value       = module.monitoring.alert_policies
}

output "environment_info" {
  description = "Environment information"
  value = {
    environment = var.environment
    region      = var.region
    project_id  = var.project_id
    project_name = var.project_name
  }
} 