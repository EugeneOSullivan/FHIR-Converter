include "root" {
  path = find_in_parent_folders()
}

# Staging environment configuration
inputs = {
  organization_id = "your-org-id"
  project_id      = "your-project-staging"
  project_name    = "fhir-converter"
  
  environment = "staging"
  environment_suffix = ""
  
  # Staging-specific settings
  cloud_run_cpu           = "1000m"
  cloud_run_memory        = "2Gi"
  cloud_run_max_instances = 50
  cloud_run_min_instances = 1
  
  enable_monitoring = true
  enable_alerting   = true
  enable_private_service = false
  
  # Staging allows specific IP ranges
  allowed_ip_ranges = [
    "10.0.0.0/8",
    "172.16.0.0/12", 
    "192.168.0.0/16",
    "your-office-ip/32"
  ]
  
  # Staging labels
  common_labels = {
    managed_by = "terragrunt"
    project    = "fhir-converter"
    environment = "staging"
    owner       = "qa-team"
  }
} 