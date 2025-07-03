# GCS Bucket for FHIR Templates
resource "google_storage_bucket" "templates" {
  name          = var.template_bucket_name
  location      = var.template_bucket_location
  project       = var.project_id
  force_destroy = var.environment == "dev" ? true : false

  # Versioning for production
  versioning {
    enabled = var.environment == "prod" ? true : false
  }

  # Lifecycle rules
  lifecycle_rule {
    condition {
      age = 365
    }
    action {
      type = "Delete"
    }
  }

  # Uniform bucket-level access
  uniform_bucket_level_access = true

  # Public access prevention
  public_access_prevention = "enforced"

  labels = var.labels
}

# Cloud Run Service
resource "google_cloud_run_v2_service" "fhir_converter" {
  name     = var.cloud_run_service_name
  location = var.region
  project  = var.project_id

  template {
    scaling {
      min_instance_count = var.cloud_run_min_instances
      max_instance_count = var.cloud_run_max_instances
    }

    vpc_access {
      connector = var.vpc_connector_name
      egress    = "ALL_TRAFFIC"
    }

    containers {
      image = var.fhir_converter_image

      resources {
        limits = {
          cpu    = var.cloud_run_cpu
          memory = var.cloud_run_memory
        }
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "prod" ? "Production" : "Development"
      }

      env {
        name  = "CloudStorageConfiguration__Provider"
        value = "GCP"
      }

      env {
        name  = "CloudStorageConfiguration__GcpBucketName"
        value = google_storage_bucket.templates.name
      }

      env {
        name  = "CloudStorageConfiguration__GcpProjectId"
        value = var.project_id
      }

      env {
        name  = "Logging__LogLevel__Default"
        value = var.environment == "prod" ? "Information" : "Debug"
      }

      env {
        name  = "Logging__LogLevel__Microsoft"
        value = var.environment == "prod" ? "Warning" : "Information"
      }

      # Health check
      startup_probe {
        http_get {
          path = "/health"
        }
        initial_delay_seconds = 10
        timeout_seconds       = 3
        period_seconds        = 5
        failure_threshold     = 3
      }

      liveness_probe {
        http_get {
          path = "/health"
        }
        timeout_seconds   = 3
        period_seconds    = 10
        failure_threshold = 3
      }

      readiness_probe {
        http_get {
          path = "/health"
        }
        timeout_seconds   = 3
        period_seconds    = 5
        failure_threshold = 3
      }
    }

    service_account = var.cloud_run_service_account_email
  }

  traffic {
    type    = "TRAFFIC_TARGET_ALLOCATION_TYPE_LATEST"
    percent = 100
  }

  labels = var.labels
}

# Cloud Run IAM - Public access (if not private)
resource "google_cloud_run_service_iam_member" "public_access" {
  count    = var.enable_private_service ? 0 : 1
  location = google_cloud_run_v2_service.fhir_converter.location
  project  = google_cloud_run_v2_service.fhir_converter.project
  service  = google_cloud_run_v2_service.fhir_converter.name
  role     = "roles/run.invoker"
  member   = "allUsers"
}

# API Gateway (if enabled)
resource "google_api_gateway_api" "api" {
  count       = var.enable_api_gateway ? 1 : 0
  provider    = google-beta
  api_id      = var.api_gateway_name
  project     = var.project_id
  display_name = "FHIR Converter API Gateway"
}

resource "google_api_gateway_api_config" "api_cfg" {
  count       = var.enable_api_gateway ? 1 : 0
  provider    = google-beta
  api         = google_api_gateway_api.api[0].api_id
  api_config_id = "${var.api_gateway_name}-config"
  project     = var.project_id

  openapi_documents {
    document {
      path = "spec.yaml"
      contents = base64encode(templatefile("${path.module}/api-spec.yaml", {
        cloud_run_url = google_cloud_run_v2_service.fhir_converter.uri
      }))
    }
  }
}

resource "google_api_gateway_gateway" "gateway" {
  count       = var.enable_api_gateway ? 1 : 0
  provider    = google-beta
  region      = var.region
  api_config  = google_api_gateway_api_config.api_cfg[0].id
  gateway_id  = var.api_gateway_name
  project     = var.project_id
  display_name = "FHIR Converter Gateway"
}

# Cloud Armor security policy (for production)
resource "google_compute_security_policy" "security_policy" {
  count   = var.environment == "prod" ? 1 : 0
  name    = "${var.cloud_run_service_name}-security-policy"
  project = var.project_id

  rule {
    action   = "deny(403)"
    priority = "1000"
    match {
      versioned_expr = "SRC_IPS_V1"
      config {
        src_ip_ranges = ["*"]
      }
    }
    description = "Deny access by default"
  }

  rule {
    action   = "allow"
    priority = "2000"
    match {
      versioned_expr = "SRC_IPS_V1"
      config {
        src_ip_ranges = var.allowed_ip_ranges
      }
    }
    description = "Allow access from specified IP ranges"
  }

  rule {
    action   = "allow"
    priority = "3000"
    match {
      expr {
        expression = "evaluatePreconfiguredExpr('sqli-stable')"
      }
    }
    description = "Prevent SQL injection"
  }

  rule {
    action   = "allow"
    priority = "4000"
    match {
      expr {
        expression = "evaluatePreconfiguredExpr('xss-stable')"
      }
    }
    description = "Prevent XSS attacks"
  }
} 