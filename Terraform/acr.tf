resource "azurerm_container_registry" "chatbot_acr" {
  name                = "chatbotacr${random_string.random.result}"
  resource_group_name = azurerm_resource_group.ai_rg.name
  location            = azurerm_resource_group.ai_rg.location
  sku                 = "Basic"
  admin_enabled       = false
}
