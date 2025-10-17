# Platform Engineering Copilot for GitHub Copilot

**AI-Powered Azure Infrastructure Management directly in VS Code**

This VS Code extension integrates Platform Engineering Copilot with GitHub Copilot Chat, enabling natural language interactions for Azure infrastructure provisioning, compliance scanning, cost analysis, and more.

---

## 🌟 Features

### 🏗️ **Infrastructure Management**
- Provision Azure resources using natural language
- Deploy Bicep and Terraform templates
- List and manage resource groups
- Real-time resource health monitoring

### 🔒 **Compliance & Security**
- NIST 800-53 compliance assessments
- NIST 800-171 and ISO 27001 support
- Automated remediation plans
- eMASS package generation
- Security hardening recommendations

### 💰 **Cost Management**
- Real-time cost estimation
- Resource cost breakdowns
- Optimization recommendations
- Budget alerts and tracking

### 🚀 **Deployment Operations**
- Bicep template validation and deployment
- Terraform deployments
- Deployment status tracking
- Rollback capabilities

### 🌐 **Environment Management**
- Create and clone environments
- Environment health monitoring
- Scaling operations
- Resource discovery

---

## 📋 Prerequisites

1. **VS Code**: Version 1.90.0 or higher
2. **GitHub Copilot**: Active subscription with Chat enabled
3. **Platform Copilot API**: Running locally or remotely
   - Default: `http://localhost:7001`
   - Can be configured in settings

---

## 🚀 Installation

### Option 1: Install from VSIX (Recommended)

```bash
# Download the latest .vsix file
# Then in VS Code:
code --install-extension platform-copilot-github-1.0.0.vsix
```

### Option 2: Build from Source

```bash
# Clone the repository
cd extensions/platform-engineering-copilot-github

# Install dependencies
npm install

# Compile TypeScript
npm run compile

# Package extension
npm run package

# Install
code --install-extension platform-copilot-github-1.0.0.vsix
```

---

## ⚙️ Configuration

### VS Code Settings

Press `Cmd+,` (Mac) or `Ctrl+,` (Windows/Linux) and search for "Platform Copilot":

```json
{
  "platform-copilot.apiUrl": "http://localhost:7001",
  "platform-copilot.apiKey": "",
  "platform-copilot.timeout": 60000,
  "platform-copilot.enableLogging": true
}
```

### Quick Configuration

Run the command **Platform Copilot: Configure Platform API Connection**

Or use the Command Palette (`Cmd+Shift+P`):
- `Platform Copilot: Check Platform API Health`
- `Platform Copilot: Configure Platform API Connection`

---

## 💬 Usage

### Using GitHub Copilot Chat

Open GitHub Copilot Chat and use the `@platform` participant:

#### **Provision Infrastructure**

```
@platform Create a storage account named mydata001 in resource group rg-dev
```

```
@platform Provision an AKS cluster with 3 nodes in East US
```

```
@platform Deploy a virtual network with 2 subnets
```

#### **Compliance Scanning**

```
@platform Run a NIST 800-53 compliance scan on subscription abc-123
```

```
@platform Check compliance for resource group rg-production
```

```
@platform Generate an eMASS package for my production environment
```

#### **Cost Analysis**

```
@platform Estimate the cost of a Standard_D4s_v3 VM for 720 hours
```

```
@platform Show me the cost breakdown for resource group rg-dev
```

```
@platform How much will it cost to run 5 AKS nodes for a month?
```

#### **Resource Discovery**

```
@platform List all resources in East US
```

```
@platform Show me storage accounts in resource group rg-data
```

```
@platform What VMs are running in my subscription?
```

#### **Deployments**

```
@platform Deploy the Bicep template in ./infra/main.bicep
```

```
@platform Check the status of deployment dep-12345
```

```
@platform Validate my Terraform configuration
```

---

## 🎯 Example Workflows

### Scenario 1: Complete Infrastructure Provisioning

```
You: @platform I need to create a new development environment

Platform: I'll help you set up a development environment. Could you provide:
1. Resource group name
2. Location (e.g., East US, West US 2)
3. Required services (e.g., AKS, Storage, SQL)

You: @platform Resource group: rg-dev-001, Location: East US, Services: AKS and Storage

Platform: ✅ Creating development environment...

Created resources:
- Resource Group: rg-dev-001
- AKS Cluster: aks-dev-001 (3 nodes, Standard_D2s_v3)
- Storage Account: stdev001 (Standard_LRS)

Estimated monthly cost: $250.00
```

### Scenario 2: Compliance Assessment and Remediation

```
You: @platform Run compliance scan on resource group rg-production

Platform: 🔒 Running NIST 800-53 compliance assessment...

Compliance Score: 78%
- Passed: 156 controls
- Failed: 34 controls
- Warnings: 12 controls

Top issues:
1. Missing encryption at rest (AC-2)
2. No network security groups (SC-7)
3. Diagnostic logs not enabled (AU-2)

Would you like me to generate a remediation plan?

You: @platform Yes, create a remediation plan

Platform: ✅ Remediation plan created with 3 automated fixes.
Estimated time: 15 minutes
Would you like me to execute the plan?
```

### Scenario 3: Cost Optimization

```
You: @platform Analyze costs for my subscription

Platform: 💰 Cost Analysis Summary:

Total monthly spend: $5,234.00

Top 5 expensive resources:
1. AKS Cluster (aks-prod-001): $1,800/month
2. SQL Database (sql-prod-db): $950/month
3. Storage Account (stprod001): $430/month
4. VMs (3x Standard_D4s_v3): $1,200/month
5. Azure Firewall: $854/month

💡 Optimization opportunities:
- Switch to Reserved Instances: Save $450/month
- Enable auto-shutdown for dev VMs: Save $200/month
- Archive old blob storage: Save $120/month

Total potential savings: $770/month (15%)
```

