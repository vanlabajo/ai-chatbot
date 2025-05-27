provider "azurerm" {
  features {}
}

provider "random" {}

resource "random_string" "random" {
  length  = 6
  special = false
  upper   = false
  numeric = true
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
    format = "OpenAI"
    name   = "gpt-4"
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

# App Service for UI (define this BEFORE referencing in API)
resource "azurerm_linux_web_app" "chatbot_ui" {
  name                = "ai-chatbot-ui-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  service_plan_id     = azurerm_service_plan.chatbot_plan.id
  site_config {
    always_on = true
    application_stack {
      node_version = "22-lts"
    }
    app_command_line = "npm install -g pnpm && pnpm install && pnpm start"
  }
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
    "AzureOpenAI__ApiKey"         = azurerm_cognitive_account.openai.primary_access_key
    "AzureOpenAI__Endpoint"       = azurerm_cognitive_account.openai.endpoint
    "AzureOpenAI__DeploymentName" = azurerm_cognitive_deployment.openai.name
    "AllowedOrigins__0"           = "https://${azurerm_linux_web_app.chatbot_ui.default_hostname}"
    "AllowedOrigins__1"           = ""
    "AllowedOrigins__2"           = ""
    "AllowedOrigins__3"           = ""
  }
}
