# Configure the Google Provider
provider "google" {
  project = var.project_id
  region  = var.region
}

provider "google-beta" {
  project = var.project_id
  region  = var.region
}

# Enable required APIs
resource "google_project_service" "required_apis" {
  for_each = toset([
    "cloudrun.googleapis.com",
    "cloudbuild.googleapis.com",
    "containerregistry.googleapis.com",
    "storage.googleapis.com",
    "vpcaccess.googleapis.com",
    "compute.googleapis.com",
    "monitoring.googleapis.com",
    "logging.googleapis.com",
    "bigquery.googleapis.com",
    "apigateway.googleapis.com",
    "servicenetworking.googleapis.com"
  ])

  project = var.project_id
  service = each.value

  disable_dependent_services = false
  disable_on_destroy         = false
}

# Networking Module
module "networking" {
  source = "./modules/networking"

  project_id           = var.project_id
  region               = var.region
  vpc_name             = var.vpc_name
  subnet_cidr          = var.subnet_cidr
  vpc_connector_name   = local.vpc_connector_name
  enable_vpc_connector = var.enable_vpc_connector
  enable_cloud_nat     = true
  labels               = local.labels

  depends_on = [google_project_service.required_apis]
}

# Security Module
module "security" {
  source = "./modules/security"

  project_id                    = var.project_id
  organization_id               = var.organization_id
  cloud_run_sa_name             = local.cloud_run_sa_name
  monitoring_sa_name            = local.monitoring_sa_name
  template_bucket_name          = local.template_bucket_name
  enable_monitoring             = local.env_config.enable_monitoring
  enable_vpc_connector          = var.enable_vpc_connector
  enable_workload_identity      = false
  enable_organization_policies  = false
  enable_vpc_service_controls   = false
  labels                        = local.labels

  depends_on = [google_project_service.required_apis]
}

# FHIR Converter Module
module "fhir_converter" {
  source = "./modules/fhir-converter"

  project_id                    = var.project_id
  region                        = var.region
  environment                   = var.environment
  cloud_run_service_name        = local.cloud_run_service_name
  cloud_run_service_account_email = module.security.cloud_run_service_account_email
  cloud_run_cpu                 = local.env_config.cloud_run_cpu
  cloud_run_memory              = local.env_config.cloud_run_memory
  cloud_run_min_instances       = local.env_config.cloud_run_min_instances
  cloud_run_max_instances       = local.env_config.cloud_run_max_instances
  fhir_converter_image          = local.fhir_converter_image
  template_bucket_name          = local.template_bucket_name
  template_bucket_location      = local.env_config.template_bucket_location
  vpc_connector_name            = local.vpc_connector_name
  enable_private_service        = local.env_config.enable_private_service
  enable_api_gateway            = var.enable_api_gateway
  api_gateway_name              = local.api_gateway_name
  allowed_ip_ranges             = var.environment == "prod" ? ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"] : ["0.0.0.0/0"]
  labels                        = local.labels

  depends_on = [
    module.networking,
    module.security,
    google_project_service.required_apis
  ]
}

# Monitoring Module
module "monitoring" {
  source = "./modules/monitoring"

  project_id              = var.project_id
  project_name            = var.project_name
  region                  = var.region
  cloud_run_service_name  = local.cloud_run_service_name
  cloud_run_service_url   = module.fhir_converter.cloud_run_service_url
  enable_monitoring       = local.env_config.enable_monitoring
  enable_alerting         = local.env_config.enable_alerting
  notification_channels   = []
  labels                  = local.labels

  depends_on = [
    module.fhir_converter,
    google_project_service.required_apis
  ]
} 