locals {
  # Environment-specific configurations
  environment_configs = {
    dev = {
      cloud_run_cpu           = "500m"
      cloud_run_memory        = "1Gi"
      cloud_run_max_instances = 10
      cloud_run_min_instances = 0
      enable_monitoring       = true
      enable_alerting         = false
      enable_private_service  = false
      template_bucket_location = "US"
    }
    staging = {
      cloud_run_cpu           = "1000m"
      cloud_run_memory        = "2Gi"
      cloud_run_max_instances = 50
      cloud_run_min_instances = 1
      enable_monitoring       = true
      enable_alerting         = true
      enable_private_service  = false
      template_bucket_location = "US"
    }
    prod = {
      cloud_run_cpu           = "2000m"
      cloud_run_memory        = "4Gi"
      cloud_run_max_instances = 200
      cloud_run_min_instances = 5
      enable_monitoring       = true
      enable_alerting         = true
      enable_private_service  = true
      template_bucket_location = "US"
    }
  }

  # Current environment config
  env_config = local.environment_configs[var.environment]

  # Resource naming
  name_prefix = "${var.project_name}-${var.environment}"
  name_suffix = var.environment_suffix != "" ? "-${var.environment_suffix}" : ""

  # Common labels
  labels = merge(var.common_labels, {
    environment = var.environment
    project     = var.project_name
  })

  # GCS bucket name
  template_bucket_name = var.template_bucket_name != "" ? var.template_bucket_name : "${local.name_prefix}-templates${local.name_suffix}"

  # Cloud Run service name
  cloud_run_service_name = "${local.name_prefix}-fhir-converter${local.name_suffix}"

  # VPC connector name
  vpc_connector_name = "${local.name_prefix}-vpc-connector${local.name_suffix}"

  # API Gateway name
  api_gateway_name = "${local.name_prefix}-gateway${local.name_suffix}"

  # Service account names
  cloud_run_sa_name = "${local.name_prefix}-cloud-run-sa${local.name_suffix}"
  monitoring_sa_name = "${local.name_prefix}-monitoring-sa${local.name_suffix}"

  # Container registry
  container_registry_url = "gcr.io/${var.project_id}"
  fhir_converter_image = replace(var.fhir_converter_image, "PROJECT_ID", var.project_id)
} 