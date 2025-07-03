#!/bin/bash

# FHIR Converter GCP Deployment Script
# This script automates the deployment process for all environments

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TERRAGRUNT_VERSION="0.45.0"
TERRAFORM_VERSION="1.5.0"

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to install Terraform
install_terraform() {
    print_status "Installing Terraform $TERRAFORM_VERSION..."
    
    if command_exists terraform; then
        CURRENT_VERSION=$(terraform version -json | jq -r '.terraform_version')
        if [[ "$CURRENT_VERSION" == "$TERRAFORM_VERSION" ]]; then
            print_success "Terraform $TERRAFORM_VERSION already installed"
            return 0
        fi
    fi
    
    # Install Terraform
    curl -fsSL https://apt.releases.hashicorp.com/gpg | sudo apt-key add -
    sudo apt-add-repository "deb [arch=amd64] https://apt.releases.hashicorp.com $(lsb_release -cs)"
    sudo apt-get update
    sudo apt-get install -y terraform=$TERRAFORM_VERSION
    
    print_success "Terraform $TERRAFORM_VERSION installed successfully"
}

# Function to install Terragrunt
install_terragrunt() {
    print_status "Installing Terragrunt $TERRAGRUNT_VERSION..."
    
    if command_exists terragrunt; then
        CURRENT_VERSION=$(terragrunt --version | grep -o '[0-9]\+\.[0-9]\+\.[0-9]\+')
        if [[ "$CURRENT_VERSION" == "$TERRAGRUNT_VERSION" ]]; then
            print_success "Terragrunt $TERRAGRUNT_VERSION already installed"
            return 0
        fi
    fi
    
    # Install Terragrunt
    curl -fsSL "https://github.com/gruntwork-io/terragrunt/releases/download/v${TERRAGRUNT_VERSION}/terragrunt_linux_amd64" -o terragrunt
    chmod +x terragrunt
    sudo mv terragrunt /usr/local/bin/
    
    print_success "Terragrunt $TERRAGRUNT_VERSION installed successfully"
}

# Function to install Google Cloud CLI
install_gcloud() {
    print_status "Installing Google Cloud CLI..."
    
    if command_exists gcloud; then
        print_success "Google Cloud CLI already installed"
        return 0
    fi
    
    # Install Google Cloud CLI
    curl https://sdk.cloud.google.com | bash
    exec -l $SHELL
    
    print_success "Google Cloud CLI installed successfully"
}

