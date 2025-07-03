output "cloud_run_service_account_email" {
  description = "Cloud Run Service Account email"
  value       = google_service_account.cloud_run.email
}

output "cloud_run_service_account_name" {
  description = "Cloud Run Service Account name"
  value       = google_service_account.cloud_run.name
}

output "monitoring_service_account_email" {
  description = "Monitoring Service Account email"
  value       = var.enable_monitoring ? google_service_account.monitoring[0].email : null
}

output "monitoring_service_account_name" {
  description = "Monitoring Service Account name"
  value       = var.enable_monitoring ? google_service_account.monitoring[0].name : null
} 