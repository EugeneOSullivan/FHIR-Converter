output "vpc_id" {
  description = "VPC ID"
  value       = google_compute_network.vpc.id
}

output "vpc_name" {
  description = "VPC name"
  value       = google_compute_network.vpc.name
}

output "subnet_id" {
  description = "Subnet ID"
  value       = google_compute_subnetwork.subnet.id
}

output "subnet_name" {
  description = "Subnet name"
  value       = google_compute_subnetwork.subnet.name
}

output "vpc_connector_id" {
  description = "VPC Connector ID"
  value       = var.enable_vpc_connector ? google_vpc_access_connector.connector[0].id : null
}

output "vpc_connector_name" {
  description = "VPC Connector name"
  value       = var.enable_vpc_connector ? google_vpc_access_connector.connector[0].name : null
}

output "router_id" {
  description = "Cloud Router ID"
  value       = var.enable_cloud_nat ? google_compute_router.router[0].id : null
}

output "nat_id" {
  description = "Cloud NAT ID"
  value       = var.enable_cloud_nat ? google_compute_router_nat.nat[0].id : null
} 