# Function to check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check for required commands
    local missing_commands=()
    
    if ! command_exists curl; then
        missing_commands+=("curl")
    fi
    
    if ! command_exists jq; then
        missing_commands+=("jq")
    fi
    
    if ! command_exists gsutil; then
        missing_commands+=("gsutil")
    fi
    
    if [[ ${#missing_commands[@]} -gt 0 ]]; then
        print_error "Missing required commands: ${missing_commands[*]}"
        print_status "Please install the missing commands and run the script again"
        exit 1
    fi
    
    print_success "All prerequisites are satisfied"
}

# Function to setup GCP authentication
setup_gcp_auth() {
    print_status "Setting up GCP authentication..."
    
    if [[ -z "$GOOGLE_APPLICATION_CREDENTIALS" ]]; then
        print_warning "GOOGLE_APPLICATION_CREDENTIALS not set"
        print_status "Please set the environment variable or run: gcloud auth application-default login"
        
        read -p "Do you want to authenticate with gcloud? (y/n): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            gcloud auth application-default login
        else
            print_error "Authentication required to continue"
            exit 1
        fi
    else
        print_success "Using service account key: $GOOGLE_APPLICATION_CREDENTIALS"
    fi
}

# Function to create GCS bucket for Terraform state
create_state_bucket() {
    local bucket_name="$1"
    
    print_status "Creating GCS bucket for Terraform state: $bucket_name"
    
    if gsutil ls -b "gs://$bucket_name" >/dev/null 2>&1; then
        print_success "Bucket $bucket_name already exists"
    else
        gsutil mb "gs://$bucket_name"
        gsutil versioning set on "gs://$bucket_name"
        print_success "Bucket $bucket_name created successfully"
    fi
}

# Function to validate environment configuration
validate_environment() {
    local environment="$1"
    local env_dir="$SCRIPT_DIR/environments/$environment"
    
    print_status "Validating $environment environment configuration..."
    
    if [[ ! -d "$env_dir" ]]; then
        print_error "Environment directory not found: $env_dir"
        return 1
    fi
    
    if [[ ! -f "$env_dir/terragrunt.hcl" ]]; then
        print_error "Terragrunt configuration not found: $env_dir/terragrunt.hcl"
        return 1
    fi
    
    print_success "$environment environment configuration is valid"
}

# Function to deploy environment
deploy_environment() {
    local environment="$1"
    local env_dir="$SCRIPT_DIR/environments/$environment"
    
    print_status "Deploying $environment environment..."
    
    cd "$env_dir"
    
    # Initialize Terragrunt
    print_status "Initializing Terragrunt..."
    terragrunt init
    
    # Plan deployment
    print_status "Planning deployment..."
    terragrunt plan -out=tfplan
    
    # Apply deployment
    print_status "Applying deployment..."
    terragrunt apply tfplan
    
    # Clean up plan file
    rm -f tfplan
    
    print_success "$environment environment deployed successfully"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [OPTIONS] COMMAND"
    echo ""
    echo "Commands:"
    echo "  install     Install required tools (Terraform, Terragrunt, gcloud)"
    echo "  setup       Setup GCP authentication and state bucket"
    echo "  validate    Validate all environment configurations"
    echo "  deploy      Deploy all environments"
    echo "  dev         Deploy development environment only"
    echo "  staging     Deploy staging environment only"
    echo "  prod        Deploy production environment only"
    echo "  destroy     Destroy all environments (use with caution)"
    echo ""
    echo "Options:"
    echo "  -h, --help  Show this help message"
    echo ""
    echo "Environment Variables:"
    echo "  GOOGLE_APPLICATION_CREDENTIALS  Path to GCP service account key"
    echo "  TF_STATE_BUCKET                GCS bucket name for Terraform state"
    echo ""
    echo "Examples:"
    echo "  $0 install"
    echo "  $0 setup"
    echo "  $0 deploy"
    echo "  $0 dev"
}

# Function to destroy environment
destroy_environment() {
    local environment="$1"
    local env_dir="$SCRIPT_DIR/environments/$environment"
    
    print_warning "Destroying $environment environment..."
    
    read -p "Are you sure you want to destroy the $environment environment? (y/n): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_status "Destroy cancelled"
        return 0
    fi
    
    cd "$env_dir"
    terragrunt destroy -auto-approve
    
    print_success "$environment environment destroyed successfully"
}

# Main script logic
main() {
    case "${1:-}" in
        install)
            check_prerequisites
            install_terraform
            install_terragrunt
            install_gcloud
            print_success "All tools installed successfully"
            ;;
        setup)
            setup_gcp_auth
            if [[ -n "$TF_STATE_BUCKET" ]]; then
                create_state_bucket "$TF_STATE_BUCKET"
            else
                print_warning "TF_STATE_BUCKET not set, skipping state bucket creation"
            fi
            print_success "Setup completed successfully"
            ;;
        validate)
            validate_environment "dev"
            validate_environment "staging"
            validate_environment "prod"
            print_success "All environment configurations are valid"
            ;;
        deploy)
            deploy_environment "dev"
            deploy_environment "staging"
            deploy_environment "prod"
            print_success "All environments deployed successfully"
            ;;
        dev)
            deploy_environment "dev"
            ;;
        staging)
            deploy_environment "staging"
            ;;
        prod)
            deploy_environment "prod"
            ;;
        destroy)
            destroy_environment "prod"
            destroy_environment "staging"
            destroy_environment "dev"
            print_success "All environments destroyed successfully"
            ;;
        -h|--help|"")
            show_usage
            ;;
        *)
            print_error "Unknown command: $1"
            show_usage
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@" 