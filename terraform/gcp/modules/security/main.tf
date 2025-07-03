# Cloud Run Service Account
resource "google_service_account" "cloud_run" {
  account_id   = var.cloud_run_sa_name
  display_name = "FHIR Converter Cloud Run Service Account"
  project      = var.project_id

  labels = var.labels
}

# Monitoring Service Account
resource "google_service_account" "monitoring" {
  count        = var.enable_monitoring ? 1 : 0
  account_id   = var.monitoring_sa_name
  display_name = "FHIR Converter Monitoring Service Account"
  project      = var.project_id

  labels = var.labels
}

# Cloud Run Service Account IAM
resource "google_project_iam_member" "cloud_run_logging" {
  project = var.project_id
  role    = "roles/logging.logWriter"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

resource "google_project_iam_member" "cloud_run_metrics" {
  project = var.project_id
  role    = "roles/monitoring.metricWriter"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

resource "google_project_iam_member" "cloud_run_trace" {
  project = var.project_id
  role    = "roles/cloudtrace.agent"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

# GCS bucket access for Cloud Run
resource "google_storage_bucket_iam_member" "cloud_run_storage_viewer" {
  bucket = var.template_bucket_name
  role   = "roles/storage.objectViewer"
  member = "serviceAccount:${google_service_account.cloud_run.email}"
}

# Monitoring Service Account IAM
resource "google_project_iam_member" "monitoring_logging" {
  count   = var.enable_monitoring ? 1 : 0
  project = var.project_id
  role    = "roles/logging.logWriter"
  member  = "serviceAccount:${google_service_account.monitoring[0].email}"
}

resource "google_project_iam_member" "monitoring_metrics" {
  count   = var.enable_monitoring ? 1 : 0
  project = var.project_id
  role    = "roles/monitoring.metricWriter"
  member  = "serviceAccount:${google_service_account.monitoring[0].email}"
}

resource "google_project_iam_member" "monitoring_admin" {
  count   = var.enable_monitoring ? 1 : 0
  project = var.project_id
  role    = "roles/monitoring.admin"
  member  = "serviceAccount:${google_service_account.monitoring[0].email}"
}

# Container Registry access
resource "google_project_iam_member" "cloud_run_registry" {
  project = var.project_id
  role    = "roles/storage.objectViewer"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

# VPC Connector access
resource "google_project_iam_member" "cloud_run_vpc" {
  count   = var.enable_vpc_connector ? 1 : 0
  project = var.project_id
  role    = "roles/compute.networkUser"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

# Workload Identity (if enabled)
resource "google_service_account_iam_binding" "workload_identity" {
  count              = var.enable_workload_identity ? 1 : 0
  service_account_id = google_service_account.cloud_run.name
  role               = "roles/iam.workloadIdentityUser"

  members = [
    "serviceAccount:${var.project_id}.svc.id.goog[default/default]"
  ]
}

# Organization Policy for Cloud Run
resource "google_organization_policy" "cloud_run_allowed_ingress" {
  count   = var.enable_organization_policies ? 1 : 0
  org_id  = var.organization_id
  constraint = "run.allowedIngress"

  list_policy {
    allow {
      all = true
    }
  }
}

# Organization Policy for VPC Service Controls
resource "google_organization_policy" "vpc_service_controls" {
  count   = var.enable_vpc_service_controls ? 1 : 0
  org_id  = var.organization_id
  constraint = "vpcaccess.allowedConnectors"

  list_policy {
    allow {
      all = true
    }
  }
} 