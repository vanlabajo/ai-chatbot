resource "azurerm_log_analytics_workspace" "chatbot_log_analytics" {
  name                = "chatbot-log-analytics-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "chatbot_insights" {
  name                = "ai-chatbot-insights-${random_string.random.result}"
  location            = azurerm_resource_group.ai_rg.location
  resource_group_name = azurerm_resource_group.ai_rg.name
  workspace_id        = azurerm_log_analytics_workspace.chatbot_log_analytics.id
  application_type    = "web"
  sampling_percentage = 25
}
