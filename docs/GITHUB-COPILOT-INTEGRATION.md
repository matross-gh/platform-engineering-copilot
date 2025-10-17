# Integrating Platform Engineering Copilot with GitHub Copilot

**Last Updated**: October 17, 2025  
**Status**: Implementation Guide

---

## Overview

This guide explains how to integrate the Platform Engineering Copilot with GitHub Copilot, enabling developers to manage Azure infrastructure directly from their IDE (VS Code, Visual Studio, JetBrains) using natural language comments and inline suggestions.

---

## 🎯 Integration Architecture

```
GitHub Copilot (IDE)
    ↓
GitHub Copilot Extension API
    ↓
Platform Engineering Copilot Agent
    ↓
Platform Engineering Copilot API (:7001)
    ↓
Azure Resources
```

---

## 🚀 Quick Start

### Prerequisites

1. **GitHub Copilot License** (Individual, Business, or Enterprise)
2. **VS Code** with GitHub Copilot extensions installed
3. **Node.js 18+** and npm
4. **Platform Engineering Copilot API** running (http://localhost:7001 or deployed)
5. **GitHub App** (for authentication)

### Installation

```bash
# Install GitHub Copilot in VS Code
code --install-extension GitHub.copilot
code --install-extension GitHub.copilot-chat

# Navigate to extensions folder
cd /Users/johnspinella/repos/platform-engineering-copilot/extensions

# Install the extension
cd platform-copilot-github-extension
npm install
npm run build
```

---

## 📦 Extension Features

### 1. **Inline Infrastructure Suggestions**

As you write comments, get instant infrastructure-as-code suggestions:

```python
# Create a storage account named mldata001 in resource group rg-ml-sbx
# [GitHub Copilot suggests Bicep/Terraform code]
```

### 2. **Chat-Based Infrastructure Management**

Open GitHub Copilot Chat (Ctrl+Shift+I) and use `@platform` agent:

```
@platform create a storage account for my ML project in usgovvirginia

@platform run compliance scan on my production subscription

@platform estimate cost for deploying AKS cluster with 3 nodes

@platform show me all resources in rg-ml-sbx-jrs
```

### 3. **Context-Aware Code Generation**

The extension reads your current workspace and provides context-aware suggestions:

- Detects existing Bicep/Terraform files
- Reads `azuredeploy.json` for current infrastructure
- Analyzes `.azure/` configuration
- Suggests consistent naming conventions

### 4. **Compliance Checks in IDE**

Get real-time compliance feedback as you write infrastructure code:

```bicep
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'mystorageaccount'  // ❌ Copilot: Name doesn't follow naming convention
  properties: {
    minimumTlsVersion: 'TLS1_0'  // ⚠️ Copilot: Use TLS 1.2+ for compliance
    allowBlobPublicAccess: true  // ❌ Copilot: Public access violates security policy
  }
}
```

### 5. **Cost Estimation Tooltips**

Hover over resource definitions to see estimated monthly costs:

```bicep
resource aksCluster 'Microsoft.ContainerService/managedClusters@2023-10-01' = {
  // 💰 Estimated cost: $350-500/month (Standard_D2s_v3, 3 nodes)
  ...
}
```

---

## 🛠️ Extension Development

### Project Structure

```
extensions/platform-copilot-github-extension/
├── package.json
├── tsconfig.json
├── README.md
├── .vscodeignore
├── src/
│   ├── extension.ts           # Main extension entry point
│   ├── agent.ts                # GitHub Copilot Agent implementation
│   ├── apiClient.ts            # Platform Copilot API client
│   ├── codeAnalyzer.ts         # Infrastructure code analysis
│   ├── costEstimator.ts        # Real-time cost estimation
│   ├── complianceChecker.ts    # In-IDE compliance validation
│   └── contextProvider.ts      # Workspace context extraction
├── test/
│   └── extension.test.ts
└── assets/
    ├── icon.png
    └── logo.png
```

### Core Implementation

#### 1. Extension Entry Point (`src/extension.ts`)

```typescript
import * as vscode from 'vscode';
import { PlatformCopilotAgent } from './agent';
import { PlatformApiClient } from './apiClient';

export function activate(context: vscode.ExtensionContext) {
    console.log('Platform Engineering Copilot extension activated');

    // Initialize API client
    const apiClient = new PlatformApiClient(
        vscode.workspace.getConfiguration('platformCopilot').get('apiUrl') || 'http://localhost:7001'
    );

    // Register GitHub Copilot Agent
    const agent = new PlatformCopilotAgent(apiClient);
    context.subscriptions.push(agent);

    // Register commands
    context.subscriptions.push(
        vscode.commands.registerCommand('platformCopilot.provisionInfrastructure', async () => {
            const query = await vscode.window.showInputBox({
                prompt: 'What infrastructure do you want to provision?',
                placeHolder: 'e.g., Create a storage account in rg-prod'
            });
            
            if (query) {
                await agent.handleQuery(query);
            }
        })
    );

    // Register inline completion provider
    const inlineProvider = vscode.languages.registerInlineCompletionItemProvider(
        ['bicep', 'terraform', 'yaml', 'json'],
        {
            async provideInlineCompletionItems(document, position, context, token) {
                const currentLine = document.lineAt(position.line).text;
                
                // Check if it's an infrastructure comment
                if (currentLine.trim().startsWith('//') || currentLine.trim().startsWith('#')) {
                    return await agent.getInlineCompletions(document, position);
                }
                
                return undefined;
            }
        }
    );
    
    context.subscriptions.push(inlineProvider);

    // Register hover provider for cost estimates
    const hoverProvider = vscode.languages.registerHoverProvider(
        ['bicep', 'terraform'],
        {
            async provideHover(document, position, token) {
                const wordRange = document.getWordRangeAtPosition(position);
                const word = document.getText(wordRange);
                
                // Check if hovering over a resource
                if (word === 'resource') {
                    return await agent.getCostEstimate(document, position);
                }
                
                return undefined;
            }
        }
    );
    
    context.subscriptions.push(hoverProvider);

    // Register code actions for compliance fixes
    const codeActionProvider = vscode.languages.registerCodeActionsProvider(
        ['bicep', 'terraform'],
        {
            async provideCodeActions(document, range, context, token) {
                const diagnostics = context.diagnostics;
                const actions: vscode.CodeAction[] = [];
                
                for (const diagnostic of diagnostics) {
                    if (diagnostic.source === 'platformCopilot') {
                        const fix = new vscode.CodeAction(
                            `Fix: ${diagnostic.message}`,
                            vscode.CodeActionKind.QuickFix
                        );
                        fix.diagnostics = [diagnostic];
                        fix.command = {
                            command: 'platformCopilot.applyComplianceFix',
                            title: 'Apply Compliance Fix',
                            arguments: [document, diagnostic]
                        };
                        actions.push(fix);
                    }
                }
                
                return actions;
            }
        }
    );
    
    context.subscriptions.push(codeActionProvider);
}

export function deactivate() {
    console.log('Platform Engineering Copilot extension deactivated');
}
```

#### 2. GitHub Copilot Agent (`src/agent.ts`)

```typescript
import * as vscode from 'vscode';
import { PlatformApiClient } from './apiClient';

export class PlatformCopilotAgent implements vscode.Disposable {
    private readonly apiClient: PlatformApiClient;
    private conversationId: string;

    constructor(apiClient: PlatformApiClient) {
        this.apiClient = apiClient;
        this.conversationId = this.generateConversationId();
    }

    async handleQuery(query: string): Promise<void> {
        try {
            vscode.window.withProgress({
                location: vscode.ProgressLocation.Notification,
                title: 'Platform Copilot',
                cancellable: true
            }, async (progress, token) => {
                progress.report({ message: 'Processing request...' });

                const response = await this.apiClient.sendQuery(query, this.conversationId);

                if (response.requiresFollowUp) {
                    // Show follow-up question
                    const answer = await vscode.window.showInputBox({
                        prompt: response.followUpPrompt,
                        placeHolder: response.missingFields?.join(', ')
                    });

                    if (answer) {
                        await this.handleQuery(answer);
                    }
                } else {
                    // Show result
                    this.showResult(response);
                }
            });
        } catch (error) {
            vscode.window.showErrorMessage(`Platform Copilot Error: ${error.message}`);
        }
    }

    async getInlineCompletions(
        document: vscode.TextDocument,
        position: vscode.Position
    ): Promise<vscode.InlineCompletionItem[]> {
        const currentLine = document.lineAt(position.line).text;
        const comment = this.extractComment(currentLine);

        if (!comment) {
            return [];
        }

        try {
            const response = await this.apiClient.getCodeSuggestion(comment, {
                language: document.languageId,
                existingCode: document.getText(),
                fileName: document.fileName
            });

            if (response.suggestions && response.suggestions.length > 0) {
                return response.suggestions.map(suggestion => ({
                    insertText: suggestion.code,
                    range: new vscode.Range(position, position)
                }));
            }
        } catch (error) {
            console.error('Failed to get inline completions:', error);
        }

        return [];
    }

    async getCostEstimate(
        document: vscode.TextDocument,
        position: vscode.Position
    ): Promise<vscode.Hover | undefined> {
        // Extract resource definition
        const resourceDefinition = this.extractResourceDefinition(document, position);
        
        if (!resourceDefinition) {
            return undefined;
        }

        try {
            const estimate = await this.apiClient.estimateCost(resourceDefinition);

            const markdown = new vscode.MarkdownString();
            markdown.appendMarkdown(`### 💰 Cost Estimate\n\n`);
            markdown.appendMarkdown(`**Monthly**: $${estimate.monthlyEstimate.toFixed(2)}\n\n`);
            markdown.appendMarkdown(`**Annually**: $${estimate.annualEstimate.toFixed(2)}\n\n`);
            
            if (estimate.breakdown) {
                markdown.appendMarkdown(`#### Breakdown:\n`);
                for (const [component, cost] of Object.entries(estimate.breakdown)) {
                    markdown.appendMarkdown(`- ${component}: $${cost}/month\n`);
                }
            }

            return new vscode.Hover(markdown);
        } catch (error) {
            console.error('Failed to get cost estimate:', error);
            return undefined;
        }
    }

    private showResult(response: any): void {
        const panel = vscode.window.createWebviewPanel(
            'platformCopilotResult',
            'Platform Copilot Result',
            vscode.ViewColumn.Beside,
            {
                enableScripts: true
            }
        );

        panel.webview.html = this.getResultHtml(response);
    }

    private getResultHtml(response: any): string {
        return `
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Platform Copilot Result</title>
                <style>
                    body { 
                        font-family: var(--vscode-font-family); 
                        padding: 20px;
                        color: var(--vscode-foreground);
                        background-color: var(--vscode-editor-background);
                    }
                    .result { 
                        margin: 20px 0; 
                        padding: 15px;
                        background: var(--vscode-editor-inactiveSelectionBackground);
                        border-radius: 5px;
                    }
                    .success { border-left: 4px solid #4caf50; }
                    .error { border-left: 4px solid #f44336; }
                    pre { 
                        background: var(--vscode-textCodeBlock-background); 
                        padding: 10px; 
                        border-radius: 3px;
                        overflow-x: auto;
                    }
                    .metadata { 
                        font-size: 0.9em; 
                        color: var(--vscode-descriptionForeground);
                        margin-top: 10px;
                    }
                </style>
            </head>
            <body>
                <h1>🤖 Platform Copilot Result</h1>
                <div class="result ${response.success ? 'success' : 'error'}">
                    <h2>${response.intentType || 'Response'}</h2>
                    <p>${response.response}</p>
                    ${response.generatedCode ? `
                        <h3>Generated Code:</h3>
                        <pre><code>${this.escapeHtml(response.generatedCode)}</code></pre>
                    ` : ''}
                    ${response.resourceId ? `
                        <div class="metadata">
                            <strong>Resource ID:</strong> ${response.resourceId}
                        </div>
                    ` : ''}
                </div>
            </body>
            </html>
        `;
    }

    private extractComment(line: string): string | null {
        const commentMatch = line.match(/(?:\/\/|#)\s*(.+)/);
        return commentMatch ? commentMatch[1].trim() : null;
    }

    private extractResourceDefinition(document: vscode.TextDocument, position: vscode.Position): any {
        // Simple parser - extract resource block
        let startLine = position.line;
        let endLine = position.line;

        // Find start of resource block
        while (startLine > 0) {
            const line = document.lineAt(startLine).text;
            if (line.includes('resource ') || line.includes('resource \'')) {
                break;
            }
            startLine--;
        }

        // Find end of resource block
        while (endLine < document.lineCount - 1) {
            const line = document.lineAt(endLine).text;
            if (line.includes('}') && !line.includes('{')) {
                break;
            }
            endLine++;
        }

        const resourceText = document.getText(new vscode.Range(startLine, 0, endLine, 1000));
        return { code: resourceText, language: document.languageId };
    }

    private escapeHtml(text: string): string {
        return text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    private generateConversationId(): string {
        return `github-copilot-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    dispose(): void {
        // Cleanup
    }
}
```

#### 3. API Client (`src/apiClient.ts`)

```typescript
import axios, { AxiosInstance } from 'axios';

export interface IntelligentChatResponse {
    response: string;
    intentType: string;
    toolExecuted: boolean;
    requiresFollowUp: boolean;
    followUpPrompt?: string;
    missingFields?: string[];
    generatedCode?: string;
    resourceId?: string;
    success: boolean;
}

export class PlatformApiClient {
    private readonly client: AxiosInstance;

    constructor(baseUrl: string) {
        this.client = axios.create({
            baseURL: baseUrl,
            timeout: 30000,
            headers: {
                'Content-Type': 'application/json'
            }
        });
    }

    async sendQuery(message: string, conversationId: string): Promise<IntelligentChatResponse> {
        const response = await this.client.post('/api/chat/intelligent-query', {
            message,
            conversationId,
            context: {
                source: 'github-copilot',
                ide: 'vscode'
            }
        });

        return response.data;
    }

    async getCodeSuggestion(comment: string, context: any): Promise<any> {
        const response = await this.client.post('/api/chat/intelligent-query', {
            message: `Generate ${context.language} code for: ${comment}`,
            conversationId: `inline-${Date.now()}`,
            context: {
                source: 'github-copilot-inline',
                language: context.language,
                existingCode: context.existingCode
            }
        });

        return {
            suggestions: response.data.generatedCode ? [
                { code: response.data.generatedCode }
            ] : []
        };
    }

    async estimateCost(resourceDefinition: any): Promise<any> {
        const response = await this.client.post('/api/cost/estimate', {
            resourceDefinition
        });

        return response.data;
    }

    async checkCompliance(code: string, language: string): Promise<any> {
        const response = await this.client.post('/api/compliance/validate-code', {
            code,
            language
        });

        return response.data;
    }
}
```

---

## 📦 Package Configuration

### `package.json`

```json
{
  "name": "platform-copilot-github-extension",
  "displayName": "Platform Engineering Copilot",
  "description": "AI-powered Azure infrastructure management with GitHub Copilot integration",
  "version": "1.0.0",
  "publisher": "azurenoops",
  "engines": {
    "vscode": "^1.85.0"
  },
  "categories": [
    "Azure",
    "Programming Languages",
    "Machine Learning"
  ],
  "keywords": [
    "azure",
    "infrastructure",
    "iac",
    "bicep",
    "terraform",
    "copilot",
    "ai"
  ],
  "activationEvents": [
    "onLanguage:bicep",
    "onLanguage:terraform",
    "onCommand:platformCopilot.provisionInfrastructure"
  ],
  "main": "./out/extension.js",
  "contributes": {
    "commands": [
      {
        "command": "platformCopilot.provisionInfrastructure",
        "title": "Platform Copilot: Provision Infrastructure",
        "category": "Platform"
      },
      {
        "command": "platformCopilot.runComplianceScan",
        "title": "Platform Copilot: Run Compliance Scan",
        "category": "Platform"
      },
      {
        "command": "platformCopilot.estimateCost",
        "title": "Platform Copilot: Estimate Cost",
        "category": "Platform"
      }
    ],
    "configuration": {
      "title": "Platform Engineering Copilot",
      "properties": {
        "platformCopilot.apiUrl": {
          "type": "string",
          "default": "http://localhost:7001",
          "description": "Platform Copilot API URL"
        },
        "platformCopilot.enableInlineCompletions": {
          "type": "boolean",
          "default": true,
          "description": "Enable inline infrastructure code completions"
        },
        "platformCopilot.enableCostEstimates": {
          "type": "boolean",
          "default": true,
          "description": "Show cost estimates on hover"
        },
        "platformCopilot.enableComplianceChecks": {
          "type": "boolean",
          "default": true,
          "description": "Enable real-time compliance checking"
        }
      }
    },
    "keybindings": [
      {
        "command": "platformCopilot.provisionInfrastructure",
        "key": "ctrl+shift+p",
        "mac": "cmd+shift+p",
        "when": "editorTextFocus"
      }
    ]
  },
  "scripts": {
    "vscode:prepublish": "npm run compile",
    "compile": "tsc -p ./",
    "watch": "tsc -watch -p ./",
    "pretest": "npm run compile && npm run lint",
    "lint": "eslint src --ext ts",
    "test": "node ./out/test/runTest.js",
    "package": "vsce package",
    "publish": "vsce publish"
  },
  "devDependencies": {
    "@types/node": "^20.0.0",
    "@types/vscode": "^1.85.0",
    "@typescript-eslint/eslint-plugin": "^6.0.0",
    "@typescript-eslint/parser": "^6.0.0",
    "eslint": "^8.0.0",
    "typescript": "^5.0.0",
    "@vscode/test-electron": "^2.3.0",
    "@vscode/vsce": "^2.22.0"
  },
  "dependencies": {
    "axios": "^1.6.0"
  }
}
```

---

## 🧪 Testing the Extension

### 1. **Local Development**

```bash
# Open extension in VS Code
code /Users/johnspinella/repos/platform-engineering-copilot/extensions/platform-copilot-github-extension

# Press F5 to launch Extension Development Host

# In the new window, open a Bicep file and try:
# - Type a comment: "// Create storage account"
# - Use command palette: "Platform Copilot: Provision Infrastructure"
```

### 2. **Test Inline Completions**

Create a test Bicep file:

```bicep
// Create a storage account named testdata001 in resource group rg-dev with LRS redundancy
// [Wait for suggestion to appear]
```

### 3. **Test Chat Integration**

Open GitHub Copilot Chat and type:

```
@platform create an AKS cluster with 3 nodes in usgovvirginia
```

---

## 🚀 Publishing

### Publish to VS Code Marketplace

```bash
# Install VSCE
npm install -g @vscode/vsce

# Package extension
cd extensions/platform-copilot-github-extension
vsce package

# Publish (requires publisher account)
vsce publish
```

### Publish to GitHub Copilot Extensions Marketplace

1. Create GitHub App at https://github.com/settings/apps
2. Configure webhook URL: `https://your-api.com/github/webhook`
3. Submit for review at https://github.com/marketplace

---

## 🎯 Usage Examples

### Example 1: Provision Storage Account

**In VS Code**:
```bicep
// Create a storage account for ML data in usgovvirginia with geo-redundancy
```

**GitHub Copilot suggests**:
```bicep
resource mlStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'stmldata${uniqueString(resourceGroup().id)}'
  location: 'usgovvirginia'
  sku: {
    name: 'Standard_GRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    encryption: {
      services: {
        blob: { enabled: true }
        file: { enabled: true }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}
```

### Example 2: Compliance Check

**In Chat**:
```
@platform check this Bicep file for NIST 800-53 compliance
```

**Platform Copilot responds**:
```
✅ Compliance Check Results:

PASS (12/15 controls)

❌ Failed Controls:
- AC-2: Public access enabled on storage account (line 45)
- SC-8: TLS 1.0 allowed, must be 1.2+ (line 67)
- SI-7: Missing diagnostic settings for audit logs (line 89)

💡 Suggested Fixes:
1. Set allowBlobPublicAccess: false
2. Set minimumTlsVersion: 'TLS1_2'
3. Add diagnostic settings resource

Would you like me to apply these fixes automatically?
```

---

## 📊 Analytics & Monitoring

Track extension usage with Application Insights:

```typescript
import { TelemetryReporter } from '@vscode/extension-telemetry';

const reporter = new TelemetryReporter(
    'YOUR_INSTRUMENTATION_KEY'
);

reporter.sendTelemetryEvent('inlineCompletion', {
    language: 'bicep',
    success: 'true'
});
```

---

## 🔒 Security Considerations

1. **API Authentication**: Use OAuth or API keys for Platform Copilot API
2. **Sensitive Data**: Never log subscription IDs or credentials
3. **Rate Limiting**: Implement client-side throttling
4. **Code Validation**: Sanitize all user inputs before sending to API

---

## 🎓 Best Practices

### For Extension Users

1. **Use descriptive comments** for better code generation
2. **Review generated code** before applying
3. **Test in dev environments** first
4. **Keep extension updated** for latest features

### For Extension Developers

1. **Cache API responses** to reduce latency
2. **Implement retry logic** for failed requests
3. **Provide clear error messages** to users
4. **Test with various file types** (Bicep, Terraform, ARM)

---

## 📚 Resources

- [VS Code Extension API](https://code.visualstudio.com/api)
- [GitHub Copilot Extensions](https://docs.github.com/en/copilot/building-copilot-extensions)
- [Language Server Protocol](https://microsoft.github.io/language-server-protocol/)
- [Azure Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)

---

## 🎯 Success Metrics

- **Adoption**: % of developers using the extension
- **Time Saved**: Average time to provision infrastructure
- **Code Quality**: Compliance pass rate for generated code
- **User Satisfaction**: Extension ratings and feedback

---

**Status**: Ready for implementation  
**Next Steps**:
1. Set up extension project structure
2. Implement core agent functionality
3. Test with GitHub Copilot
4. Publish to marketplace
