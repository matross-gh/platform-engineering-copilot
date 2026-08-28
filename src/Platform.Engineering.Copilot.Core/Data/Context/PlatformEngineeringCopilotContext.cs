using Microsoft.EntityFrameworkCore;
using Platform.Engineering.Copilot.Compliance.Core.Data.Entities;

// TODO: These entities should be moved to Core if needed, or this DbContext should be in Compliance.Agent
// using Platform.Engineering.Copilot.Compliance.Core.Data.Entities;
using Platform.Engineering.Copilot.Core.Data.Entities;

namespace Platform.Engineering.Copilot.Core.Data.Context;

/// <summary>
/// Environment Management Database Context
/// </summary>
public class PlatformEngineeringCopilotContext : DbContext
{
    public PlatformEngineeringCopilotContext(DbContextOptions<PlatformEngineeringCopilotContext> options)
        : base(options)
    {
    }

    // Environment Templates
    public DbSet<EnvironmentTemplate> EnvironmentTemplates { get; set; }
    public DbSet<TemplateVersion> TemplateVersions { get; set; }
    public DbSet<TemplateFile> TemplateFiles { get; set; }

    // Environment Deployments
    public DbSet<EnvironmentDeployment> EnvironmentDeployments { get; set; }
    public DbSet<DeploymentHistory> DeploymentHistory { get; set; }

    // Scaling
    public DbSet<ScalingPolicy> ScalingPolicies { get; set; }
    public DbSet<ScalingEvent> ScalingEvents { get; set; }

    // Metrics
    public DbSet<EnvironmentMetrics> EnvironmentMetrics { get; set; }

    // Agent Configuration
    public DbSet<AgentConfiguration> AgentConfigurations { get; set; }

    // Semantic Processing
    public DbSet<SemanticIntent> SemanticIntents { get; set; }
    public DbSet<IntentFeedback> IntentFeedback { get; set; }
    public DbSet<IntentPattern> IntentPatterns { get; set; }

    // Enhanced Environment Management
    public DbSet<EnvironmentLifecycle> EnvironmentLifecycles { get; set; }
    public DbSet<EnvironmentActivity> EnvironmentActivities { get; set; }
    public DbSet<EnvironmentCostTracking> EnvironmentCostTrackings { get; set; }
    public DbSet<EnvironmentClone> EnvironmentClones { get; set; }
    public DbSet<EnvironmentSynchronization> EnvironmentSynchronizations { get; set; }

    // Navy Flankspeed ServiceCreation
    // TODO: Uncomment when ServiceCreationRequest model is implemented
    // public DbSet<ServiceCreationRequest> ServiceCreationRequests { get; set; }

    // Governance and Approval Workflows
    public DbSet<ApprovalWorkflowEntity> ApprovalWorkflows { get; set; }

    public DbSet<ComplianceAssessment> ComplianceAssessments { get; set; }
    public DbSet<ComplianceFinding> ComplianceFindings { get; set; }

    // Audit Logging (NIST 800-53 AU-2, AU-3, AU-9)
    public DbSet<AuditLogEntity> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ------------------------------------------------------------------
        // Cosmos DB modeling notes:
        // - Each aggregate gets its own container (no cross-container joins/
        //   Include() are supported by the Cosmos EF provider). Repositories
        //   now do manual multi-query fetch + in-memory assembly instead.
        // - Partition key defaults to each entity's own id property. This is
        //   the simplest, safest starting point but means "fetch all X for
        //   parent Y" (e.g. DeploymentHistory by DeploymentId, EnvironmentMetrics
        //   by DeploymentId, ComplianceFindings by AssessmentId) is a
        //   cross-partition query. Revisit partition keys once real query/
        //   throughput patterns are known (e.g. partition DeploymentHistory by
        //   "/deploymentId" instead) - documented as a follow-up, not done here
        //   since it requires knowing real traffic patterns to choose well.
        // - SQL Server-only constructs removed: HasDefaultValueSql, HasIndex
        //   (Cosmos indexes all properties by default), HasForeignKey/
        //   OnDelete cascade (Cosmos has no cross-container FK enforcement -
        //   see repository code for manual cascade-delete of child documents),
        //   and unique HasIndex (Cosmos unique keys are partition-scoped only;
        //   uniqueness for fields like EnvironmentTemplate.Name is now enforced
        //   at the repository/application layer via a pre-insert existence check).
        // ------------------------------------------------------------------

