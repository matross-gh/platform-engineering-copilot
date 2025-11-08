# Integrating Platform Engineering Copilot with M365 Copilot

**Last Updated**: October 17, 2025  
**Status**: Implementation Guide

---

## Overview

This guide explains how to integrate the Platform Engineering Copilot with Microsoft 365 Copilot, enabling users to manage Azure infrastructure directly from Teams, Outlook, or other M365 apps.

---

## 🎯 Integration Options

### Option 1: Declarative Agent with API Plugin (Recommended)

**Best For**: Teams-based infrastructure management  
**Complexity**: Medium  
**Features**: Full natural language integration, authentication, rich responses

**Architecture**:
```
Microsoft Teams Chat
    ↓
M365 Copilot
    ↓
Declarative Agent (manifest.json)
    ↓
API Plugin (OpenAPI spec)
    ↓
Platform MCP Server (:5100)
    ↓
Azure Resources
```

### Option 2: Microsoft Graph Connector

**Best For**: Making infrastructure data searchable in M365  
**Complexity**: Low  
**Features**: Search integration, knowledge mining

### Option 3: Custom Copilot via Bot Framework

**Best For**: Standalone bot experience  
**Complexity**: High  
**Features**: Full control, custom UI

---

## 🚀 Option 1: Declarative Agent Setup (Recommended)

### Prerequisites

1. **Microsoft 365 Tenant** with Copilot licenses
2. **Azure AD App Registration** for authentication
3. **Teams Developer Account** (M365 E5 or Developer subscription)
4. **Teams Toolkit for VS Code** (or CLI)
5. **Public-facing endpoint** (ngrok, Azure App Service, or API Management)

### Step 1: Expose Your API Publicly

Your API needs to be accessible from M365 Copilot.

#### Option A: Use ngrok (Development)

```bash
# Install ngrok
brew install ngrok

# Start your API
cd /Users/johnspinella/repos/platform-engineering-copilot
cd src/Platform.Engineering.Copilot.API
dotnet run

# In another terminal, expose it
ngrok http 5100 --domain=<your-static-domain>.ngrok.io
```

#### Option B: Deploy to Azure App Service (Production)

```bash
# Create App Service
az webapp create \
  --resource-group rg-platform-copilot \
  --plan asp-platform-copilot \
  --name platform-engineering-copilot \
  --runtime "DOTNETCORE:8.0"

# Deploy
dotnet publish -c Release
az webapp deployment source config-zip \
  --resource-group rg-platform-copilot \
  --name platform-engineering-copilot \
  --src ./publish.zip
```

Your API will be at: `https://platform-engineering-copilot.azurewebsites.us` (Azure Gov)

---

### Step 2: Create OpenAPI Specification

M365 Copilot requires an OpenAPI spec to understand your API.

**File**: `openapi/platform-copilot-openapi.yaml`

```yaml
openapi: 3.0.1
info:
  title: Platform Engineering Copilot API
  description: AI-powered Azure infrastructure provisioning and management
  version: 1.0.0
servers:
  - url: https://platform-engineering-copilot.azurewebsites.us
    description: Production API
  - url: https://your-domain.ngrok.io
    description: Development API

paths:
  /api/chat/intelligent-query:
    post:
      operationId: queryInfrastructure
      summary: Ask natural language questions about Azure infrastructure
      description: Process infrastructure queries using AI
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                message:
                  type: string
                  description: Natural language query (e.g., "Create storage account in rg-prod")
                conversationId:
                  type: string
                  description: Conversation ID for context
                context:
                  type: object
                  description: Additional context
              required:
                - message
      responses:
        '200':
          description: Query processed successfully
          content:
            application/json:
              schema:
                type: object
                properties:
                  response:
                    type: string
                  intentType:
                    type: string
                  toolExecuted:
                    type: boolean

  /api/infrastructure/provision:
    post:
      operationId: provisionInfrastructure
      summary: Provision Azure infrastructure
      description: Create Azure resources from natural language description
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                query:
                  type: string
                  description: What to create (e.g., "storage account named mydata")
              required:
                - query
      responses:
        '200':
          description: Resource provisioned
          content:
            application/json:
              schema:
                type: object
                properties:
                  success:
                    type: boolean
                  resourceId:
                    type: string
                  message:
                    type: string

  /api/compliance/assess:
    post:
      operationId: runComplianceAssessment
      summary: Run ATO compliance assessment
      description: Scan Azure resources for NIST 800-53 compliance
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                subscriptionId:
                  type: string
                  description: Azure subscription ID or name
                resourceGroupName:
                  type: string
                  description: Optional resource group to scope assessment
              required:
                - subscriptionId
      responses:
        '200':
          description: Assessment completed
          content:
            application/json:
              schema:
                type: object
                properties:
                  overallScore:
                    type: number
                  findings:
                    type: array
                    items:
                      type: object

components:
  securitySchemes:
    oauth2:
      type: oauth2
      flows:
        authorizationCode:
          authorizationUrl: https://login.microsoftonline.com/common/oauth2/v2.0/authorize
          tokenUrl: https://login.microsoftonline.com/common/oauth2/v2.0/token
          scopes:
            api://platform-copilot/Infrastructure.Manage: Manage infrastructure
            api://platform-copilot/Compliance.Read: Read compliance data

security:
  - oauth2:
      - api://platform-copilot/Infrastructure.Manage
      - api://platform-copilot/Compliance.Read
```

