terraform {
  required_version = "~> 1.0"

  required_providers {
    azurerm = {
      version = "~> 4.0"
      source  = "hashicorp/azurerm"
    }
    random = {
      version = "~> 3.0"
      source  = "hashicorp/random"
    }
    azuread = {
      version = "~> 2.0"
      source  = "hashicorp/azuread"
    }
    time = {
      version = "~> 0.9"
      source  = "hashicorp/time"
    }
  }

  backend "azurerm" {
    use_cli              = true
    resource_group_name  = "rg-tf-state"
    storage_account_name = "aichatbot4133"
    container_name       = "tfstate"
    key                  = "dev.terraform.tfstate"
  }
}

provider "azurerm" {
  features {}
}
provider "random" {}
provider "azuread" {}
provider "time" {}
