data "azuread_application" "my_app" {
  display_name = "TOPS-PoC"
}

data "azuread_service_principal" "my_sp" {
  client_id = data.azuread_application.my_app.client_id
}

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

resource "azurerm_role_assignment" "acr_pull" {
  scope                = azurerm_container_registry.chatbot_acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.chatbot_ui_identity.principal_id
}

resource "azurerm_role_assignment" "acr_push" {
  scope                = azurerm_container_registry.chatbot_acr.id
  role_definition_name = "AcrPush"
  principal_id         = data.azuread_service_principal.my_sp.object_id
}

resource "azurerm_role_assignment" "cosmos_db_contributor" {
  scope              = azurerm_cosmosdb_account.chatbot_cosmos.id
  role_definition_id = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c"
  principal_id       = azurerm_user_assigned_identity.chatbot_api_identity.principal_id
}

resource "time_rotating" "my_app_secret_rotation" {
  rotation_years = 1
}

resource "azuread_application_password" "my_app_secret" {
  application_id = data.azuread_application.my_app.id
  display_name   = "terraform-ai-chatbox-generated secret"
  rotate_when_changed = {
    rotation = time_rotating.my_app_secret_rotation.id
  }
}
