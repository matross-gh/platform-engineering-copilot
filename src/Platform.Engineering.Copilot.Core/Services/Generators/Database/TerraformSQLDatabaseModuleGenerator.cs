using System.Text;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models;

namespace Platform.Engineering.Copilot.Core.Services.Generators.Database;

/// <summary>
/// Terraform module generator for Azure SQL Database infrastructure
/// </summary>
public class TerraformSQLDatabaseModuleGenerator : IInfrastructureModuleGenerator
{
    public InfrastructureFormat Format => InfrastructureFormat.Terraform;
    public ComputePlatform Platform => ComputePlatform.Database;
    public CloudProvider Provider => CloudProvider.Azure;

    public Dictionary<string, string> GenerateModule(TemplateGenerationRequest request)
    {
        var files = new Dictionary<string, string>();
        var serviceName = request.ServiceName ?? "sqldb";
        var infrastructure = request.Infrastructure ?? new InfrastructureSpec();
        var security = request.Security ?? new SecuritySpec();
        var observability = request.Observability ?? new ObservabilitySpec();

        // Generate main SQL module
        files["modules/database/main.tf"] = GenerateMainTf(request);
        files["modules/database/variables.tf"] = GenerateVariablesTf(request);
        files["modules/database/outputs.tf"] = GenerateOutputsTf(request);
        files["modules/database/README.md"] = GenerateReadme(request);

        return files;
    }

