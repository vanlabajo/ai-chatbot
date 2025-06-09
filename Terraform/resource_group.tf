resource "random_string" "random" {
  length  = 6
  special = false
  upper   = false
  numeric = true
}

resource "azurerm_resource_group" "ai_rg" {
  name     = "rg-ai-chatbot-${random_string.random.result}"
  location = "Canada East"
}
