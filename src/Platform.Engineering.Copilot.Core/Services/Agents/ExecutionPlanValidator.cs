using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Models.Agents;

namespace Platform.Engineering.Copilot.Core.Services.Agents;

/// <summary>
/// Validates and corrects execution plans to ensure they match user intent
/// </summary>
public class ExecutionPlanValidator
{
    private readonly ILogger<ExecutionPlanValidator> _logger;

    public ExecutionPlanValidator(ILogger<ExecutionPlanValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validate and potentially correct an execution plan based on user message
    /// </summary>
    public ExecutionPlan ValidateAndCorrect(ExecutionPlan plan, string userMessage, string conversationId)
    {
        // CRITICAL: Check for compliance scanning requests FIRST - this takes precedence over all other validations
        if (IsComplianceScanningRequest(userMessage))
        {
            // If plan correctly has Compliance agent, return as-is
            if (plan.Tasks.Any(t => t.AgentType == AgentType.Compliance))
            {
                _logger.LogInformation("✅ Plan validation: Compliance scanning request correctly routed to ComplianceAgent");
                return plan; // STOP HERE - compliance scan is correctly routed
            }
            
            // If plan is wrong (e.g., Infrastructure), correct it
            _logger.LogWarning("⚠️  Plan validation: Compliance scanning request incorrectly routed, correcting to ComplianceAgent");
            return CreateComplianceScanningPlan(userMessage, conversationId); // STOP HERE - corrected to compliance
        }

        // CRITICAL: Check for actual provisioning requests SECOND - user explicitly wants to deploy
        if (IsActualProvisioningRequest(userMessage))
        {
            // If plan has all 5 agents, it's correct
            if (HasAllProvisioningAgents(plan))
            {
                _logger.LogInformation("✅ Plan validation: Actual provisioning request correctly routed to all 5 agents");
                return plan;
            }
            
            // If plan is incomplete (missing agents), correct it
            _logger.LogWarning("⚠️  Plan validation: Actual provisioning request missing agents, correcting to full workflow");
            return CreateActualProvisioningPlan(userMessage, conversationId);
        }

        // ONLY check template generation if NOT a compliance scan or actual provisioning
        // Check if this is a template generation request being treated as provisioning
        if (IsTemplateGenerationRequest(userMessage) && !IsActualProvisioningRequest(userMessage) && HasProvisioningAgents(plan))
        {
            _logger.LogWarning("⚠️  Plan validation: Template generation request incorrectly includes provisioning agents");
            return CreateTemplateGenerationPlan(userMessage, conversationId);
        }

        return plan;
    }

    private bool IsActualProvisioningRequest(string userMessage)
    {
        var lowerMessage = userMessage.ToLowerInvariant();

        // Strong indicators of actual provisioning - user explicitly wants to deploy
        var provisioningIndicators = new[]
        {
            "actually provision", "make it live", "make this live",
            "execute deployment", "create the resources now", "deploy the template",
            "provision for real", "provision this", "deploy this now",
            "create resources now", "execute this", "provision now"
        };

        // Check for strong multi-word indicators (explicit provisioning intent)
        if (provisioningIndicators.Any(i => lowerMessage.Contains(i)))
        {
            _logger.LogInformation("🚀 DETECTED ACTUAL PROVISIONING REQUEST: '{UserMessage}'", userMessage);
            return true;
        }

        // Check for combination of urgency + provisioning keywords
        var urgencyKeywords = new[] { "actually", "now", "immediately", "right now", "execute", "live" };
        var provisioningKeywords = new[] { "provision", "deploy", "create resources", "make it", "deployment" };

        var hasUrgency = urgencyKeywords.Any(v => lowerMessage.Contains(v));
        var hasProvisioning = provisioningKeywords.Any(k => lowerMessage.Contains(k));

        if (hasUrgency && hasProvisioning)
        {
            _logger.LogInformation("🚀 DETECTED ACTUAL PROVISIONING REQUEST (urgency + provisioning): '{UserMessage}'", userMessage);
            return true;
        }

        return false;
    }

    private bool HasAllProvisioningAgents(ExecutionPlan plan)
    {
        // Check if plan has all 5 agents needed for actual provisioning
        // Correct execution order: Infrastructure (1) → Environment (2) → Discovery (3) → Compliance (4) → Cost (5)
        var requiredAgents = new[]
        {
            AgentType.Infrastructure,  // Priority 1: Generate/validate template
            AgentType.Environment,     // Priority 2: Deploy resources
            AgentType.Discovery,       // Priority 3: Verify created resources in new RG
            AgentType.Compliance,      // Priority 4: Scan new RG only (not entire subscription)
            AgentType.CostManagement   // Priority 5: Estimate costs
        };

        var hasAllAgents = requiredAgents.All(agentType => 
            plan.Tasks.Any(t => t.AgentType == agentType));

        if (hasAllAgents)
        {
            _logger.LogInformation("✅ Plan has all 5 provisioning agents in correct order: Infrastructure (1) → Environment (2) → Discovery (3) → Compliance (4) → CostManagement (5)");
        }
        else
        {
            var presentAgents = plan.Tasks.Select(t => t.AgentType).Distinct().ToList();
            var missingAgents = requiredAgents.Except(presentAgents).ToList();
            _logger.LogWarning("⚠️  Plan missing provisioning agents: {MissingAgents}", string.Join(", ", missingAgents));
        }

        return hasAllAgents;
    }

    private ExecutionPlan CreateActualProvisioningPlan(string userMessage, string conversationId)
    {
        _logger.LogInformation("✅ Creating corrected plan for ACTUAL PROVISIONING with all 5 agents");

        return new ExecutionPlan
        {
            PrimaryIntent = "provisioning",
            ExecutionPattern = ExecutionPattern.Sequential, // Sequential: Infrastructure → Environment → Discovery → Compliance → Cost
            EstimatedTimeSeconds = 120, // 60-180 seconds for actual provisioning
            Tasks = new List<AgentTask>
            {
                // Step 1: Infrastructure - Generate/validate template FIRST
                new AgentTask
                {
                    AgentType = AgentType.Infrastructure,
                    Description = userMessage,
                    Priority = 1, // FIRST: Generate template before deployment
                    IsCritical = true,
                    ConversationId = conversationId
                },
                // Step 2: Environment - Deploy resources SECOND
                new AgentTask
                {
                    AgentType = AgentType.Environment,
                    Description = $"Deploy the infrastructure template: {userMessage}",
                    Priority = 2, // SECOND: Deploy after template is ready
                    IsCritical = true,
                    ConversationId = conversationId
                },
                // Step 3: Discovery - Verify created resources THIRD
                new AgentTask
                {
                    AgentType = AgentType.Discovery,
                    Description = "Discover and verify newly created resources in the deployed resource group",
                    Priority = 3, // THIRD: Discover after deployment completes
                    IsCritical = false,
                    ConversationId = conversationId
                },
                // Step 4: Compliance - Scan ONLY the new resource group FOURTH
                new AgentTask
                {
                    AgentType = AgentType.Compliance,
                    Description = "Perform compliance scan on newly created resource group only (not entire subscription)",
                    Priority = 4, // FOURTH: Scan after resources exist
                    IsCritical = false,
                    ConversationId = conversationId
                },
                // Step 5: CostManagement - Estimate costs LAST
                new AgentTask
                {
                    AgentType = AgentType.CostManagement,
                    Description = "Estimate monthly costs for deployed resources",
                    Priority = 5, // FIFTH: Estimate costs after everything is deployed
                    IsCritical = false,
                    ConversationId = conversationId
                }
            }
        };
    }

    private ExecutionPlan CreateInfrastructurePlan(string userMessage, string conversationId)
    {
        _logger.LogInformation("✅ Correcting plan to infrastructure-only (without provisioning agents)");

        return new ExecutionPlan
        {
            PrimaryIntent = "infrastructure",
            ExecutionPattern = ExecutionPattern.Sequential,
            EstimatedTimeSeconds = 30,
            Tasks = new List<AgentTask>
            {
                new AgentTask
                {
                    AgentType = AgentType.Infrastructure,
                    Description = userMessage,
                    Priority = 1,
                    IsCritical = true,
                    ConversationId = conversationId
                }
            }
        };
    }

    private bool IsComplianceScanningRequest(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        // Strong indicators of compliance scanning (checking existing resources)
        var scanningIndicators = new[]
        {
            "check compliance", "run a compliance assessment", "scan my subscription",
            "compliance status", "security assessment", "assess compliance",
            "validate compliance", "audit", "compliance scan"
        };

        // Check for strong multi-word indicators first
        if (scanningIndicators.Any(i => lowerMessage.Contains(i)))
        {
            return true;
        }

        // Check for combination of action + compliance keywords
        var actionVerbs = new[] { "check", "scan", "assess", "validate", "audit", "evaluate", "review" };
        var complianceKeywords = new[] { "compliance", "compliant", "nist", "fedramp", "security" };

        var hasActionVerb = actionVerbs.Any(v => lowerMessage.Contains(v));
        var hasComplianceKeyword = complianceKeywords.Any(k => lowerMessage.Contains(k));

        return hasActionVerb && hasComplianceKeyword;
    }

    private ExecutionPlan CreateComplianceScanningPlan(string userMessage, string conversationId)
    {
        _logger.LogInformation("✅ Creating corrected plan for compliance scanning");

        return new ExecutionPlan
        {
            PrimaryIntent = "compliance",
            ExecutionPattern = ExecutionPattern.Sequential,
            EstimatedTimeSeconds = 60,
            Tasks = new List<AgentTask>
            {
                new AgentTask
                {
                    AgentType = AgentType.Compliance,
                    Description = userMessage,
                    Priority = 1,
                    IsCritical = true,
                    ConversationId = conversationId
                }
            }
        };
    }

    private bool IsTemplateGenerationRequest(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        // CRITICAL: Check if this is actually a provisioning request FIRST
        // If user says "actually provision", they want provisioning, NOT template generation
        if (IsActualProvisioningRequest(message))
        {
            return false; // NOT a template generation request - it's provisioning!
        }

        // Indicators of template generation (when NOT provisioning)
        var templateIndicators = new[]
        {
            "template", "bicep", "arm", "iac", "blueprint", "terraform",
            "generate code", "show me the code", "infrastructure code"
        };

        var hasTemplateIndicator = templateIndicators.Any(i => lowerMessage.Contains(i));

        // If they explicitly ask for a template or code, it's template generation
        if (hasTemplateIndicator)
        {
            return true;
        }

        // Generic deployment words like "deploy", "create", "set up" are template generation
        // UNLESS they have urgency/provisioning keywords
        var deploymentWords = new[] { "deploy", "create", "set up", "i need", "provision" };
        var hasDeploymentWord = deploymentWords.Any(w => lowerMessage.Contains(w));

        // Only consider it template generation if it has deployment words
        // AND doesn't have actual provisioning intent
        return hasDeploymentWord && !IsActualProvisioningRequest(message);
    }

    private bool HasProvisioningAgents(ExecutionPlan plan)
    {
        var provisioningAgents = new[]
        {
            AgentType.Compliance,
            AgentType.Infrastructure,
            AgentType.Discovery,
            AgentType.Environment,
            AgentType.CostManagement
        };

        return plan.Tasks.Any(t => provisioningAgents.Contains(t.AgentType));
    }

    private ExecutionPlan CreateTemplateGenerationPlan(string userMessage, string conversationId)
    {
        _logger.LogInformation("✅ Correcting plan to infrastructure-only template generation");

        return new ExecutionPlan
        {
            PrimaryIntent = "template_generation",
            ExecutionPattern = ExecutionPattern.Sequential,
            EstimatedTimeSeconds = 30,
            Tasks = new List<AgentTask>
            {
                new AgentTask
                {
                    AgentType = AgentType.Infrastructure,
                    Description = userMessage,
                    Priority = 1,
                    IsCritical = true,
                    ConversationId = conversationId
                }
            }
        };
    }
}
