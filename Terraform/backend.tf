terraform {
  backend "azurerm" {
    use_cli               = true
    resource_group_name   = "rg-tf-state"
    storage_account_name  = "aichatbot4133"
    container_name        = "tfstate"
    key                   = "dev.terraform.tfstate"
  }
}