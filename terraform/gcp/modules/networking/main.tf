# VPC
resource "google_compute_network" "vpc" {
  name                    = var.vpc_name
  auto_create_subnetworks = false
  project                 = var.project_id

  labels = var.labels
}

# Subnet
resource "google_compute_subnetwork" "subnet" {
  name          = "${var.vpc_name}-subnet"
  ip_cidr_range = var.subnet_cidr
  region        = var.region
  network       = google_compute_network.vpc.id
  project       = var.project_id

  # Enable flow logs for network monitoring
  log_config {
    aggregation_interval = "INTERVAL_5_SEC"
    flow_sampling        = 0.5
    metadata            = "INCLUDE_ALL_METADATA"
  }

  labels = var.labels
}

# VPC Connector for Cloud Run
resource "google_vpc_access_connector" "connector" {
  count         = var.enable_vpc_connector ? 1 : 0
  name          = var.vpc_connector_name
  region        = var.region
  project       = var.project_id
  ip_cidr_range = var.vpc_connector_cidr
  network       = google_compute_network.vpc.name

  labels = var.labels
}

# Firewall rule for Cloud Run VPC connector
resource "google_compute_firewall" "vpc_connector" {
  count   = var.enable_vpc_connector ? 1 : 0
  name    = "${var.vpc_connector_name}-firewall"
  network = google_compute_network.vpc.name
  project = var.project_id

  allow {
    protocol = "tcp"
    ports    = ["22", "443"]
  }

  source_ranges = [var.vpc_connector_cidr]
  target_tags   = ["vpc-connector"]

  labels = var.labels
}

# Cloud NAT for private instances
resource "google_compute_router" "router" {
  count   = var.enable_cloud_nat ? 1 : 0
  name    = "${var.vpc_name}-router"
  region  = var.region
  project = var.project_id
  network = google_compute_network.vpc.id

  labels = var.labels
}

resource "google_compute_router_nat" "nat" {
  count                              = var.enable_cloud_nat ? 1 : 0
  name                               = "${var.vpc_name}-nat"
  router                             = google_compute_router.router[0].name
  region                             = var.region
  project                            = var.project_id
  nat_ip_allocate_option             = "AUTO_ONLY"
  source_subnetwork_ip_ranges_to_nat = "ALL_SUBNETWORKS_ALL_IP_RANGES"

  log_config {
    enable = true
    filter = "ERRORS_ONLY"
  }
} 