**Generate from your API**:
```bash
# If you have Swashbuckle/Swagger configured
curl http://localhost:5100/swagger/v1/swagger.json > openapi/platform-copilot-openapi.json
```

---

### Step 3: Create Declarative Agent Manifest

**File**: `appPackage/manifest.json`

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.16/MicrosoftTeams.schema.json",
  "manifestVersion": "1.16",
  "version": "1.0.0",
  "id": "com.azurenoops.platform-copilot",
  "packageName": "com.azurenoops.platform-copilot",
  "developer": {
    "name": "Azure NoOps",
    "websiteUrl": "https://azurenoops.org",
    "privacyUrl": "https://azurenoops.org/privacy",
    "termsOfUseUrl": "https://azurenoops.org/terms"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "Platform Copilot",
    "full": "Platform Engineering Copilot"
  },
  "description": {
    "short": "AI-powered Azure infrastructure management",
    "full": "Provision Azure resources, run compliance assessments, and manage infrastructure using natural language in Microsoft Teams."
  },
  "accentColor": "#FFFFFF",
  "bots": [],
  "composeExtensions": [],
  "declarativeCopilots": [
    {
      "id": "platformCopilot",
      "name": "Platform Engineering Copilot",
      "description": "Manage Azure infrastructure with AI",
      "capabilities": [
        {
          "name": "WebSearch"
        },
        {
          "name": "OneDriveAndSharePoint"
        }
      ],
      "conversation_starters": [
        {
          "title": "Provision Infrastructure",
          "text": "Create a storage account in my production resource group"
        },
        {
          "title": "Run Compliance Scan",
          "text": "Check my subscription for NIST 800-53 compliance"
        },
        {
          "title": "List Resources",
          "text": "Show me all resources in rg-prod"
        },
        {
          "title": "Estimate Costs",
          "text": "What would it cost to create an AKS cluster?"
        }
      ],
      "instructions": "You are the Platform Engineering Copilot, an AI assistant that helps DevOps engineers and platform teams manage Azure infrastructure. You can provision resources, run compliance assessments, estimate costs, and answer questions about Azure best practices. Always confirm destructive actions before executing. Use Azure Government regions (usgovvirginia, usgovarizona) for production workloads."
    }
  ],
  "plugins": [
    {
      "pluginId": "platformCopilotAPI",
      "file": "ai-plugin.json"
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "platform-engineering-copilot.azurewebsites.us",
    "*.ngrok.io"
  ],
  "webApplicationInfo": {
    "id": "YOUR_AAD_APP_ID",
    "resource": "api://platform-copilot"
  }
}
```

---

### Step 4: Create AI Plugin Definition

**File**: `appPackage/ai-plugin.json`

```json
{
  "schema_version": "v2",
  "name_for_human": "Platform Engineering Copilot",
  "name_for_model": "platform_copilot",
  "description_for_human": "Manage Azure infrastructure using natural language",
  "description_for_model": "This plugin allows you to provision Azure resources, run compliance assessments, manage infrastructure, and answer questions about Azure best practices. It supports infrastructure-as-code generation, cost estimation, and NIST 800-53 compliance scanning.",
  "auth": {
    "type": "oauth",
    "authorization_url": "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
    "client_url": "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
    "authorization_content_type": "application/x-www-form-urlencoded",
    "scope": "api://platform-copilot/Infrastructure.Manage api://platform-copilot/Compliance.Read",
    "token_url": "https://login.microsoftonline.com/common/oauth2/v2.0/token"
  },
  "api": {
    "type": "openapi",
    "url": "https://platform-engineering-copilot.azurewebsites.us/openapi/v1/openapi.json",
    "has_user_authentication": true
  },
  "logo_url": "https://platform-engineering-copilot.azurewebsites.us/logo.png",
  "contact_email": "support@azurenoops.org",
  "legal_info_url": "https://azurenoops.org/legal"
}
```

---

### Step 5: Configure Azure AD Authentication

```bash
# Create AAD App Registration
az ad app create \
  --display-name "Platform Engineering Copilot - M365" \
  --sign-in-audience AzureADMyOrg \
  --web-redirect-uris \
    "https://token.botframework.com/.auth/web/redirect" \
    "https://platform-engineering-copilot.azurewebsites.us/auth/callback"

