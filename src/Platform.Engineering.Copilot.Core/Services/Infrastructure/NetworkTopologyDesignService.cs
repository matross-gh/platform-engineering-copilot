using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Models;

namespace Platform.Engineering.Copilot.Core.Services.Infrastructure;

/// <summary>
/// Intelligent network topology design service with automatic CIDR calculation
/// Generates multi-tier network topologies for Azure VNets with proper subnet segmentation
/// </summary>
public interface INetworkTopologyDesignService
{
    /// <summary>
    /// Design a multi-tier network topology with automatic subnet calculation
    /// </summary>
    NetworkingConfiguration DesignMultiTierTopology(
        string addressSpace,
        int tierCount = 3,
        bool includeBastion = true,
        bool includeFirewall = true,
        bool includeGateway = true);

    /// <summary>
    /// Calculate subnet CIDRs from a given address space
    /// </summary>
    List<SubnetConfiguration> CalculateSubnetCIDRs(
        string addressSpace,
        int requiredSubnets,
        SubnetAllocationStrategy strategy = SubnetAllocationStrategy.EqualSize);
}

public class NetworkTopologyDesignService : INetworkTopologyDesignService
{
    private readonly ILogger<NetworkTopologyDesignService> _logger;

    // Standard Azure subnet names
    private const string GatewaySubnetName = "GatewaySubnet"; // Required name for VPN Gateway
    private const string AzureBastionSubnetName = "AzureBastionSubnet"; // Required name for Bastion
    private const string AzureFirewallSubnetName = "AzureFirewallSubnet"; // Required name for Firewall

