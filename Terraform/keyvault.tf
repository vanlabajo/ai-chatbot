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

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = azurerm_user_assigned_identity.chatbot_api_identity.principal_id

    secret_permissions = ["Get", "List"]
  }
}

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

resource "azurerm_key_vault_secret" "my_app_secret" {
  name         = "MyApp--ClientSecret"
  value        = azuread_application_password.my_app_secret.value
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "azuread_tenant_id" {
  name         = "AzureAd--TenantId"
  value        = data.azurerm_client_config.current.tenant_id
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "azuread_client_id" {
  name         = "AzureAd--ClientId"
  value        = data.azuread_application.my_app.client_id
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "azuread_audiences_0" {
  name         = "AzureAd--Audiences--0"
  value        = data.azuread_application.my_app.identifier_uris[0]
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "azuread_audiences_1" {
  name         = "AzureAd--Audiences--1"
  value        = data.azuread_application.my_app.client_id
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "swaggerui_client_id" {
  name         = "SwaggerUI--ClientId"
  value        = data.azuread_application.my_app.client_id
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}

resource "azurerm_key_vault_secret" "swaggerui_scope" {
  name         = "SwaggerUI--Scope"
  value        = "${data.azuread_application.my_app.identifier_uris[0]}/ai-chatbot-api-access"
  key_vault_id = azurerm_key_vault.chatbot_key_vault.id
}
