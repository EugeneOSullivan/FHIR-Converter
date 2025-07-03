variable "project_id" {
  description = "GCP Project ID"
  type        = string
}

variable "region" {
  description = "GCP Region"
  type        = string
}

variable "vpc_name" {
  description = "VPC name"
  type        = string
}

variable "subnet_cidr" {
  description = "Subnet CIDR block"
  type        = string
}

variable "vpc_connector_name" {
  description = "VPC Connector name"
  type        = string
}

variable "vpc_connector_cidr" {
  description = "VPC Connector CIDR block"
  type        = string
  default     = "10.8.0.0/28"
}

variable "enable_vpc_connector" {
  description = "Enable VPC Connector"
  type        = bool
  default     = true
}

variable "enable_cloud_nat" {
  description = "Enable Cloud NAT"
  type        = bool
  default     = true
}

variable "labels" {
  description = "Resource labels"
  type        = map(string)
  default     = {}
} 