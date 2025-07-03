output "cloud_run_service_url" {
  description = "Cloud Run service URL"
  value       = google_cloud_run_v2_service.fhir_converter.uri
}

output "cloud_run_service_name" {
  description = "Cloud Run service name"
  value       = google_cloud_run_v2_service.fhir_converter.name
}

output "template_bucket_name" {
  description = "Template bucket name"
  value       = google_storage_bucket.templates.name
}

output "template_bucket_url" {
  description = "Template bucket URL"
  value       = google_storage_bucket.templates.url
}

output "api_gateway_url" {
  description = "API Gateway URL"
  value       = var.enable_api_gateway ? google_api_gateway_gateway.gateway[0].default_hostname : null
}

output "api_gateway_name" {
  description = "API Gateway name"
  value       = var.enable_api_gateway ? google_api_gateway_gateway.gateway[0].name : null
}

output "security_policy_name" {
  description = "Cloud Armor security policy name"
  value       = var.environment == "prod" ? google_compute_security_policy.security_policy[0].name : null
} 