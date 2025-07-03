# Development Environment Configuration
organization_id = "your-org-id"
project_id      = "your-project-dev"
project_name    = "fhir-converter"
billing_account = "your-billing-account"

environment = "dev"
environment_suffix = ""

region = "us-central1"
zones  = ["us-central1-a", "us-central1-b"]

# Development-specific overrides
cloud_run_cpu           = "500m"
cloud_run_memory        = "1Gi"
cloud_run_max_instances = 10
cloud_run_min_instances = 0

enable_monitoring = true
enable_alerting   = false
enable_private_service = false

# Development allows public access
allowed_ip_ranges = ["0.0.0.0/0"]

# Common labels
common_labels = {
  managed_by = "terraform"
  project    = "fhir-converter"
  environment = "dev"
  owner       = "dev-team"
} 