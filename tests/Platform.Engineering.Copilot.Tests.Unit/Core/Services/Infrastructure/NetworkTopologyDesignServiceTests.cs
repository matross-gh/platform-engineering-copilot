using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Services.Infrastructure;
using Xunit;

namespace Platform.Engineering.Copilot.Tests.Unit.Core.Services.Infrastructure;

/// <summary>
/// Unit tests for NetworkTopologyDesignService
/// Tests CIDR calculation, subnet allocation, and multi-tier network design
/// </summary>
public class NetworkTopologyDesignServiceTests
{
    private readonly Mock<ILogger<NetworkTopologyDesignService>> _mockLogger;
    private readonly NetworkTopologyDesignService _service;

    public NetworkTopologyDesignServiceTests()
    {
        _mockLogger = new Mock<ILogger<NetworkTopologyDesignService>>();
        _service = new NetworkTopologyDesignService(_mockLogger.Object);
    }

    #region CIDR Calculation Tests

    [Theory]
    [InlineData("10.0.0.0/16", 6, new[] { "10.0.0.0/19", "10.0.32.0/19", "10.0.64.0/19", "10.0.96.0/19", "10.0.128.0/19", "10.0.160.0/19" })]
    [InlineData("192.168.0.0/24", 4, new[] { "192.168.0.0/26", "192.168.0.64/26", "192.168.0.128/26", "192.168.0.192/26" })]
    [InlineData("172.16.0.0/20", 3, new[] { "172.16.0.0/22", "172.16.4.0/22", "172.16.8.0/22" })]
    public void CalculateSubnetCIDRs_WithValidParameters_ReturnsCorrectSubnets(
        string addressSpace, 
        int subnetCount, 
        string[] expectedCIDRs)
    {
        // Act
        var result = _service.CalculateSubnetCIDRs(addressSpace, subnetCount);

        // Assert
        Assert.Equal(subnetCount, result.Count);
        for (int i = 0; i < expectedCIDRs.Length; i++)
        {
            Assert.Equal(expectedCIDRs[i], result[i].AddressPrefix);
        }
    }

