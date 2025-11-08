# Outputs for Platform Engineering Copilot Infrastructure

output "admin_api_url" {
  description = "Admin API App Service URL"
  value       = "https://${module.appservice.api_app_service_hostname}"
}

output "mcp_url" {
  description = "MCP Server URL"
  value       = "https://${module.appservice.mcp_app_service_hostname}"
}

output "sql_server_fqdn" {
  description = "SQL Server FQDN"
  value       = module.sql.sql_server_fqdn
}

output "key_vault_uri" {
  description = "Key Vault URI"
  value       = module.keyvault.key_vault_uri
}

output "application_insights_instrumentation_key" {
  description = "Application Insights Instrumentation Key"
  value       = module.monitoring.application_insights_instrumentation_key
  sensitive   = true
}

output "storage_account_name" {
  description = "Storage Account Name"
  value       = module.storage.storage_account_name
}

output "resource_group_name" {
  description = "Resource Group Name"
  value       = var.resource_group_name
}

output "virtual_network_name" {
  description = "Virtual Network Name"
  value       = module.network.vnet_name
}

output "deployment_summary" {
  description = "Deployment Summary"
  value = {
    project_name         = var.project_name
    environment          = var.environment
    location             = var.location
    admin_api_app_service = module.appservice.api_app_service_name
    mcp_app_service      = module.appservice.mcp_app_service_name
    sql_server           = module.sql.sql_server_name
    sql_database         = module.sql.sql_database_name
    key_vault            = module.keyvault.key_vault_name
    storage_account      = module.storage.storage_account_name
    application_insights = module.monitoring.application_insights_name
    virtual_network      = module.network.vnet_name
    deployed_at          = timestamp()
  }
}