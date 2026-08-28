using Azure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Platform.Engineering.Copilot.Core.Data.Extensions;

/// <summary>
/// Configures EF Core's Cosmos DB provider, choosing key-based auth (e.g. the local
/// Cosmos DB Emulator) when a key is supplied, or Entra ID/RBAC via
/// DefaultAzureCredential when no key is configured (production accounts with local
/// auth disabled).
/// </summary>
public static class CosmosDbContextOptionsExtensions
{
    public static void UseCosmosWithKeyOrEntraId(
        this DbContextOptionsBuilder optionsBuilder,
        string accountEndpoint,
        string? accountKey,
        string databaseName)
    {
        if (!string.IsNullOrWhiteSpace(accountKey))
        {
            optionsBuilder.UseCosmos(accountEndpoint, accountKey, databaseName);
        }
        else
        {
            optionsBuilder.UseCosmos(accountEndpoint, new DefaultAzureCredential(), databaseName);
        }
    }
}