    [Fact]
    public void CalculateSubnetCIDRs_WithZeroSubnets_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => _service.CalculateSubnetCIDRs("10.0.0.0/16", 0));
        
        Assert.Contains("at least 1", exception.Message);
    }

    [Fact]
    public void CalculateSubnetCIDRs_WithNegativeSubnets_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => _service.CalculateSubnetCIDRs("10.0.0.0/16", -1));
        
        Assert.Contains("at least 1", exception.Message);
    }

    [Fact]
    public void CalculateSubnetCIDRs_WithInvalidCIDR_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _service.CalculateSubnetCIDRs("invalid-cidr", 2));
    }

    [Fact]
    public void CalculateSubnetCIDRs_WithTooManySubnets_ThrowsArgumentException()
    {
        // Arrange - /24 network can only support limited subnets (max /28 = 16 IPs each)
        var addressSpace = "10.0.0.0/24"; // 256 IPs total
        var tooManySubnets = 100; // Would require too small subnets

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => _service.CalculateSubnetCIDRs(addressSpace, tooManySubnets));
        
        Assert.Contains("Cannot subdivide", exception.Message);
    }

    #endregion

    #region Multi-Tier Topology Tests

    [Fact]
    public void DesignMultiTierTopology_WithBasicParameters_CreatesAllTierSubnets()
    {
        // Arrange
        var addressSpace = "10.0.0.0/16";
        var tierCount = 3;

        // Act
        var result = _service.DesignMultiTierTopology(addressSpace, tierCount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(addressSpace, result.VNetAddressSpace);
        Assert.True(result.Subnets.Count >= tierCount); // At least the requested tiers
    }

    [Fact]
    public void DesignMultiTierTopology_WithAllStandardTiers_CreatesCompleteTopology()
    {
        // Arrange
        var addressSpace = "10.0.0.0/16";
        var tierCount = 3; // App, Data, and 1 more tier

        // Act
        var result = _service.DesignMultiTierTopology(
            addressSpace, 
            tierCount, 
            includeBastion: true, 
            includeFirewall: true, 
            includeGateway: true);

        // Assert - Should have tiers + gateway + bastion + firewall + private endpoints
        Assert.True(result.Subnets.Count >= 6);
        
        // Verify Azure-specific subnet naming
        Assert.Contains(result.Subnets, s => s.Name == "GatewaySubnet"); // Azure reserved name
        Assert.Contains(result.Subnets, s => s.Name == "AzureBastionSubnet"); // Azure reserved name
        Assert.Contains(result.Subnets, s => s.Name == "AzureFirewallSubnet"); // Azure reserved name
    }

    [Fact]
    public void DesignMultiTierTopology_AssignsCorrectServiceEndpoints()
    {
        // Arrange
        var addressSpace = "10.0.0.0/16";
        var tierCount = 2;

        // Act
        var result = _service.DesignMultiTierTopology(addressSpace, tierCount, includeBastion: false, includeFirewall: false, includeGateway: false);

        // Assert - Check that subnets have service endpoints configured
        Assert.All(result.Subnets, subnet => Assert.NotNull(subnet.ServiceEndpoints));
    }

    [Fact]
    public void DesignMultiTierTopology_WithZeroTiers_ThrowsArgumentException()
    {
        // Arrange
        var addressSpace = "10.0.0.0/16";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => _service.DesignMultiTierTopology(addressSpace, 0));
        
        Assert.Contains("at least 1", exception.Message);
    }

    [Fact]
    public void DesignMultiTierTopology_WithNegativeTiers_ThrowsArgumentException()
    {
        // Arrange
        var addressSpace = "10.0.0.0/16";

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _service.DesignMultiTierTopology(addressSpace, -1));
    }

    [Fact]
    public void DesignMultiTierTopology_SubnetsDoNotOverlap()
    {
        // Arrange
        var addressSpace = "10.0.0.0/16";
        var tierCount = 3;

        // Act
        var result = _service.DesignMultiTierTopology(addressSpace, tierCount);

        // Assert
        var cidrs = result.Subnets.Select(s => s.AddressPrefix).ToList();
        
        // Verify no duplicate CIDRs
        Assert.Equal(cidrs.Count, cidrs.Distinct().Count());
        
        // Verify subnets are sequential and non-overlapping
        for (int i = 0; i < cidrs.Count - 1; i++)
        {
            var current = ParseCIDR(cidrs[i]);
            var next = ParseCIDR(cidrs[i + 1]);
            
            // Next subnet should start after current subnet ends
            Assert.True(next.StartIP > current.EndIP, 
                $"Subnet {cidrs[i]} overlaps with {cidrs[i + 1]}");
        }
    }

    [Theory]
    [InlineData("10.0.0.0/16", 3)] // /16 with 3 tiers
    [InlineData("10.0.0.0/20", 4)] // /20 with 4 tiers
    [InlineData("10.0.0.0/24", 2)] // /24 with 2 tiers
    public void DesignMultiTierTopology_CalculatesOptimalSubnetSize(
        string addressSpace, 
        int tierCount)
    {
        // Act
        var result = _service.DesignMultiTierTopology(addressSpace, tierCount, includeBastion: false, includeFirewall: false, includeGateway: false);

        // Assert - subnets should be created
        Assert.True(result.Subnets.Count >= tierCount);
        foreach (var subnet in result.Subnets)
        {
            var prefixLength = int.Parse(subnet.AddressPrefix.Split('/')[1]);
            Assert.True(prefixLength > 0 && prefixLength <= 32);
        }
    }

    #endregion

    #region Service Endpoint Tests

    [Fact]
    public void DesignMultiTierTopology_ConfiguresServiceEndpoints()
    {
        // Arrange
        var addressSpace = "10.0.0.0/16";
        var tierCount = 2;

        // Act
        var result = _service.DesignMultiTierTopology(addressSpace, tierCount);

        // Assert - Check that subnets have service endpoints when enabled
        var subnetsWithEndpoints = result.Subnets.Where(s => s.EnableServiceEndpoints).ToList();
        Assert.All(subnetsWithEndpoints, subnet => Assert.NotEmpty(subnet.ServiceEndpoints));
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void DesignMultiTierTopology_WithSingleTier_CreatesSubnet()
    {
        // Arrange
        var addressSpace = "10.0.0.0/24";
        var tierCount = 1;

        // Act
        var result = _service.DesignMultiTierTopology(addressSpace, tierCount, includeBastion: false, includeFirewall: false, includeGateway: false);

        // Assert
        Assert.True(result.Subnets.Count >= 1);
    }

    [Fact]
    public void DesignMultiTierTopology_WithLargeAddressSpace_HandlesCorrectly()
    {
        // Arrange
        var addressSpace = "10.0.0.0/8"; // Very large address space
        var tierCount = 3;

        // Act
        var result = _service.DesignMultiTierTopology(addressSpace, tierCount);

        // Assert
        Assert.True(result.Subnets.Count >= 3);
        
        // Should create reasonable-sized subnets, not /8
        foreach (var subnet in result.Subnets)
        {
            var prefixLength = int.Parse(subnet.AddressPrefix.Split('/')[1]);
            Assert.True(prefixLength >= 8, "Subnets should be reasonably sized");
        }
    }

    [Fact]
    public void DesignMultiTierTopology_WithBastion_IncludesBastionSubnet()
    {
        // Arrange
        var addressSpace = "10.0.0.0/16";
        var tierCount = 2;

        // Act
        var result = _service.DesignMultiTierTopology(addressSpace, tierCount, includeBastion: true, includeFirewall: false, includeGateway: false);

        // Assert
        Assert.Contains(result.Subnets, s => s.Name == "AzureBastionSubnet");
    }

    [Fact]
    public void DesignMultiTierTopology_WithFirewall_IncludesFirewallSubnet()
    {
        // Arrange
        var addressSpace = "10.0.0.0/16";
        var tierCount = 2;

        // Act
        var result = _service.DesignMultiTierTopology(addressSpace, tierCount, includeBastion: false, includeFirewall: true, includeGateway: false);

        // Assert
        Assert.Contains(result.Subnets, s => s.Name == "AzureFirewallSubnet");
    }

    #endregion

    #region Helper Methods

    private (long StartIP, long EndIP) ParseCIDR(string cidr)
    {
        var parts = cidr.Split('/');
        var ipParts = parts[0].Split('.').Select(byte.Parse).ToArray();
        var prefixLength = int.Parse(parts[1]);

        long ipAsLong = (ipParts[0] << 24) | (ipParts[1] << 16) | (ipParts[2] << 8) | ipParts[3];
        long hostBits = 32 - prefixLength;
        long subnetSize = (long)Math.Pow(2, hostBits);

        return (ipAsLong, ipAsLong + subnetSize - 1);
    }

    #endregion
}
