using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Infrastructure.Configuration;
using Platform.Engineering.Copilot.Agents.Infrastructure.State;

namespace Platform.Engineering.Copilot.Agents.Infrastructure.Tools;

/// <summary>
/// Tool for generating Azure Arc onboarding scripts for hybrid and multi-cloud scenarios.
/// </summary>
public class AzureArcTool : BaseTool
{
    private readonly InfrastructureStateAccessors _stateAccessors;
    private readonly InfrastructureAgentOptions _options;

    public override string Name => "generate_arc_onboarding_script";

    public override string Description =>
        "Generates Azure Arc onboarding scripts for connecting on-premises or multi-cloud servers " +
        "to Azure. Supports Windows and Linux with service principal or interactive authentication.";

    public AzureArcTool(
        ILogger<AzureArcTool> logger,
        InfrastructureStateAccessors stateAccessors,
        IOptions<InfrastructureAgentOptions> options) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _options = options?.Value ?? new InfrastructureAgentOptions();

        Parameters.Add(new ToolParameter("os_type", "Operating system: 'windows' or 'linux'", true));
        Parameters.Add(new ToolParameter("resource_group_name", "Resource group for Arc servers", true));
        Parameters.Add(new ToolParameter("subscription_id", "Azure subscription ID", false));
        Parameters.Add(new ToolParameter("location", "Azure region. Default: eastus", false));
        Parameters.Add(new ToolParameter("service_principal_id", "SP client ID for automated onboarding", false));
        Parameters.Add(new ToolParameter("service_principal_secret", "SP client secret", false));
        Parameters.Add(new ToolParameter("tenant_id", "Azure AD tenant ID (required with SP)", false));
        Parameters.Add(new ToolParameter("proxy_url", "Proxy server URL if needed", false));
        Parameters.Add(new ToolParameter("conversation_id", "Conversation ID for state tracking", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.EnableAzureArc)
                return ToJson(new { success = false, error = "Azure Arc is disabled" });

            var osType = GetOptionalString(arguments, "os_type")?.ToLowerInvariant()
                ?? throw new ArgumentException("os_type is required");
            var resourceGroupName = GetOptionalString(arguments, "resource_group_name")
                ?? throw new ArgumentException("resource_group_name is required");

            var conversationId = GetOptionalString(arguments, "conversation_id") ?? Guid.NewGuid().ToString();
            var location = GetOptionalString(arguments, "location") ?? _options.DefaultRegion;
            var servicePrincipalId = GetOptionalString(arguments, "service_principal_id");
            var servicePrincipalSecret = GetOptionalString(arguments, "service_principal_secret");
            var tenantId = GetOptionalString(arguments, "tenant_id");
            var proxyUrl = GetOptionalString(arguments, "proxy_url");

            var subscriptionId = GetOptionalString(arguments, "subscription_id")
                ?? await _stateAccessors.GetCurrentSubscriptionAsync(conversationId, cancellationToken)
                ?? _options.DefaultSubscriptionId;

            if (string.IsNullOrEmpty(subscriptionId))
                return ToJson(new { success = false, error = "Subscription ID is required" });

            var useServicePrincipal = !string.IsNullOrEmpty(servicePrincipalId);
            if (useServicePrincipal && (string.IsNullOrEmpty(servicePrincipalSecret) || string.IsNullOrEmpty(tenantId)))
                return ToJson(new { success = false, error = "SP secret and tenant ID required for SP auth" });

            Logger.LogInformation("Generating Azure Arc script for {OsType}", osType);

            var script = osType switch
            {
                "windows" => GenerateWindowsScript(subscriptionId, resourceGroupName, location, tenantId,
                    servicePrincipalId, servicePrincipalSecret, proxyUrl),
                "linux" => GenerateLinuxScript(subscriptionId, resourceGroupName, location, tenantId,
                    servicePrincipalId, servicePrincipalSecret, proxyUrl),
                _ => throw new ArgumentException($"Unsupported OS: {osType}")
            };

            var fileName = osType == "windows" ? "OnboardToArc.ps1" : "onboard_to_arc.sh";

            return ToJson(new
            {
                success = true,
                script = new
                {
                    osType, fileName, content = script,
                    authType = useServicePrincipal ? "service_principal" : "interactive",
                    resourceGroup = resourceGroupName, subscriptionId, location
                },
                instructions = GetInstructions(osType, useServicePrincipal),
                message = $"Generated Azure Arc onboarding script for {osType}"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating Azure Arc script");
            return ToJson(new { success = false, error = ex.Message });
        }
    }

    private string GenerateWindowsScript(string subscriptionId, string resourceGroupName, string location,
        string? tenantId, string? spId, string? spSecret, string? proxyUrl)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("#Requires -RunAsAdministrator");
        sb.AppendLine("# Azure Arc Onboarding Script for Windows");
        sb.AppendLine("# Generated by Platform Engineering Copilot\n");
        sb.AppendLine("$ErrorActionPreference = 'Stop'\n");
        sb.AppendLine($"$subscriptionId = '{subscriptionId}'");
        sb.AppendLine($"$resourceGroupName = '{resourceGroupName}'");
        sb.AppendLine($"$location = '{location}'");
        if (!string.IsNullOrEmpty(tenantId)) sb.AppendLine($"$tenantId = '{tenantId}'");
        if (!string.IsNullOrEmpty(spId))
        {
            sb.AppendLine($"$servicePrincipalId = '{spId}'");
            sb.AppendLine($"$servicePrincipalSecret = '{spSecret}'");
        }
        if (!string.IsNullOrEmpty(proxyUrl)) sb.AppendLine($"\n$proxyUrl = '{proxyUrl}'");
        
        sb.AppendLine("\ntry {");
        sb.AppendLine("    Write-Host 'Downloading Azure Connected Machine agent...'");
        sb.AppendLine("    Invoke-WebRequest -Uri 'https://aka.ms/AzureConnectedMachineAgent' -OutFile \"$env:TEMP\\AzureConnectedMachineAgent.msi\"");
        sb.AppendLine("    Write-Host 'Installing agent...'");
        sb.AppendLine("    Start-Process msiexec.exe -ArgumentList @('/i', \"$env:TEMP\\AzureConnectedMachineAgent.msi\", '/qn') -Wait");
        sb.AppendLine("    Write-Host 'Connecting to Azure Arc...'");
        
        if (!string.IsNullOrEmpty(spId))
            sb.AppendLine("    & azcmagent connect --subscription-id $subscriptionId --resource-group $resourceGroupName --location $location --tenant-id $tenantId --service-principal-id $servicePrincipalId --service-principal-secret $servicePrincipalSecret");
        else
            sb.AppendLine("    & azcmagent connect --subscription-id $subscriptionId --resource-group $resourceGroupName --location $location");
        
        sb.AppendLine("    Write-Host 'Successfully connected to Azure Arc!' -ForegroundColor Green");
        sb.AppendLine("} catch { Write-Host \"Error: $_\" -ForegroundColor Red; exit 1 }");
        return sb.ToString();
    }