    private string GenerateMainTf(TemplateGenerationRequest request)
    {
        var sb = new StringBuilder();
        var serviceName = request.ServiceName ?? "sqldb";
        var infrastructure = request.Infrastructure ?? new InfrastructureSpec();
        var security = request.Security ?? new SecuritySpec();
        var observability = request.Observability ?? new ObservabilitySpec();

        sb.AppendLine("# Azure SQL Database Infrastructure Module");
        sb.AppendLine($"# Service: {serviceName}");
        sb.AppendLine($"# Region: {infrastructure.Region}");
        sb.AppendLine();

        // SQL Server
        sb.AppendLine("resource \"azurerm_mssql_server\" \"sql\" {");
        sb.AppendLine("  name                         = var.sql_server_name");
        sb.AppendLine("  resource_group_name          = var.resource_group_name");
        sb.AppendLine("  location                     = var.location");
        sb.AppendLine("  version                      = \"12.0\"");
        sb.AppendLine("  administrator_login          = var.administrator_login");
        sb.AppendLine("  administrator_login_password = var.administrator_password");
        sb.AppendLine($"  minimum_tls_version          = \"{security.TLSVersion ?? "1.2"}\"");
        sb.AppendLine($"  public_network_access_enabled = {(security.EnablePrivateEndpoint != true ? "true" : "false")}");
        sb.AppendLine();
        sb.AppendLine("  tags = var.tags");
        sb.AppendLine("}");
        sb.AppendLine();

        // SQL Database
        sb.AppendLine("resource \"azurerm_mssql_database\" \"db\" {");
        sb.AppendLine("  name           = var.sql_database_name");
        sb.AppendLine("  server_id      = azurerm_mssql_server.sql.id");
        sb.AppendLine("  collation      = \"SQL_Latin1_General_CP1_CI_AS\"");
        sb.AppendLine("  max_size_gb    = var.max_size_gb");
        sb.AppendLine("  sku_name       = var.sku_name");
        sb.AppendLine("  zone_redundant = false");
        sb.AppendLine();
        sb.AppendLine("  tags = var.tags");
        sb.AppendLine("}");
        sb.AppendLine();

        // Firewall Rules (if enabled and no private endpoint)
        if (security.EnableFirewall && security.EnablePrivateEndpoint != true)
        {
            sb.AppendLine("# Allow Azure services to access SQL Server");
            sb.AppendLine("resource \"azurerm_mssql_firewall_rule\" \"azure_services\" {");
            sb.AppendLine("  name             = \"AllowAzureServices\"");
            sb.AppendLine("  server_id        = azurerm_mssql_server.sql.id");
            sb.AppendLine("  start_ip_address = \"0.0.0.0\"");
            sb.AppendLine("  end_ip_address   = \"0.0.0.0\"");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Private Endpoint (if enabled)
        if (security.EnablePrivateEndpoint)
        {
            sb.AppendLine("# Private Endpoint for SQL Server");
            sb.AppendLine("resource \"azurerm_private_endpoint\" \"sql\" {");
            sb.AppendLine("  name                = \"${var.sql_server_name}-pe\"");
            sb.AppendLine("  location            = var.location");
            sb.AppendLine("  resource_group_name = var.resource_group_name");
            sb.AppendLine("  subnet_id           = var.private_endpoint_subnet_id");
            sb.AppendLine();
            sb.AppendLine("  private_service_connection {");
            sb.AppendLine("    name                           = \"${var.sql_server_name}-psc\"");
            sb.AppendLine("    private_connection_resource_id = azurerm_mssql_server.sql.id");
            sb.AppendLine("    is_manual_connection           = false");
            sb.AppendLine("    subresource_names              = [\"sqlServer\"]");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  tags = var.tags");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Diagnostics (if enabled)
        if (observability.EnableDiagnostics == true)
        {
            sb.AppendLine("# Diagnostic Settings for SQL Database");
            sb.AppendLine("resource \"azurerm_monitor_diagnostic_setting\" \"db\" {");
            sb.AppendLine("  name                       = \"${var.sql_database_name}-diag\"");
            sb.AppendLine("  target_resource_id         = azurerm_mssql_database.db.id");
            sb.AppendLine("  log_analytics_workspace_id = var.log_analytics_workspace_id");
            sb.AppendLine();
            sb.AppendLine("  log {");
            sb.AppendLine("    category = \"SQLInsights\"");
            sb.AppendLine("    enabled  = true");
            sb.AppendLine();
            sb.AppendLine("    retention_policy {");
            sb.AppendLine("      enabled = true");
            sb.AppendLine("      days    = 30");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  log {");
            sb.AppendLine("    category = \"QueryStoreRuntimeStatistics\"");
            sb.AppendLine("    enabled  = true");
            sb.AppendLine();
            sb.AppendLine("    retention_policy {");
            sb.AppendLine("      enabled = true");
            sb.AppendLine("      days    = 30");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  metric {");
            sb.AppendLine("    category = \"Basic\"");
            sb.AppendLine("    enabled  = true");
            sb.AppendLine();
            sb.AppendLine("    retention_policy {");
            sb.AppendLine("      enabled = true");
            sb.AppendLine("      days    = 30");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateVariablesTf(TemplateGenerationRequest request)
    {
        var sb = new StringBuilder();
        var security = request.Security ?? new SecuritySpec();
        var observability = request.Observability ?? new ObservabilitySpec();

        sb.AppendLine("# SQL Database Variables");
        sb.AppendLine();
        sb.AppendLine("variable \"sql_server_name\" {");
        sb.AppendLine("  description = \"Name of the SQL Server\"");
        sb.AppendLine("  type        = string");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("variable \"sql_database_name\" {");
        sb.AppendLine("  description = \"Name of the SQL Database\"");
        sb.AppendLine("  type        = string");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("variable \"resource_group_name\" {");
        sb.AppendLine("  description = \"Name of the resource group\"");
        sb.AppendLine("  type        = string");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("variable \"location\" {");
        sb.AppendLine("  description = \"Azure region\"");
        sb.AppendLine("  type        = string");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("variable \"administrator_login\" {");
        sb.AppendLine("  description = \"SQL Server administrator login\"");
        sb.AppendLine("  type        = string");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("variable \"administrator_password\" {");
        sb.AppendLine("  description = \"SQL Server administrator password\"");
        sb.AppendLine("  type        = string");
        sb.AppendLine("  sensitive   = true");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("variable \"sku_name\" {");
        sb.AppendLine("  description = \"SQL Database SKU\"");
        sb.AppendLine("  type        = string");
        sb.AppendLine("  default     = \"S0\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("variable \"max_size_gb\" {");
        sb.AppendLine("  description = \"Maximum size in GB\"");
        sb.AppendLine("  type        = number");
        sb.AppendLine("  default     = 2");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("variable \"tags\" {");
        sb.AppendLine("  description = \"Resource tags\"");
        sb.AppendLine("  type        = map(string)");
        sb.AppendLine("  default     = {}");
        sb.AppendLine("}");
        sb.AppendLine();

        if (security.EnablePrivateEndpoint)
        {
            sb.AppendLine("variable \"private_endpoint_subnet_id\" {");
            sb.AppendLine("  description = \"Subnet ID for private endpoint\"");
            sb.AppendLine("  type        = string");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        if (observability.EnableDiagnostics == true)
        {
            sb.AppendLine("variable \"log_analytics_workspace_id\" {");
            sb.AppendLine("  description = \"Log Analytics Workspace ID\"");
            sb.AppendLine("  type        = string");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateOutputsTf(TemplateGenerationRequest request)
    {
        var sb = new StringBuilder();
        var security = request.Security ?? new SecuritySpec();

        sb.AppendLine("# SQL Database Outputs");
        sb.AppendLine();
        sb.AppendLine("output \"sql_server_id\" {");
        sb.AppendLine("  description = \"SQL Server ID\"");
        sb.AppendLine("  value       = azurerm_mssql_server.sql.id");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("output \"sql_server_name\" {");
        sb.AppendLine("  description = \"SQL Server name\"");
        sb.AppendLine("  value       = azurerm_mssql_server.sql.name");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("output \"sql_server_fqdn\" {");
        sb.AppendLine("  description = \"SQL Server FQDN\"");
        sb.AppendLine("  value       = azurerm_mssql_server.sql.fully_qualified_domain_name");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("output \"sql_database_id\" {");
        sb.AppendLine("  description = \"SQL Database ID\"");
        sb.AppendLine("  value       = azurerm_mssql_database.db.id");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("output \"sql_database_name\" {");
        sb.AppendLine("  description = \"SQL Database name\"");
        sb.AppendLine("  value       = azurerm_mssql_database.db.name");
        sb.AppendLine("}");
        sb.AppendLine();

        if (security.EnablePrivateEndpoint)
        {
            sb.AppendLine("output \"private_endpoint_id\" {");
            sb.AppendLine("  description = \"Private endpoint ID\"");
            sb.AppendLine("  value       = azurerm_private_endpoint.sql.id");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateReadme(TemplateGenerationRequest request)
    {
        var sb = new StringBuilder();
        var serviceName = request.ServiceName ?? "sqldb";

        sb.AppendLine($"# Azure SQL Database - {serviceName}");
        sb.AppendLine();
        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine("Terraform module for Azure SQL Database with:");
        sb.AppendLine("- Azure SQL Server");
        sb.AppendLine("- Azure SQL Database with configurable SKU");
        sb.AppendLine("- TLS encryption");
        
        if (request.Security?.EnablePrivateEndpoint == true)
        {
            sb.AppendLine("- Private endpoint connectivity");
        }
        
        if (request.Security?.EnableFirewall == true && request.Security?.EnablePrivateEndpoint != true)
        {
            sb.AppendLine("- Firewall rules for Azure services");
        }
        
        if (request.Observability?.EnableDiagnostics == true)
        {
            sb.AppendLine("- Diagnostic settings and logging");
        }

        sb.AppendLine();
        sb.AppendLine("## Usage");
        sb.AppendLine();
        sb.AppendLine("```hcl");
        sb.AppendLine("module \"database\" {");
        sb.AppendLine("  source = \"./modules/database\"");
        sb.AppendLine();
        sb.AppendLine($"  sql_server_name      = \"{serviceName}-server\"");
        sb.AppendLine($"  sql_database_name    = \"{serviceName}-db\"");
        sb.AppendLine("  resource_group_name  = azurerm_resource_group.main.name");
        sb.AppendLine("  location             = azurerm_resource_group.main.location");
        sb.AppendLine("  administrator_login  = var.sql_admin_username");
        sb.AppendLine("  administrator_password = var.sql_admin_password");
        sb.AppendLine("  tags                 = local.common_tags");
        sb.AppendLine("}");
        sb.AppendLine("```");

        return sb.ToString();
    }

    public bool CanHandle(TemplateGenerationRequest request)
    {
        var infrastructure = request.Infrastructure;
        if (infrastructure == null) return false;

        return infrastructure.ComputePlatform == ComputePlatform.Database &&
               infrastructure.Format == InfrastructureFormat.Terraform &&
               infrastructure.Provider == CloudProvider.Azure;
    }
}