        modelBuilder.Entity<EnvironmentTemplate>().ToContainer("EnvironmentTemplates").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<TemplateVersion>().ToContainer("TemplateVersions").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<TemplateFile>().ToContainer("TemplateFiles").HasPartitionKey(e => e.Id);

        modelBuilder.Entity<EnvironmentDeployment>().ToContainer("EnvironmentDeployments").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<EnvironmentDeployment>().HasQueryFilter(e => !e.IsDeleted); // soft delete, still supported on Cosmos provider
        modelBuilder.Entity<DeploymentHistory>().ToContainer("DeploymentHistory").HasPartitionKey(e => e.Id);

        modelBuilder.Entity<ScalingPolicy>().ToContainer("ScalingPolicies").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<ScalingEvent>().ToContainer("ScalingEvents").HasPartitionKey(e => e.Id);

        modelBuilder.Entity<EnvironmentMetrics>().ToContainer("EnvironmentMetrics").HasPartitionKey(e => e.Id);

        modelBuilder.Entity<AgentConfiguration>().ToContainer("AgentConfigurations").HasPartitionKey(e => e.AgentConfigurationId);

        modelBuilder.Entity<SemanticIntent>().ToContainer("SemanticIntents").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<IntentFeedback>().ToContainer("IntentFeedback").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<IntentPattern>().ToContainer("IntentPatterns").HasPartitionKey(e => e.Id);

        modelBuilder.Entity<EnvironmentLifecycle>().ToContainer("EnvironmentLifecycles").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<EnvironmentActivity>().ToContainer("EnvironmentActivities").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<EnvironmentCostTracking>().ToContainer("EnvironmentCostTrackings").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<EnvironmentClone>().ToContainer("EnvironmentClones").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<EnvironmentSynchronization>().ToContainer("EnvironmentSynchronizations").HasPartitionKey(e => e.Id);

        modelBuilder.Entity<ApprovalWorkflowEntity>().ToContainer("ApprovalWorkflows").HasPartitionKey(e => e.Id);

        modelBuilder.Entity<ComplianceAssessment>().ToContainer("ComplianceAssessments").HasPartitionKey(e => e.Id);
        modelBuilder.Entity<ComplianceFinding>().ToContainer("ComplianceFindings").HasPartitionKey(e => e.Id);

        ConfigureAuditLogs(modelBuilder);
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.ToContainer("AuditLogs").HasPartitionKey(e => e.EntryId);

            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.EntryId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Severity).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ActorId).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Result).HasMaxLength(50).IsRequired();

            // Note: RowVersion-based optimistic concurrency (SQL Server "rowversion")
            // is not supported by Cosmos. Cosmos has its own ETag-based concurrency
            // (the "_etag" shadow property EF Core maintains automatically); the
            // RowVersion property is kept on the entity as an informational field
            // only and is no longer configured as a concurrency token here.

            // Note: HasDefaultValue is a relational-only API not supported by the Cosmos
            // provider. IsArchived already defaults to false via the CLR bool default.
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is EnvironmentTemplate or EnvironmentDeployment or ScalingPolicy or IntentPattern or AgentConfiguration
                       or SemanticIntent or EnvironmentLifecycle &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                if (entityEntry.Property("CreatedAt") != null)
                    entityEntry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
            }

            if (entityEntry.Property("UpdatedAt") != null)
                entityEntry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }
    }
}