    public NetworkTopologyDesignService(ILogger<NetworkTopologyDesignService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public NetworkingConfiguration DesignMultiTierTopology(
        string addressSpace,
        int tierCount = 3,
        bool includeBastion = true,
        bool includeFirewall = true,
        bool includeGateway = true)
    {
        if (string.IsNullOrWhiteSpace(addressSpace))
            throw new ArgumentException("Address space cannot be null or empty", nameof(addressSpace));
        
        if (tierCount < 1)
            throw new ArgumentException("Tier count must be at least 1", nameof(tierCount));

        _logger.LogInformation("Designing multi-tier network topology: AddressSpace={AddressSpace}, Tiers={Tiers}, Bastion={Bastion}, Firewall={Firewall}",
            addressSpace, tierCount, includeBastion, includeFirewall);

        // Calculate total required subnets
        int requiredSubnets = tierCount;
        if (includeGateway) requiredSubnets++;
        if (includeBastion) requiredSubnets++;
        if (includeFirewall) requiredSubnets++;
        requiredSubnets++; // Add one for private endpoints

        // Parse the address space to determine the network size
        var (baseAddress, prefixLength) = ParseCIDR(addressSpace);
        
        // Calculate subnet size based on total required subnets
        var subnetPrefixLength = CalculateSubnetPrefixLength(prefixLength, requiredSubnets);

        _logger.LogDebug("Network calculation: Base={Base}, Prefix={Prefix}, SubnetPrefix={SubnetPrefix}, TotalSubnets={Total}",
            baseAddress, prefixLength, subnetPrefixLength, requiredSubnets);

        var subnets = new List<SubnetConfiguration>();
        int subnetIndex = 0;

        // Reserve first subnet for Gateway (if needed)
        if (includeGateway)
        {
            subnets.Add(new SubnetConfiguration
            {
                Name = GatewaySubnetName,
                AddressPrefix = CalculateSubnetCIDR(baseAddress, subnetPrefixLength, subnetIndex++),
                Purpose = SubnetPurpose.ApplicationGateway,
                EnableServiceEndpoints = false
            });
        }

        // Reserve second subnet for Bastion (if needed)
        if (includeBastion)
        {
            subnets.Add(new SubnetConfiguration
            {
                Name = AzureBastionSubnetName,
                AddressPrefix = CalculateSubnetCIDR(baseAddress, subnetPrefixLength, subnetIndex++),
                Purpose = SubnetPurpose.Other,
                EnableServiceEndpoints = false
            });
        }

        // Reserve third subnet for Firewall (if needed)
        if (includeFirewall)
        {
            subnets.Add(new SubnetConfiguration
            {
                Name = AzureFirewallSubnetName,
                AddressPrefix = CalculateSubnetCIDR(baseAddress, subnetPrefixLength, subnetIndex++),
                Purpose = SubnetPurpose.Other,
                EnableServiceEndpoints = false
            });
        }

        // Create application tier subnets
        for (int i = 0; i < tierCount; i++)
        {
            var tierName = i switch
            {
                0 => "ApplicationTier",
                1 => "DataTier",
                2 => "GatewayTier",
                _ => $"Tier{i + 1}"
            };

            var purpose = i switch
            {
                0 => SubnetPurpose.Application,
                1 => SubnetPurpose.Database,
                2 => SubnetPurpose.ApplicationGateway,
                _ => SubnetPurpose.Other
            };

            subnets.Add(new SubnetConfiguration
            {
                Name = $"{tierName}Subnet",
                AddressPrefix = CalculateSubnetCIDR(baseAddress, subnetPrefixLength, subnetIndex++),
                Purpose = purpose,
                EnableServiceEndpoints = true,
                ServiceEndpoints = GetServiceEndpointsForTier(purpose)
            });
        }

        // Add Private Endpoints subnet
        subnets.Add(new SubnetConfiguration
        {
            Name = "PrivateEndpointsSubnet",
            AddressPrefix = CalculateSubnetCIDR(baseAddress, subnetPrefixLength, subnetIndex++),
            Purpose = SubnetPurpose.PrivateEndpoints,
            EnableServiceEndpoints = true,
            ServiceEndpoints = new List<string>
            {
                "Microsoft.Storage",
                "Microsoft.KeyVault",
                "Microsoft.Sql",
                "Microsoft.AzureCosmosDB"
            }
        });

        var config = new NetworkingConfiguration
        {
            Mode = NetworkMode.CreateNew,
            VNetName = "vnet-mission-primary",
            VNetAddressSpace = addressSpace,
            Subnets = subnets,
            EnableNetworkSecurityGroup = true,
            NsgMode = "new",
            EnableDDoSProtection = true,
            DdosMode = "new",
            EnablePrivateDns = true,
            PrivateDnsMode = "new",
            EnablePrivateEndpoint = true,
            PrivateEndpointSubnetName = "PrivateEndpointsSubnet",
            EnableServiceEndpoints = true,
            ServiceEndpoints = new List<string>
            {
                "Microsoft.Storage",
                "Microsoft.KeyVault",
                "Microsoft.Sql",
                "Microsoft.Web"
            }
        };

        _logger.LogInformation("Network topology designed: {SubnetCount} subnets created", subnets.Count);
        return config;
    }

    public List<SubnetConfiguration> CalculateSubnetCIDRs(
        string addressSpace,
        int requiredSubnets,
        SubnetAllocationStrategy strategy = SubnetAllocationStrategy.EqualSize)
    {
        if (string.IsNullOrWhiteSpace(addressSpace))
            throw new ArgumentException("Address space cannot be null or empty", nameof(addressSpace));
        
        if (requiredSubnets < 1)
            throw new ArgumentException("Required subnets must be at least 1", nameof(requiredSubnets));

        _logger.LogDebug("Calculating subnet CIDRs for {AddressSpace} with {Count} subnets using {Strategy} strategy",
            addressSpace, requiredSubnets, strategy);

        var (baseAddress, prefixLength) = ParseCIDR(addressSpace);
        
        // Check if subdivision is possible BEFORE capping
        int bitsNeeded = (int)Math.Ceiling(Math.Log(requiredSubnets, 2));
        int calculatedPrefixLength = prefixLength + bitsNeeded;
        
        if (calculatedPrefixLength > 30) // /30 is minimum viable subnet (4 IPs)
            throw new ArgumentException($"Cannot subdivide /{prefixLength} network into {requiredSubnets} viable subnets. Would require /{calculatedPrefixLength} subnets.", nameof(requiredSubnets));
        
        var subnetPrefixLength = CalculateSubnetPrefixLength(prefixLength, requiredSubnets);

        var subnets = new List<SubnetConfiguration>();

        for (int i = 0; i < requiredSubnets; i++)
        {
            subnets.Add(new SubnetConfiguration
            {
                Name = $"subnet-{i + 1:D2}",
                AddressPrefix = CalculateSubnetCIDR(baseAddress, subnetPrefixLength, i),
                Purpose = SubnetPurpose.Application
            });
        }

        return subnets;
    }

    #region Helper Methods

    private (IPAddress baseAddress, int prefixLength) ParseCIDR(string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid CIDR notation: {cidr}");
        }

        if (!IPAddress.TryParse(parts[0], out var baseAddress))
        {
            throw new ArgumentException($"Invalid IP address: {parts[0]}");
        }

        if (!int.TryParse(parts[1], out var prefixLength) || prefixLength < 0 || prefixLength > 32)
        {
            throw new ArgumentException($"Invalid prefix length: {parts[1]}");
        }

        return (baseAddress, prefixLength);
    }

