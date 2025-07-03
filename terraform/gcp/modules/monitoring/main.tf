# Cloud Monitoring Workspace
resource "google_monitoring_monitored_project" "primary" {
  count   = var.enable_monitoring ? 1 : 0
  metrics_scope = var.project_id
}

# Logging Sink for Cloud Run
resource "google_logging_project_sink" "cloud_run_sink" {
  count   = var.enable_monitoring ? 1 : 0
  name    = "${var.project_name}-cloud-run-logs"
  project = var.project_id
  destination = "bigquery.googleapis.com/projects/${var.project_id}/datasets/${google_bigquery_dataset.logs[0].dataset_id}"

  filter = "resource.type = \"cloud_run_revision\" AND resource.labels.service_name = \"${var.cloud_run_service_name}\""

  unique_writer_identity = true
}

# BigQuery Dataset for logs
resource "google_bigquery_dataset" "logs" {
  count   = var.enable_monitoring ? 1 : 0
  dataset_id = "${var.project_name}_logs"
  project    = var.project_id
  location   = var.region

  labels = var.labels
}

# Cloud Monitoring Alert Policies
resource "google_monitoring_alert_policy" "high_error_rate" {
  count   = var.enable_alerting ? 1 : 0
  display_name = "High Error Rate - FHIR Converter"
  project      = var.project_id
  combiner     = "OR"
  enabled      = true

  conditions {
    display_name = "Error rate is high"

    condition_threshold {
      filter = "metric.type=\"run.googleapis.com/request_count\" resource.type=\"cloud_run_revision\" resource.label.\"service_name\"=\"${var.cloud_run_service_name}\""

      comparison = "COMPARISON_GREATER_THAN"
      threshold_value = 0.05

      duration = "300s"

      aggregations {
        alignment_period = "60s"
        per_series_aligner = "ALIGN_RATE"
        cross_series_reducer = "REDUCE_MEAN"
      }
    }
  }

  notification_channels = var.notification_channels

  documentation {
    content = "The FHIR Converter service is experiencing a high error rate (>5% over 5 minutes). Please investigate the service logs and health."
  }
}

resource "google_monitoring_alert_policy" "high_latency" {
  count   = var.enable_alerting ? 1 : 0
  display_name = "High Latency - FHIR Converter"
  project      = var.project_id
  combiner     = "OR"
  enabled      = true

  conditions {
    display_name = "Request latency is high"

    condition_threshold {
      filter = "metric.type=\"run.googleapis.com/request_latencies\" resource.type=\"cloud_run_revision\" resource.label.\"service_name\"=\"${var.cloud_run_service_name}\""

      comparison = "COMPARISON_GREATER_THAN"
      threshold_value = 10000

      duration = "300s"

      aggregations {
        alignment_period = "60s"
        per_series_aligner = "ALIGN_PERCENTILE_95"
        cross_series_reducer = "REDUCE_MEAN"
      }
    }
  }

  notification_channels = var.notification_channels

  documentation {
    content = "The FHIR Converter service is experiencing high latency (>10s 95th percentile over 5 minutes). Please investigate performance issues."
  }
}

resource "google_monitoring_alert_policy" "service_unavailable" {
  count   = var.enable_alerting ? 1 : 0
  display_name = "Service Unavailable - FHIR Converter"
  project      = var.project_id
  combiner     = "OR"
  enabled      = true

  conditions {
    display_name = "Service is unavailable"

    condition_threshold {
      filter = "metric.type=\"run.googleapis.com/request_count\" resource.type=\"cloud_run_revision\" resource.label.\"service_name\"=\"${var.cloud_run_service_name}\""

      comparison = "COMPARISON_LESS_THAN"
      threshold_value = 1

      duration = "60s"

      aggregations {
        alignment_period = "60s"
        per_series_aligner = "ALIGN_RATE"
        cross_series_reducer = "REDUCE_SUM"
      }
    }
  }

  notification_channels = var.notification_channels

  documentation {
    content = "The FHIR Converter service appears to be unavailable (no requests in the last minute). Please check service health and logs."
  }
}

# Cloud Monitoring Uptime Checks
resource "google_monitoring_uptime_check_config" "health_check" {
  count   = var.enable_monitoring ? 1 : 0
  display_name = "FHIR Converter Health Check"
  project      = var.project_id

  http_check {
    uri = "${var.cloud_run_service_url}/health"
    port = 443
    use_ssl = true
    validate_ssl = true
  }

  monitored_resource {
    type = "uptime_url"
    labels = {
      host = replace(var.cloud_run_service_url, "https://", "")
    }
  }

  timeout = "5s"
  period = "60s"
}

# Cloud Monitoring Dashboard
resource "google_monitoring_dashboard" "fhir_converter_dashboard" {
  count   = var.enable_monitoring ? 1 : 0
  dashboard_json = jsonencode({
    displayName = "FHIR Converter Dashboard"
    gridLayout = {
      widgets = [
        {
          title = "Request Count"
          xyChart = {
            dataSets = [{
              timeSeriesQuery = {
                timeSeriesFilter = {
                  filter = "metric.type=\"run.googleapis.com/request_count\" resource.type=\"cloud_run_revision\" resource.label.\"service_name\"=\"${var.cloud_run_service_name}\""
                }
              }
            }]
          }
        },
        {
          title = "Request Latency"
          xyChart = {
            dataSets = [{
              timeSeriesQuery = {
                timeSeriesFilter = {
                  filter = "metric.type=\"run.googleapis.com/request_latencies\" resource.type=\"cloud_run_revision\" resource.label.\"service_name\"=\"${var.cloud_run_service_name}\""
                }
              }
            }]
          }
        },
        {
          title = "Error Rate"
          xyChart = {
            dataSets = [{
              timeSeriesQuery = {
                timeSeriesFilter = {
                  filter = "metric.type=\"run.googleapis.com/request_count\" resource.type=\"cloud_run_revision\" resource.label.\"service_name\"=\"${var.cloud_run_service_name}\""
                }
              }
            }]
          }
        },
        {
          title = "CPU Utilization"
          xyChart = {
            dataSets = [{
              timeSeriesQuery = {
                timeSeriesFilter = {
                  filter = "metric.type=\"run.googleapis.com/container/cpu/utilizations\" resource.type=\"cloud_run_revision\" resource.label.\"service_name\"=\"${var.cloud_run_service_name}\""
                }
              }
            }]
          }
        },
        {
          title = "Memory Utilization"
          xyChart = {
            dataSets = [{
              timeSeriesQuery = {
                timeSeriesFilter = {
                  filter = "metric.type=\"run.googleapis.com/container/memory/utilizations\" resource.type=\"cloud_run_revision\" resource.label.\"service_name\"=\"${var.cloud_run_service_name}\""
                }
              }
            }]
          }
        }
      ]
    }
  })
} 