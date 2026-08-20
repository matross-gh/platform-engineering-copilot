namespace Platform.Engineering.Copilot.Core.Models;

/// <summary>
/// Purpose of a subnet in the architecture
/// </summary>
public enum SubnetPurpose
{
    // Basic/Legacy purposes
    Application,        // Main application subnet (App Service, AKS nodes, Container Instances)
    PrivateEndpoints,   // For private endpoints
    ApplicationGateway, // For Application Gateway (AKS ingress)
    Database,           // For database resources
    Other,              // Custom purpose

    // 3-Tier Architecture purposes
    WebTier,            // Web/presentation tier - public-facing
    ApplicationTier,    // Application/business logic tier
    DataTier,           // Data/persistence tier

    // AKS-specific purposes
    AksSystemNodePool,  // AKS system node pool
    AksUserNodePool,    // AKS user/workload node pool
    AksIngress,         // AKS ingress controller subnet

    // Landing Zone purposes
    Management,         // Management subnet for bastion, jump boxes
    SharedServices,     // Shared services - DNS, AD, monitoring
    Workload,           // Primary workload subnet

    // Security/Infrastructure purposes
    Firewall,           // Azure Firewall subnet
    Bastion,            // Azure Bastion subnet
    Gateway             // VPN/ExpressRoute Gateway subnet
}

/// <summary>
/// Subnet configuration for VNet
/// </summary>
public class SubnetConfiguration
{
    public string Name { get; set; } = "";
    public string AddressPrefix { get; set; } = "";
    public string? Delegation { get; set; } // e.g., "Microsoft.Web/serverFarms"
    public bool EnableServiceEndpoints { get; set; } = false;
    public List<string> ServiceEndpoints { get; set; } = new();
    public SubnetPurpose Purpose { get; set; } = SubnetPurpose.Application;  // Purpose of this subnet
}

/// <summary>
/// Network Security Group rule
/// </summary>
public class NetworkSecurityRule
{
    public string Name { get; set; } = "";
    public int Priority { get; set; } = 100;
    public string Direction { get; set; } = "Inbound"; // Inbound or Outbound
    public string Access { get; set; } = "Allow"; // Allow or Deny
    public string Protocol { get; set; } = "Tcp"; // Tcp, Udp, Icmp, Esp, Ah, or *
    public string SourcePortRange { get; set; } = "*";
    public string DestinationPortRange { get; set; } = "*";
    public string SourceAddressPrefix { get; set; } = "*";
    public string DestinationAddressPrefix { get; set; } = "*";
    public string Description { get; set; } = "";
}
