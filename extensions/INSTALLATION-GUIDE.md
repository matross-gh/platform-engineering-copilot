# Platform Copilot Extensions - Installation & Setup Guide

**Created**: October 17, 2025  
**Status**: Ready for Development

---

## 📦 What Was Created

### 1. GitHub Copilot Integration Documentation
**Location**: `/docs/GITHUB-COPILOT-INTEGRATION.md`

Complete guide for integrating Platform Engineering Copilot with GitHub Copilot, including:
- VS Code extension architecture
- Inline code completions
- Chat-based infrastructure management
- Real-time compliance checking
- Cost estimation tooltips

### 2. M365 Copilot Extension Project
**Location**: `/extensions/platform-copilot-m365/`

Full Node.js/TypeScript project for Microsoft 365 Copilot integration with:
- Express.js webhook server
- MCP HTTP client
- Adaptive Cards builder
- Message handler with intent routing
- Teams app manifest
- OpenAPI specification

---

## 🚀 Quick Start

### M365 Copilot Extension

#### 1. Install Dependencies

```bash
cd /Users/johnspinella/repos/platform-engineering-copilot/extensions/platform-copilot-m365

# Install Node.js dependencies
npm install
```

#### 2. Configure Environment

```bash
# Copy example environment file
cp .env.example .env

# Edit .env with your configuration
nano .env
```

Required configuration:
```bash
# MCP HTTP endpoint (legacy env name)
PLATFORM_API_URL=http://localhost:5100
PORT=3978
NODE_ENV=development
```

#### 3. Build the Extension

```bash
npm run build
```

#### 4. Start Development Server

```bash
# Development mode with auto-reload
npm run dev

# Or standard start
npm start
```

You should see:
```
╔════════════════════════════════════════════════════╗
║  Platform Engineering Copilot - M365 Extension     ║
║                                                    ║
║  🚀 Server running on port 3978                    ║
║  🔗 MCP Server: http://localhost:5100              ║
║  🎯 Environment: development                       ║
║                                                    ║
║  📡 Endpoints:                                     ║
║     POST /api/messages  - M365 webhook            ║
║     GET  /health        - Health check            ║
║     GET  /openapi.json  - API specification       ║
║                                                    ║
╚════════════════════════════════════════════════════╝
```

#### 5. Test the Extension

```bash
# Test health endpoint
curl http://localhost:3978/health

# Test message processing
curl -X POST http://localhost:3978/api/messages \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Create a storage account named testdata001",
    "conversation": {"id": "test-001"},
    "from": {"id": "test-user"}
  }'
```

#### 6. Package for Teams

```bash
# Create deployment package
npm run package
```

This creates `dist/platform-copilot-m365.zip` containing:
- manifest.json
- ai-plugin.json
- color.png (you need to add this)
- outline.png (you need to add this)

---

## 🎨 Adding Icons

The Teams app requires two icons:

### Color Icon (192x192px)
```bash
# Create or download your app icon
# Place at: src/appPackage/color.png
# Requirements: 192x192px PNG, represents your brand
```

### Outline Icon (32x32px)
```bash
# Create or download your outline icon
# Place at: src/appPackage/outline.png
# Requirements: 32x32px PNG, transparent background, white foreground
```

