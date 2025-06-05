resource "azurerm_cognitive_account" "openai" {
  name                = "openai-chatbot-${random_string.random.result}"
  location            = "East US 2"
  resource_group_name = azurerm_resource_group.ai_rg.name
  kind                = "OpenAI"
  sku_name            = "S0"
}

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