# Note the Application (client) ID
# Add API permissions:
# - Microsoft Graph: User.Read
# - Your API: Infrastructure.Manage, Compliance.Read

# Create client secret
az ad app credential reset --id YOUR_APP_ID
```

Update `appsettings.json`:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.us/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "Audience": "api://platform-copilot"
  }
}
```

---

### Step 6: Deploy to Teams

#### Using Teams Toolkit

1. **Install Teams Toolkit** for VS Code:
   ```bash
   code --install-extension TeamsDevApp.ms-teams-vscode-extension
   ```

2. **Open the project** in VS Code

3. **Create Teams App Package**:
   - Create `appPackage/` folder with:
     - `manifest.json`
     - `ai-plugin.json`
     - `color.png` (192x192)
     - `outline.png` (32x32)

4. **Package & Deploy**:
   ```bash
   # From appPackage directory
   zip -r platform-copilot.zip manifest.json ai-plugin.json *.png
   ```

5. **Upload to Teams**:
   - Open Microsoft Teams
   - Go to **Apps** > **Manage your apps**
   - Click **Upload an app** > **Upload a custom app**
   - Select `platform-copilot.zip`

#### Using CLI

```bash
# Install Teams CLI
npm install -g @microsoft/teamsfx-cli

# Login
teamsfx account login m365

# Deploy
teamsfx deploy
```

---

### Step 7: Test in Microsoft 365 Copilot

1. **Open Microsoft Teams**
2. **Start a new chat** with Copilot
3. **Enable your plugin**:
   - Type `@Platform Copilot`
   - Or go to Copilot settings and enable "Platform Engineering Copilot"

4. **Test prompts**:
   ```
   @Platform Copilot create a storage account named testdata001 in resource group rg-dev
   
   @Platform Copilot run a compliance assessment for my production subscription
   
   @Platform Copilot what resources are in rg-ml-sbx-jrs?
   
   @Platform Copilot estimate the cost of deploying an AKS cluster with 3 nodes
   ```

---

## 🔧 Code Changes Needed

### 1. Add OpenAPI/Swagger Support

Update `Program.cs`:

```csharp
// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Platform Engineering Copilot API",
        Version = "v1",
        Description = "AI-powered Azure infrastructure management",
        Contact = new OpenApiContact
        {
            Name = "Azure NoOps",
            Email = "support@azurenoops.org",
            Url = new Uri("https://azurenoops.org")
        }
    });

    // Add OAuth2 security definition
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("https://login.microsoftonline.us/common/oauth2/v2.0/authorize"),
                TokenUrl = new Uri("https://login.microsoftonline.us/common/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "api://platform-copilot/Infrastructure.Manage", "Manage infrastructure" },
                    { "api://platform-copilot/Compliance.Read", "Read compliance data" }
                }
            }
        }
    });
});

// Enable Swagger in Production (for M365 Copilot)
app.UseSwagger();
app.UseSwaggerUI();
```

