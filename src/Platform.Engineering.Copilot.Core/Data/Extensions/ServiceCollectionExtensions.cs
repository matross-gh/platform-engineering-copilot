using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Platform.Engineering.Copilot.Core.Data.Context;
using Platform.Engineering.Copilot.Core.Data.Repositories;
using Platform.Engineering.Copilot.Core.Services;

namespace Platform.Engineering.Copilot.Core.Data.Extensions;

/// <summary>
/// Service collection extensions for data layer configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Environment Management database context and services
    /// </summary>
    public static IServiceCollection AddEnvironmentManagementData(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection")
    {
        // Add Entity Framework DbContext (Cosmos DB)
        services.AddDbContext<PlatformEngineeringCopilotContext>(options =>
        {
            var endpoint = configuration["CosmosDb:Endpoint"]
                ?? configuration.GetConnectionString(connectionStringName)
                ?? throw new InvalidOperationException("Cosmos DB endpoint not found. Set 'CosmosDb:Endpoint' or connection string '" + connectionStringName + "'.");
            var key = configuration["CosmosDb:Key"] ?? string.Empty;
            var databaseName = configuration["CosmosDb:DatabaseName"] ?? "PlatformEngineeringCopilot";

            options.UseCosmosWithKeyOrEntraId(endpoint, key, databaseName);

            // Enable sensitive data logging in development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            // Enable query tracking optimization
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // Add repository services
        services.AddScoped<Repositories.IEnvironmentTemplateRepository, Repositories.EnvironmentTemplateRepository>();
        services.AddScoped<Repositories.IEnvironmentDeploymentRepository, Repositories.EnvironmentDeploymentRepository>();
        services.AddScoped<Repositories.IComplianceAssessmentRepository, Repositories.ComplianceAssessmentRepository>();
        
        // Semantic Intent Repository and Service (real implementations)
        services.AddScoped<Platform.Engineering.Copilot.Core.Data.Repositories.ISemanticIntentRepository, 
            Platform.Engineering.Copilot.Core.Data.Repositories.SemanticIntentRepository>();
        services.AddScoped<ISemanticIntentService, SemanticIntentService>();

        return services;
    }

    /// <summary>
    /// Add Environment Management database context with in-memory database (for testing)
    /// </summary>
    public static IServiceCollection AddEnvironmentManagementDataInMemory(
        this IServiceCollection services,
        string databaseName = "EnvironmentManagementTestDb")
    {
        services.AddDbContext<PlatformEngineeringCopilotContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName);
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // Add repository services
        services.AddScoped<Repositories.IEnvironmentTemplateRepository, Repositories.EnvironmentTemplateRepository>();
        services.AddScoped<Repositories.IEnvironmentDeploymentRepository, Repositories.EnvironmentDeploymentRepository>();
        services.AddScoped<Repositories.IComplianceAssessmentRepository, Repositories.ComplianceAssessmentRepository>();
        
        // Semantic Intent Repository and Service (real implementations)
        services.AddScoped<Platform.Engineering.Copilot.Core.Data.Repositories.ISemanticIntentRepository, 
            Platform.Engineering.Copilot.Core.Data.Repositories.SemanticIntentRepository>();
        services.AddScoped<ISemanticIntentService, SemanticIntentService>();

        return services;
    }
}