    private string GenerateLinuxScript(string subscriptionId, string resourceGroupName, string location,
        string? tenantId, string? spId, string? spSecret, string? proxyUrl)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("#!/bin/bash");
        sb.AppendLine("# Azure Arc Onboarding Script for Linux");
        sb.AppendLine("# Generated by Platform Engineering Copilot\n");
        sb.AppendLine("set -e\n");
        sb.AppendLine($"SUBSCRIPTION_ID='{subscriptionId}'");
        sb.AppendLine($"RESOURCE_GROUP='{resourceGroupName}'");
        sb.AppendLine($"LOCATION='{location}'");
        if (!string.IsNullOrEmpty(tenantId)) sb.AppendLine($"TENANT_ID='{tenantId}'");
        if (!string.IsNullOrEmpty(spId))
        {
            sb.AppendLine($"SERVICE_PRINCIPAL_ID='{spId}'");
            sb.AppendLine($"SERVICE_PRINCIPAL_SECRET='{spSecret}'");
        }
        if (!string.IsNullOrEmpty(proxyUrl))
        {
            sb.AppendLine($"\nexport http_proxy='{proxyUrl}'");
            sb.AppendLine($"export https_proxy='{proxyUrl}'");
        }

        sb.AppendLine("\n[[ $EUID -ne 0 ]] && { echo 'Run as root'; exit 1; }");
        sb.AppendLine("\necho 'Downloading Azure Connected Machine agent...'");
        sb.AppendLine("curl -fsSL -o install_linux_azcmagent.sh https://aka.ms/azcmagent");
        sb.AppendLine("chmod +x install_linux_azcmagent.sh && bash install_linux_azcmagent.sh\n");
        sb.AppendLine("echo 'Connecting to Azure Arc...'");
        
        if (!string.IsNullOrEmpty(spId))
            sb.AppendLine("azcmagent connect --subscription-id \"$SUBSCRIPTION_ID\" --resource-group \"$RESOURCE_GROUP\" --location \"$LOCATION\" --tenant-id \"$TENANT_ID\" --service-principal-id \"$SERVICE_PRINCIPAL_ID\" --service-principal-secret \"$SERVICE_PRINCIPAL_SECRET\"");
        else
            sb.AppendLine("azcmagent connect --subscription-id \"$SUBSCRIPTION_ID\" --resource-group \"$RESOURCE_GROUP\" --location \"$LOCATION\"");

        sb.AppendLine("\necho 'Successfully connected to Azure Arc!'");
        return sb.ToString();
    }

    private List<string> GetInstructions(string osType, bool useServicePrincipal)
    {
        var instructions = new List<string>();
        if (osType == "windows")
        {
            instructions.Add("Save as 'OnboardToArc.ps1' on target server");
            instructions.Add("Run PowerShell as Administrator");
            instructions.Add("Execute: .\\OnboardToArc.ps1");
        }
        else
        {
            instructions.Add("Save as 'onboard_to_arc.sh' on target server");
            instructions.Add("Make executable: chmod +x onboard_to_arc.sh");
            instructions.Add("Run as root: sudo ./onboard_to_arc.sh");
        }
        if (!useServicePrincipal)
            instructions.Add("Sign in when prompted with Azure credentials");
        instructions.Add("Verify in Azure Portal under 'Azure Arc > Servers'");
        return instructions;
    }
}
