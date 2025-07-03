include "root" {
  path = find_in_parent_folders()
}

# Development environment configuration
inputs = {
  organization_id = "your-org-id"
  project_id      = "your-project-dev"
  project_name    = "fhir-converter"
  
  environment = "dev"
  environment_suffix = ""
  
  # Development-specific settings
  cloud_run_cpu           = "500m"
  cloud_run_memory        = "1Gi"
  cloud_run_max_instances = 10
  cloud_run_min_instances = 0
  
  enable_monitoring = true
  enable_alerting   = false
  enable_private_service = false
  
  # Development allows public access
  allowed_ip_ranges = ["0.0.0.0/0"]
  
  # Development labels
  common_labels = {
    managed_by = "terragrunt"
    project    = "fhir-converter"
    environment = "dev"
    owner       = "dev-team"
  }
} 