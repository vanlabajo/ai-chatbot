resource "azurerm_service_plan" "chatbot_plan" {
  name                = "chatbot-appservice-plan-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  os_type             = "Linux"
  sku_name            = "B1"
}

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

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.chatbot_api_identity.id]
  }

  key_vault_reference_identity_id = azurerm_user_assigned_identity.chatbot_api_identity.id

  app_settings = {
    "AzureAd__TenantId"                     = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.azuread_tenant_id.id})"
    "AzureAd__ClientId"                     = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.azuread_client_id.id})"
    "AzureAd__ClientSecret"                 = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.my_app_secret.id})"
    "AzureAd__Audiences__0"                 = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.azuread_audiences_0.id})"
    "AzureAd__Audiences__1"                 = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.azuread_audiences_1.id})"
    "SwaggerUI__ClientId"                   = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.swaggerui_client_id.id})"
    "SwaggerUI__Scope"                      = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.swaggerui_scope.id})"
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
