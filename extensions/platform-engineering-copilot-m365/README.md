# Platform Engineering Copilot - M365 Extension

AI-powered Azure infrastructure management directly in Microsoft Teams, Outlook, and Microsoft 365 Copilot.

## 🚀 Features

- **Natural Language Infrastructure Management**: Create and manage Azure resources using conversational language in Teams
- **Compliance Assessments**: Run NIST 800-53 ATO compliance scans from M365 Copilot
- **Cost Estimation**: Get real-time cost estimates for Azure resources
- **Rich Adaptive Cards**: Interactive results with drill-down capabilities
- **Proactive Guidance**: AI asks clarifying questions for incomplete requests
- **Multi-Plugin Support**: Works with Onboarding, Compliance, Infrastructure, Cost, and Deployment plugins

## 📦 Installation

### Prerequisites

- **Microsoft 365 Tenant** with Copilot licenses
- **Azure AD App Registration**
- **Platform Engineering Copilot API** running (http://localhost:7001 or deployed)
- **Node.js 18+**

### Setup

```bash
# Install dependencies
npm install

# Configure environment
cp .env.example .env
# Edit .env with your configuration

# Build the extension
npm run build

# Package for Teams
npm run package
```

This creates `platform-copilot-m365.zip` in the `dist/` folder.

### Deploy to Teams

1. Open Microsoft Teams
2. Go to **Apps** → **Manage your apps**
3. Click **Upload an app** → **Upload a custom app**
4. Select `dist/platform-copilot-m365.zip`
5. Add to your team or personal space

## 🎯 Usage

### In Microsoft Teams

Start a conversation with Copilot and use the Platform Copilot agent:

```
@Platform Copilot create a storage account named mldata001 in rg-ml-sbx-jrs

@Platform Copilot run compliance scan on my production subscription

@Platform Copilot estimate cost for AKS cluster with 3 nodes

@Platform Copilot show me all resources in rg-prod
```

### Conversation Starters

The extension provides quick-start prompts:

- 🏗️ **Provision Infrastructure**: "Create a storage account in my production resource group"
- ✅ **Run Compliance Scan**: "Check my subscription for NIST 800-53 compliance"
- 📊 **List Resources**: "Show me all resources in rg-prod"
- 💰 **Estimate Costs**: "What would it cost to create an AKS cluster?"

## 🏗️ Architecture

```
Microsoft Teams / M365 Copilot
    ↓
Declarative Agent (manifest.json)
    ↓
API Plugin (ai-plugin.json + OpenAPI)
    ↓
Platform Copilot M365 Service (Node.js)
    ↓
Platform Engineering Copilot API (:7001)
    ↓
Azure Resources
```

## 📁 Project Structure

```
platform-copilot-m365/
├── package.json
├── tsconfig.json
├── .env.example
├── README.md
├── src/
│   ├── index.ts                    # Main server entry point
│   ├── config.ts                   # Configuration management
│   ├── services/
│   │   ├── platformApiClient.ts    # Client for Platform Copilot API
│   │   ├── adaptiveCardBuilder.ts  # Builds rich Teams cards
│   │   └── messageHandler.ts       # Processes user messages
│   ├── appPackage/
│   │   ├── manifest.json           # Teams app manifest
│   │   ├── ai-plugin.json          # M365 Copilot plugin definition
│   │   ├── color.png               # App icon (192x192)
│   │   └── outline.png             # App icon outline (32x32)
│   └── openapi/
│       └── openapi.yaml            # API specification
└── dist/                           # Build output
```

## ⚙️ Configuration

### Environment Variables (`.env`)

```bash
# Platform Copilot API
PLATFORM_API_URL=http://localhost:7001
PLATFORM_API_KEY=your-api-key

# Azure AD
AZURE_AD_TENANT_ID=your-tenant-id
AZURE_AD_CLIENT_ID=your-client-id
AZURE_AD_CLIENT_SECRET=your-client-secret

# Server
PORT=3978
NODE_ENV=development

# M365 Copilot
BOT_ID=your-bot-id
BOT_PASSWORD=your-bot-password
```

## 🧪 Development

```bash
# Start in development mode with auto-reload
npm run dev

# Run tests
npm test

# Lint code
npm run lint

# Format code
npm run format
```

## 🚀 Deployment

### Deploy to Azure App Service

```bash
# Build for production
npm run build

# Deploy to Azure (requires Azure CLI)
az webapp up \
  --resource-group rg-platform-copilot \
  --name platform-copilot-m365 \
  --runtime "NODE:18-lts" \
  --src-path ./dist
```

### Update Teams App

After deployment, update the `validDomains` in `manifest.json`:

```json
"validDomains": [
  "platform-copilot-m365.azurewebsites.us",
  "*.ngrok.io"
]
```

Then repackage and re-upload to Teams.

## 📊 Example Interactions

### Scenario 1: Infrastructure Provisioning

**User**: "I need to create a storage account for my ML project"

**Platform Copilot**: 
```
I'll help you create a storage account. I need a few details:

❓ What should I name the storage account?
❓ Which resource group should it go in?
❓ What region? (usgovvirginia, usgovarizona, etc.)
```

**User**: "Name it mldata001, put it in rg-ml-sbx-jrs, use usgovvirginia"

**Platform Copilot**:
```
✅ Storage Account Created Successfully

📦 Resource Details:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Name:         mldata001
Resource ID:  /subscriptions/.../mldata001
Location:     usgovvirginia
SKU:          Standard_LRS
HTTPS Only:   Enabled
TLS Version:  1.2
Status:       Succeeded

[View in Azure Portal] [Create Container] [Configure Access]
```

### Scenario 2: Compliance Assessment

**User**: "@Platform Copilot run compliance scan for my prod subscription"

**Platform Copilot**:
```
🔍 Running NIST 800-53 Compliance Assessment...

✅ Assessment Complete (95% compliant)

📊 Results:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Passed Controls:  142/150
⚠️  Warnings:        5
❌ Failed:           3
📈 Overall Score:    95%

Top Issues:
1. ❌ AC-2: Public access enabled on 3 storage accounts
2. ⚠️  SC-8: TLS 1.0 allowed on 5 resources
3. ❌ AU-12: Missing audit logs on 2 VMs

[View Full Report] [Export PDF] [Remediation Plan]
```

## 🎨 Adaptive Cards

The extension uses rich Adaptive Cards for interactive responses:

- **Infrastructure Results**: Resource details with action buttons
- **Compliance Reports**: Visual compliance scores with drill-down
- **Cost Estimates**: Breakdown charts and comparisons
- **Error Messages**: Clear error descriptions with troubleshooting steps

## 🔒 Security

- **OAuth 2.0 Authentication**: Secure user authentication via Azure AD
- **API Key Management**: Encrypted storage of Platform API credentials
- **Role-Based Access**: Respects Azure RBAC permissions
- **Audit Logging**: All operations logged for compliance
- **Rate Limiting**: Prevents abuse and excessive API calls

## 📈 Monitoring

The extension includes Application Insights integration:

```typescript
// Tracks user interactions
telemetry.trackEvent('M365CopilotQuery', {
  userId: context.user.id,
  query: userMessage,
  intent: response.intentType,
  success: response.success
});
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📄 License

MIT License - see LICENSE file for details

## 🆘 Support

- **Documentation**: See `M365-COPILOT-INTEGRATION.md` in `/docs`
- **Issues**: Report bugs on GitHub
- **Email**: support@azurenoops.org

## 🎯 Roadmap

- [ ] Multi-language support (Spanish, French)
- [ ] Voice command integration
- [ ] Outlook calendar integration for scheduled deployments
- [ ] SharePoint document integration for architecture diagrams
- [ ] Power BI dashboard embedding
- [ ] Teams workflow automation

---

**Status**: Production Ready  
**Version**: 1.0.0  
**Last Updated**: October 17, 2025
