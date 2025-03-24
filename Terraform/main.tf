provider "azurerm" {
  features {}
}

provider "random" {}

resource "random_string" "random" {
  length  = 6
  special = false
  upper   = false
  numeric  = true
}

# Resource Group
resource "azurerm_resource_group" "ai_rg" {
  name     = "rg-ai-chatbot-${random_string.random.result}"
  location = "Canada East"
}

# OpenAI Cognitive Services
resource "azurerm_cognitive_account" "openai" {
  name                = "openai-chatbot-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  kind                = "OpenAI"
  sku_name            = "S0"
}

# OpenAI Cognitive Deployment
resource "azurerm_cognitive_deployment" "openai" {
  name                 = "openai-chatbot-deployment-${random_string.random.result}"
  cognitive_account_id = azurerm_cognitive_account.openai.id
  model {
    format  = "OpenAI"
    name    = "gpt-4"
  }

  sku {
    name = "Standard"
  }
}

# App Service Plan
resource "azurerm_service_plan" "chatbot_plan" {
  name                = "chatbot-appservice-plan-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  os_type             = "Linux"
  sku_name            = "B1"
}

# App Service for API
resource "azurerm_linux_web_app" "chatbot_api" {
  name                = "ai-chatbot-api-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  service_plan_id     = azurerm_service_plan.chatbot_plan.id

  site_config {
    always_on = true
    application_stack {
      dotnet_version = "8.0"
    }
  }

  app_settings = {
    "OPENAI_API_KEY" = azurerm_cognitive_account.openai.primary_access_key
    "OPENAI_ENDPOINT" = azurerm_cognitive_account.openai.endpoint
    "OPENAI_DEPLOYMENT_NAME" = azurerm_cognitive_deployment.openai.name
  }
}

# Azure Load Testing
resource "azurerm_load_test" "chatbot_loadtest" {
  name                = "ai-chatbot-loadtest-${random_string.random.result}"
  location            = "canadacentral"
  resource_group_name = azurerm_resource_group.ai_rg.name
}