You can use tools like:
- [Icon Kitchen](https://icon.kitchen/) - Generate app icons
- Figma/Sketch - Design custom icons
- [Canva](https://www.canva.com/) - Create simple icons

---

## 🔧 Development Workflow

### Running Locally with ngrok

Since M365 Copilot needs a public URL, use ngrok during development:

```bash
# In one terminal, start the extension
npm run dev

# In another terminal, expose it publicly
ngrok http 3978

# Copy the ngrok URL (e.g., https://abc123.ngrok.io)
# Update manifest.json validDomains to include your ngrok URL
```

### Hot Reload

The extension uses `nodemon` for automatic reloading:

```bash
npm run dev

# Make changes to src/**/*.ts
# Server automatically restarts
```

### Testing

```bash
# Run tests
npm test

# Run tests with coverage
npm test -- --coverage

# Run linting
npm run lint

# Format code
npm run format
```

---

## 📤 Deploying to Teams

### Option 1: Upload Custom App (Development)

1. Build and package the extension:
   ```bash
   npm run build
   npm run package
   ```

2. Open Microsoft Teams

3. Go to **Apps** → **Manage your apps** → **Upload an app**

4. Select **Upload a custom app**

5. Choose `dist/platform-copilot-m365.zip`

6. Add to your team or personal space

### Option 2: Teams Admin Center (Production)

1. Build production package:
   ```bash
   NODE_ENV=production npm run build
   npm run package
   ```

2. Go to [Teams Admin Center](https://admin.teams.microsoft.com/)

3. Navigate to **Teams apps** → **Manage apps**

4. Click **Upload** and select your `.zip` file

5. Configure availability and permissions

6. Submit for approval

---

## 🌐 Deploying to Azure

### Deploy M365 Extension to Azure App Service

```bash
# Login to Azure
az login

# Create resource group (if needed)
az group create \
  --name rg-platform-copilot \
  --location usgovvirginia

# Create App Service Plan
az appservice plan create \
  --name asp-platform-copilot-m365 \
  --resource-group rg-platform-copilot \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name platform-copilot-m365 \
  --resource-group rg-platform-copilot \
  --plan asp-platform-copilot-m365 \
  --runtime "NODE:18-lts"

# Configure environment variables
az webapp config appsettings set \
  --name platform-copilot-m365 \
  --resource-group rg-platform-copilot \
  --settings \
  PLATFORM_API_URL="https://mcp.yourdomain.com" \
    NODE_ENV="production" \
    PORT="8080"

# Deploy code
npm run build
cd dist
zip -r deploy.zip *
az webapp deployment source config-zip \
  --name platform-copilot-m365 \
  --resource-group rg-platform-copilot \
  --src deploy.zip

# Your app is now at: https://platform-copilot-m365.azurewebsites.us
```

### Update Teams Manifest

After deployment, update `src/appPackage/manifest.json`:

```json
"validDomains": [
  "platform-copilot-m365.azurewebsites.us"
]
```

Then repackage and re-upload to Teams.

---

## 🧪 Testing the Integration

### Test in Microsoft Teams

1. Open Microsoft Teams

2. Start a chat with Copilot

3. Type: `@Platform Copilot create a storage account named testdata001`

4. You should receive an adaptive card response

### Expected Responses

**Incomplete Request (Follow-up):**
```
❓ Additional Information Needed

I'll help you create a storage account. I need a few details:

Missing Information:
1. Resource group name
2. Azure region
3. Redundancy type (LRS, GRS, etc.)
```

**Successful Creation:**
```
✅ Infrastructure Operation Complete

Storage account 'testdata001' has been created successfully.

Resource Details:
- Name: testdata001
- Resource Group: rg-ml-sbx-jrs
- Location: usgovvirginia
- Status: Succeeded

[View in Azure Portal]
```

---

## 📊 Monitoring

### Application Insights (Recommended)

Add Application Insights to track usage:

```bash
# Install package
npm install applicationinsights

# Add to src/index.ts
import * as appInsights from 'applicationinsights';
appInsights.setup('YOUR_INSTRUMENTATION_KEY').start();
```

### Logs

View logs in development:
```bash
# Real-time logs
npm run dev
```

View logs in Azure:
```bash
# Stream logs from Azure
az webapp log tail \
  --name platform-copilot-m365 \
  --resource-group rg-platform-copilot
```

---

## 🔒 Security Checklist

Before deploying to production:

- [ ] Replace placeholder Azure AD App ID in manifest.json
- [ ] Configure proper OAuth authentication
- [ ] Set up API key authentication for MCP server
- [ ] Enable HTTPS only
- [ ] Configure CORS policies
- [ ] Add rate limiting
- [ ] Enable Application Insights
- [ ] Set up Azure Key Vault for secrets
- [ ] Review and restrict permissions
- [ ] Add input validation and sanitization

---

## 🐛 Troubleshooting

### Extension not responding

**Problem**: M365 Copilot doesn't respond to commands

**Solutions**:
1. Check MCP server is running: `curl http://localhost:5100/health`
2. Verify M365 extension is running: `curl http://localhost:3978/health`
3. Check ngrok tunnel is active (for local development)
4. Review logs for errors

### "Cannot find module" errors

**Problem**: TypeScript compilation errors about missing modules

**Solution**:
```bash
# Install all dependencies
npm install

# Rebuild
npm run build
```

### Teams app won't install

**Problem**: Error uploading ZIP to Teams

**Solutions**:
1. Ensure icons are present in `src/appPackage/`
2. Validate manifest.json format
3. Check file size (must be < 20MB)
4. Verify you have permissions to upload custom apps

### Adaptive cards not displaying

**Problem**: Text appears but no formatted card

**Solution**:
- Check card schema version (must be 1.5 or lower)
- Validate JSON structure
- Review Teams adaptive card documentation

---

## 📚 Next Steps

1. **Add Icons**: Create `color.png` and `outline.png`
2. **Test Locally**: Run extension and test with curl
3. **Deploy to Azure**: Follow Azure deployment steps
4. **Upload to Teams**: Package and upload to Teams
5. **User Testing**: Have team members test the integration
6. **Documentation**: Update internal docs with usage examples
7. **Monitoring**: Set up Application Insights

---

## 🎯 Project Structure Reference

```
platform-copilot-m365/
├── package.json              # Node.js dependencies and scripts
├── tsconfig.json             # TypeScript configuration
├── .env.example              # Example environment variables
├── .gitignore                # Git ignore patterns
├── README.md                 # Project documentation
├── src/
│   ├── index.ts              # Main server entry point
│   ├── config.ts             # Configuration management
│   ├── services/
│   │   ├── platformApiClient.ts      # MCP HTTP client (legacy name)
│   │   ├── adaptiveCardBuilder.ts    # Builds Teams cards
│   │   └── messageHandler.ts         # Message processing
│   ├── appPackage/
│   │   ├── manifest.json     # Teams app manifest
│   │   ├── ai-plugin.json    # M365 Copilot plugin
│   │   ├── color.png         # App icon (192x192) - TO ADD
│   │   └── outline.png       # Outline icon (32x32) - TO ADD
│   └── openapi/
│       └── openapi.yaml      # API specification
└── dist/                     # Build output (generated)
```

---

## 🤝 Getting Help

- **Documentation**: See `/docs/M365-COPILOT-INTEGRATION.md` for detailed guide
- **GitHub Issues**: Report bugs on GitHub repository
- **Email**: support@azurenoops.org

---

**Status**: ✅ Project scaffolding complete and ready for development!

**Version**: 1.0.0  
**Last Updated**: October 17, 2025