---

## 🏗️ Architecture

```
GitHub Copilot Chat (@platform)
    ↓
VS Code Extension (TypeScript)
    ↓
Platform API Client (Axios)
    ↓
Platform Copilot API (:7001)
    ↓
Semantic Kernel + GPT-4
    ↓
Azure Resources
```

### Components

**1. Chat Participant** (`chatParticipant.ts`)
- Handles GitHub Copilot Chat requests
- Routes to Platform API
- Formats responses with rich markdown
- Manages conversation history

**2. Platform API Client** (`services/platformApiClient.ts`)
- HTTP client for Platform API
- Request/response interceptors
- Error handling with retry logic
- Typed interfaces for all operations

**3. Configuration Manager** (`config.ts`)
- Centralized settings management
- Validation and logging
- Hot-reload on settings changes

---

## 🔧 Development

### Project Structure

```
platform-engineering-copilot-github/
├── src/
│   ├── extension.ts              # Extension entry point
│   ├── chatParticipant.ts        # GitHub Copilot Chat handler
│   ├── config.ts                 # Configuration management
│   └── services/
│       └── platformApiClient.ts  # Platform API client
├── package.json                  # Extension manifest
├── tsconfig.json                 # TypeScript configuration
├── .env.example                  # Environment template
└── README.md                     # This file
```

### Build Commands

```bash
# Install dependencies
npm install

# Compile TypeScript
npm run compile

# Watch mode (auto-compile on changes)
npm run watch

# Lint code
npm run lint

# Format code
npm run format

# Package extension
npm run package
```

### Testing

```bash
# Press F5 in VS Code to launch Extension Development Host
# Or use the command:
npm test
```

### Debugging

1. Open project in VS Code
2. Press `F5` to launch Extension Development Host
3. Set breakpoints in source code
4. Use GitHub Copilot Chat with `@platform` in the dev window

---

## 📝 Configuration Examples

### Enterprise Setup (Azure Government)

```json
{
  "platform-copilot.apiUrl": "https://platform-api.azurewebsites.us",
  "platform-copilot.apiKey": "your-production-api-key",
  "platform-copilot.timeout": 120000,
  "platform-copilot.enableLogging": false
}
```

### Local Development

```json
{
  "platform-copilot.apiUrl": "http://localhost:7001",
  "platform-copilot.apiKey": "",
  "platform-copilot.timeout": 60000,
  "platform-copilot.enableLogging": true
}
```

### Testing Environment

```json
{
  "platform-copilot.apiUrl": "https://platform-api-dev.azurewebsites.us",
  "platform-copilot.apiKey": "dev-api-key",
  "platform-copilot.timeout": 90000,
  "platform-copilot.enableLogging": true
}
```

---

## 🆘 Troubleshooting

### Extension Not Activating

**Problem**: Extension doesn't appear in GitHub Copilot Chat

**Solutions**:
1. Verify GitHub Copilot Chat is installed and enabled
2. Check VS Code version (must be 1.90.0+)
3. Reload VS Code: `Developer: Reload Window`
4. Check Output panel: `View > Output > Platform Engineering Copilot`

### Cannot Connect to Platform API

**Problem**: `Could not reach Platform API` error

**Solutions**:
1. Verify Platform API is running:
   ```bash
   curl http://localhost:7001/health
   ```
2. Check API URL in settings
3. Run command: `Platform Copilot: Check Platform API Health`
4. Review firewall/network settings

### Timeout Errors

**Problem**: Requests timing out

**Solutions**:
1. Increase timeout in settings: `platform-copilot.timeout`
2. Check Platform API performance
3. Verify network connectivity
4. Review API logs for slow operations

### Chat Participant Not Responding

**Problem**: `@platform` doesn't respond

**Solutions**:
1. Check if extension is activated (green checkmark in Extensions panel)
2. Look for errors in Output panel
3. Verify GitHub Copilot subscription is active
4. Try reloading VS Code

---

## 📚 Additional Resources

- [Platform Engineering Copilot Documentation](../../docs/)
- [GitHub Copilot Integration Guide](../../docs/GITHUB-COPILOT-INTEGRATION.md)
- [M365 Copilot Integration Guide](../../docs/M365-COPILOT-INTEGRATION.md)
- [Chat Application Integration](../../docs/CHAT-APPLICATION-INTEGRATION.md)
- [Platform API Documentation](../../README.md)

---

## 🤝 Contributing

Contributions are welcome! Please see the main repository for contribution guidelines.

---

## 📄 License

MIT License - see [LICENSE](LICENSE) for details

---

## 🎉 What's New

### Version 1.0.0 (Current)

- ✅ Complete refactor with modern architecture
- ✅ Aligned with M365 Copilot extension patterns
- ✅ Improved error handling and logging
- ✅ Centralized configuration management
- ✅ Better TypeScript types and interfaces
- ✅ Removed obsolete MCP parser code
- ✅ Added health check commands
- ✅ Enhanced chat participant with rich responses
- ✅ Added Azure Portal integration buttons

### Removed (Obsolete)

- ❌ Universal MCP Parser (no longer needed)
- ❌ MCP Client (replaced with direct API integration)
- ❌ Legacy command handlers
- ❌ Test commands and debug utilities

---

## 📧 Support

For issues and feature requests, please use the GitHub repository issue tracker.

---

**Built with ❤️ by the Azure NoOps Team**
