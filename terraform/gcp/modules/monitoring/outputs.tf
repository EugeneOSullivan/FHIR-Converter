output "dashboard_url" {
  description = "Cloud Monitoring dashboard URL"
  value       = var.enable_monitoring ? "https://console.cloud.google.com/monitoring/dashboards/custom/${google_monitoring_dashboard.fhir_converter_dashboard[0].dashboard_id}?project=${var.project_id}" : null
}

output "uptime_check_url" {
  description = "Uptime check URL"
  value       = var.enable_monitoring ? "https://console.cloud.google.com/monitoring/uptime?project=${var.project_id}" : null
}

output "logs_dataset_id" {
  description = "BigQuery logs dataset ID"
  value       = var.enable_monitoring ? google_bigquery_dataset.logs[0].dataset_id : null
}

output "alert_policies" {
  description = "Alert policy names"
  value       = var.enable_alerting ? [
    google_monitoring_alert_policy.high_error_rate[0].display_name,
    google_monitoring_alert_policy.high_latency[0].display_name,
    google_monitoring_alert_policy.service_unavailable[0].display_name
  ] : []
} 