    private int CalculateSubnetPrefixLength(int vnetPrefixLength, int requiredSubnets)
    {
        // Calculate how many bits we need for subnets
        // Example: 4 subnets needs 2 bits (2^2 = 4), 8 subnets needs 3 bits (2^3 = 8)
        int bitsNeeded = (int)Math.Ceiling(Math.Log(requiredSubnets, 2));
        
        // Subnet prefix = VNet prefix + bits needed
        int subnetPrefixLength = vnetPrefixLength + bitsNeeded;

        // Ensure subnet prefix doesn't exceed /29 (minimum 8 IPs per subnet)
        // Azure reserves 5 IPs per subnet, so /29 gives 3 usable IPs (too small)
        // Use /28 minimum (11 usable IPs) for practical purposes
        if (subnetPrefixLength > 28)
        {
            _logger.LogWarning("Calculated subnet prefix /{SubnetPrefix} exceeds /28, capping at /28", subnetPrefixLength);
            subnetPrefixLength = 28;
        }

        // Ensure we don't exceed /32
        if (subnetPrefixLength > 32)
        {
            throw new InvalidOperationException($"Cannot fit {requiredSubnets} subnets into /{vnetPrefixLength} address space");
        }

        return subnetPrefixLength;
    }

    private string CalculateSubnetCIDR(IPAddress baseAddress, int prefixLength, int subnetIndex)
    {
        // Convert IP to bytes
        var addressBytes = baseAddress.GetAddressBytes();
        var addressValue = BitConverter.ToUInt32(addressBytes.Reverse().ToArray(), 0);

        // Calculate the number of addresses per subnet
        int hostBits = 32 - prefixLength;
        uint addressesPerSubnet = (uint)(1 << hostBits);

        // Calculate the subnet base address
        uint subnetAddressValue = addressValue + (addressesPerSubnet * (uint)subnetIndex);

        // Convert back to IP address
        var subnetBytes = BitConverter.GetBytes(subnetAddressValue).Reverse().ToArray();
        var subnetAddress = new IPAddress(subnetBytes);

        return $"{subnetAddress}/{prefixLength}";
    }

    private List<string> GetServiceEndpointsForTier(SubnetPurpose purpose)
    {
        return purpose switch
        {
            SubnetPurpose.Application => new List<string>
            {
                "Microsoft.Storage",
                "Microsoft.KeyVault",
                "Microsoft.Sql",
                "Microsoft.Web"
            },
            SubnetPurpose.Database => new List<string>
            {
                "Microsoft.Sql",
                "Microsoft.Storage",
                "Microsoft.KeyVault",
                "Microsoft.AzureCosmosDB"
            },
            SubnetPurpose.PrivateEndpoints => new List<string>
            {
                "Microsoft.Storage",
                "Microsoft.KeyVault",
                "Microsoft.Sql",
                "Microsoft.AzureCosmosDB",
                "Microsoft.Web"
            },
            SubnetPurpose.ApplicationGateway => new List<string>
            {
                "Microsoft.Web"
            },
            _ => new List<string>()
        };
    }

    #endregion
}

/// <summary>
/// Strategy for allocating subnet sizes
/// </summary>
public enum SubnetAllocationStrategy
{
    /// <summary>
    /// All subnets get equal size
    /// </summary>
    EqualSize,

    /// <summary>
    /// Application tier gets larger subnets, data tier gets medium, others get smaller
    /// </summary>
    Tiered,

    /// <summary>
    /// Custom allocation based on requirements
    /// </summary>
    Custom
}
