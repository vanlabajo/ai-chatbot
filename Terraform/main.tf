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

# User Assigned Managed Identity
resource "azurerm_user_assigned_identity" "chatbot_ui_identity" {
  name                = "chatbot-ui-identity-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
}

# Azure Container Registry (ACR)
resource "azurerm_container_registry" "chatbot_acr" {
  name                = "chatbotacr${random_string.random.result}"
  resource_group_name = azurerm_resource_group.ai_rg.name
  location            = azurerm_resource_group.ai_rg.location
  sku                 = "Basic"
  admin_enabled       = true
}

# Assign AcrPull role to the managed identity
resource "azurerm_role_assignment" "acr_pull" {
  scope                = azurerm_container_registry.chatbot_acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.chatbot_ui_identity.principal_id
}

# App Service for UI using Docker image and managed identity
resource "azurerm_linux_web_app" "chatbot_ui" {
  name                = "ai-chatbot-ui-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  service_plan_id     = azurerm_service_plan.chatbot_plan.id

  site_config {
    always_on = true
    application_stack {
      docker_image_name   = "${azurerm_container_registry.chatbot_acr.name}.azurecr.io/ai-chatbot-ui:latest"
      docker_registry_url = "https://${azurerm_container_registry.chatbot_acr.login_server}"
    }
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.chatbot_ui_identity.id]
  }
}
