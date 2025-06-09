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

resource "azurerm_cosmosdb_sql_database" "chatbot_db" {
  name                = "chatbotdb"
  resource_group_name = azurerm_resource_group.ai_rg.name
  account_name        = azurerm_cosmosdb_account.chatbot_cosmos.name
}

resource "azurerm_cosmosdb_sql_container" "chatbot_container" {
  name                = "chatbotcontainer"
  resource_group_name = azurerm_resource_group.ai_rg.name
  account_name        = azurerm_cosmosdb_account.chatbot_cosmos.name
  database_name       = azurerm_cosmosdb_sql_database.chatbot_db.name
  partition_key_paths = ["/userId"]
}