### 2. Add CORS for M365 Copilot

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("M365Copilot", policy =>
    {
        policy.WithOrigins(
            "https://teams.microsoft.com",
            "https://outlook.office.com",
            "https://www.office.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

app.UseCors("M365Copilot");
```

### 3. Add Authentication Middleware

```csharp
using Microsoft.Identity.Web;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

app.UseAuthentication();
app.UseAuthorization();
```

### 4. Create Simplified API Endpoints

Add `InfrastructureController.cs`:

```csharp
[ApiController]
[Route("api/infrastructure")]
[Authorize]
public class InfrastructureController : ControllerBase
{
    private readonly IInfrastructureProvisioningService _provisioningService;

    [HttpPost("provision")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InfrastructureProvisionResult>> Provision(
        [FromBody] ProvisionRequest request)
    {
        var result = await _provisioningService.ProvisionInfrastructureAsync(
            request.Query, 
            HttpContext.RequestAborted);
        
        return Ok(result);
    }
}

public record ProvisionRequest(string Query);
```

---

## 📊 User Experience in Teams

### Example Conversation

**User**: `@Platform Copilot create a storage account for my ML project`

**Copilot**: I'll help you create a storage account. I need a few details:
- What should I name the storage account?
- Which resource group should it go in?
- What region? (usgovvirginia, usgovarizona, etc.)

**User**: `name it mldata001, put it in rg-ml-sbx-jrs in usgovvirginia`

**Copilot**: ✅ Creating storage account `mldata001` in resource group `rg-ml-sbx-jrs` (usgovvirginia)...

[Progress indicator]

✅ **Storage Account Created Successfully**

📦 **Resource Details:**
- **Name**: mldata001
- **Resource ID**: /subscriptions/.../mldata001
- **Location**: usgovvirginia
- **SKU**: Standard_LRS
- **HTTPS Only**: Enabled
- **TLS Version**: 1.2

Would you like me to create any blob containers or configure access policies?

---

## 🎨 UI Enhancements

### Adaptive Cards for Rich Responses

Use Adaptive Cards for formatted responses in Teams:

```csharp
public class AdaptiveCardBuilder
{
    public static AdaptiveCard BuildProvisionResult(InfrastructureProvisionResult result)
    {
        return new AdaptiveCard("1.5")
        {
            Body = new List<AdaptiveElement>
            {
                new AdaptiveTextBlock
                {
                    Text = result.Success ? "✅ Resource Provisioned" : "❌ Provisioning Failed",
                    Size = AdaptiveTextSize.Large,
                    Weight = AdaptiveTextWeight.Bolder,
                    Color = result.Success ? AdaptiveTextColor.Good : AdaptiveTextColor.Attention
                },
                new AdaptiveFactSet
                {
                    Facts = new List<AdaptiveFact>
                    {
                        new AdaptiveFact("Resource Name", result.ResourceName),
                        new AdaptiveFact("Resource Type", result.ResourceType),
                        new AdaptiveFact("Location", result.Properties["location"]),
                        new AdaptiveFact("Status", result.Status)
                    }
                }
            },
            Actions = new List<AdaptiveAction>
            {
                new AdaptiveOpenUrlAction
                {
                    Title = "View in Azure Portal",
                    Url = new Uri($"https://portal.azure.us/#resource{result.ResourceId}")
                }
            }
        };
    }
}
```

---

## 🔒 Security Considerations

### 1. **Authentication**
- ✅ Use Azure AD OAuth 2.0
- ✅ Validate JWT tokens
- ✅ Implement role-based access control (RBAC)

### 2. **Authorization**
- Check user permissions before executing operations
- Implement approval workflows for production changes
- Log all infrastructure changes

### 3. **Rate Limiting**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

---

## 📈 Monitoring & Logging

### Application Insights Integration

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// Log M365 Copilot interactions
public class M365CopilotTelemetry
{
    private readonly TelemetryClient _telemetry;

    public void TrackCopilotQuery(string userId, string query, string intent)
    {
        _telemetry.TrackEvent("M365CopilotQuery", new Dictionary<string, string>
        {
            { "UserId", userId },
            { "Query", query },
            { "Intent", intent },
            { "Timestamp", DateTime.UtcNow.ToString() }
        });
    }
}
```

---

## 🚀 Deployment Checklist

- [ ] API publicly accessible (Azure App Service or ngrok)
- [ ] OpenAPI spec published
- [ ] Azure AD app registered
- [ ] OAuth configured
- [ ] Teams app manifest created
- [ ] AI plugin definition created
- [ ] Icons prepared (192x192 and 32x32)
- [ ] App package zipped
- [ ] Uploaded to Teams Admin Center
- [ ] Tested in Microsoft Teams
- [ ] Monitored with Application Insights

---

## 🎓 Training Users

### Quick Start Guide for End Users

**Getting Started with Platform Copilot in Teams:**

1. **Enable the Plugin**:
   - Open Microsoft Teams
   - Click on Copilot
   - Go to Settings → Plugins
   - Enable "Platform Engineering Copilot"

2. **Start Chatting**:
   - Type `@Platform Copilot` to invoke it
   - Or use conversation starters

3. **Common Commands**:
   - "Create a storage account..."
   - "Run compliance scan for..."
   - "List resources in..."
   - "Estimate cost of..."

4. **Get Help**:
   - Type `@Platform Copilot help`
   - Visit internal documentation portal

---

## 📚 Additional Resources

- [Microsoft 365 Copilot Extensibility](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/)
- [Declarative Agents for M365 Copilot](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/build-declarative-agents)
- [Teams Toolkit Documentation](https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/teams-toolkit-fundamentals)
- [API Plugin Development](https://learn.microsoft.com/en-us/semantic-kernel/agents-and-plugins/plugins/)

---

## 🎯 Success Metrics

Track these KPIs for M365 Copilot integration:

- **Adoption Rate**: % of users who enabled the plugin
- **Query Volume**: Queries per day/week
- **Intent Accuracy**: % of queries correctly classified
- **Success Rate**: % of operations completed successfully
- **Time Savings**: Average time saved vs manual Azure Portal
- **User Satisfaction**: NPS score from feedback

---

**Status**: Ready for implementation  
**Next Steps**: 
1. Create OpenAPI spec from existing API
2. Register Azure AD application
3. Create Teams app manifest
4. Deploy and test in Teams

