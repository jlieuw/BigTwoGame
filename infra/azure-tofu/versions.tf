terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }

  # Azure Blob Storage remote state.
  # resource_group_name and container_name are fixed; storage_account_name is
  # passed at tofu init time via -backend-config or the ARM_STORAGE_ACCOUNT_NAME
  # env var, because the account name must be globally unique and is generated
  # by the bootstrap script.
  backend "azurerm" {
    resource_group_name = "bigtwo-state-rg"
    container_name      = "tfstate"
    key                 = "prod/terraform.tfstate"
    # storage_account_name → set via -backend-config="storage_account_name=..."
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.azure_subscription_id
}

provider "random" {}
