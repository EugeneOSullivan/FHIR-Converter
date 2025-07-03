# Production Environment Configuration
organization_id = "your-org-id"
project_id      = "your-project-prod"
project_name    = "fhir-converter"
billing_account = "your-billing-account"

environment = "prod"
environment_suffix = ""

region = "us-central1"
zones  = ["us-central1-a", "us-central1-b", "us-central1-c"]

# Production-specific overrides
cloud_run_cpu           = "2000m"
cloud_run_memory        = "4Gi"
cloud_run_max_instances = 200
cloud_run_min_instances = 5

enable_monitoring = true
enable_alerting   = true
enable_private_service = true

# Production restricts to specific IP ranges
allowed_ip_ranges = [
  "10.0.0.0/8",
  "172.16.0.0/12",
  "192.168.0.0/16",
  "your-vpn-ip/32",
  "your-load-balancer-ip/32"
]

# Common labels
common_labels = {
  managed_by = "terraform"
  project    = "fhir-converter"
  environment = "prod"
  owner       = "ops-team"
  cost_center = "healthcare"
  compliance  = "hipaa"
} 