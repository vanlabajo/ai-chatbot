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

data "azurerm_client_config" "current" {}

# Resource Group
resource "azurerm_resource_group" "ai_rg" {
  name     = "rg-ai-chatbot-${random_string.random.result}"
  location = "Canada East"
}

# OpenAI Cognitive Services
resource "azurerm_cognitive_account" "openai" {
  name                = "openai-chatbot-${random_string.random.result}"
  location            = "East US 2"
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
    name    = "model-router"
    version = "2025-05-19"
  }

  sku {
    name = "GlobalStandard"
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
    "AzureOpenAI__ApiKey"                   = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.openai_api_key.id})"
    "AzureOpenAI__Endpoint"                 = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.openai_endpoint.id})"
    "AzureOpenAI__DeploymentName"           = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.openai_deployment_name.id})"
    "AllowedOrigins__0"                     = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.allowed_origins_0.id})"
    "AllowedOrigins__1"                     = ""
    "AllowedOrigins__2"                     = ""
    "AllowedOrigins__3"                     = ""
    "ApplicationInsights__ConnectionString" = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.appinsights_connection_string.id})"
    "CosmosDb__Endpoint"                    = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.cosmosdb_endpoint.id})"
    "CosmosDb__DatabaseName"                = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.cosmosdb_database_name.id})"
    "CosmosDb__ContainerName"               = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.cosmosdb_container_name.id})"
  }
}

# User Assigned Managed Identity
resource "azurerm_user_assigned_identity" "chatbot_ui_identity" {
  name                = "chatbot-ui-identity-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
}

resource "azurerm_user_assigned_identity" "chatbot_api_identity" {
  name                = "chatbot-api-identity-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
}

# Azure Container Registry (ACR)
resource "azurerm_container_registry" "chatbot_acr" {
  name                = "chatbotacr${random_string.random.result}"
  resource_group_name = azurerm_resource_group.ai_rg.name
  location            = azurerm_resource_group.ai_rg.location
  sku                 = "Basic"
  admin_enabled       = false
}

# Assign AcrPull role to the managed identity
resource "azurerm_role_assignment" "acr_pull" {
  scope                = azurerm_container_registry.chatbot_acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.chatbot_ui_identity.principal_id
}

# Assign AcrPush role to the App Registration that I own
resource "azurerm_role_assignment" "acr_push" {
  scope                = azurerm_container_registry.chatbot_acr.id
  role_definition_name = "AcrPush"
  principal_id         = "ed6b86c0-bb50-4281-8486-1645164e95ee"
}

# App Service for UI using Docker image and managed identity
resource "azurerm_linux_web_app" "chatbot_ui" {
  name                = "ai-chatbot-ui-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  service_plan_id     = azurerm_service_plan.chatbot_plan.id

  site_config {
    always_on                                     = true
    container_registry_managed_identity_client_id = azurerm_user_assigned_identity.chatbot_ui_identity.client_id
    container_registry_use_managed_identity       = true
    application_stack {
      docker_image_name   = "ai-chatbot-ui:latest"
      docker_registry_url = "https://${azurerm_container_registry.chatbot_acr.login_server}"
    }
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.chatbot_ui_identity.id]
  }
}

# Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "chatbot_log_analytics" {
  name                = "chatbot-log-analytics-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# Application Insights
resource "azurerm_application_insights" "chatbot_insights" {
  name                = "ai-chatbot-insights-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  workspace_id        = azurerm_log_analytics_workspace.chatbot_log_analytics.id
  application_type    = "web"
  sampling_percentage = 25
}

# Cosmos DB Account
resource "azurerm_cosmosdb_account" "chatbot_cosmos" {
  name                = "chatbot-cosmos-${random_string.random.result}"
  location            = "Central US"
  resource_group_name = azurerm_resource_group.ai_rg.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = "Central US"
    failover_priority = 0
  }

  capabilities {
    name = "EnableServerless"
  }

  automatic_failover_enabled = true
}

# Cosmos DB SQL Database
resource "azurerm_cosmosdb_sql_database" "chatbot_db" {
  name                = "chatbotdb"
  resource_group_name = azurerm_resource_group.ai_rg.name
  account_name        = azurerm_cosmosdb_account.chatbot_cosmos.name
}

# Cosmos DB SQL Container
resource "azurerm_cosmosdb_sql_container" "chatbot_container" {
  name                = "chatbotcontainer"
  resource_group_name = azurerm_resource_group.ai_rg.name
  account_name        = azurerm_cosmosdb_account.chatbot_cosmos.name
  database_name       = azurerm_cosmosdb_sql_database.chatbot_db.name
  partition_key_paths = ["/UserId"]
}

# Assign Cosmos DB Contributor role to the API managed identity
resource "azurerm_role_assignment" "cosmos_db_contributor" {
  scope              = azurerm_cosmosdb_account.chatbot_cosmos.id
  role_definition_id = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c"
  principal_id       = azurerm_user_assigned_identity.chatbot_api_identity.principal_id
}

# Key Vault
resource "azurerm_key_vault" "chatbot_key_vault" {
  name                = "chatbot-keyvault-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    key_permissions         = ["Get", "List"]
    secret_permissions      = ["Get", "List", "Set", "Delete"]
    certificate_permissions = ["Get", "List"]
  }
}

# Store secrets in Key Vault
resource "azurerm_key_vault_secret" "openai_api_key" {
  name         = "AzureOpenAI--ApiKey"
  value        = azurerm_cognitive_account.openai.primary_access_key
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "openai_endpoint" {
  name         = "AzureOpenAI--Endpoint"
  value        = azurerm_cognitive_account.openai.endpoint
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "openai_deployment_name" {
  name         = "AzureOpenAI--DeploymentName"
  value        = azurerm_cognitive_deployment.openai.name
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "allowed_origins_0" {
  name         = "AllowedOrigins--0"
  value        = "https://${azurerm_linux_web_app.chatbot_ui.default_hostname}"
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "appinsights_connection_string" {
  name         = "ApplicationInsights--ConnectionString"
  value        = azurerm_application_insights.chatbot_insights.connection_string
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "cosmosdb_endpoint" {
  name         = "CosmosDb--Endpoint"
  value        = azurerm_cosmosdb_account.chatbot_cosmos.endpoint
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "cosmosdb_database_name" {
  name         = "CosmosDb--DatabaseName"
  value        = azurerm_cosmosdb_sql_database.chatbot_db.name
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "cosmosdb_container_name" {
  name         = "CosmosDb--ContainerName"
  value        = azurerm_cosmosdb_sql_container.chatbot_container.name
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}
