# Quick Reference: Building & Installing the Extension

## Prerequisites

- Node.js 18+ and npm
- VS Code 1.90.0+
- GitHub Copilot subscription
- MCP Server running (http://localhost:5100)

---

## Quick Start

```bash
# Navigate to extension directory
cd /Users/johnspinella/repos/platform-engineering-copilot/extensions/platform-engineering-copilot-github

# Install dependencies
npm install

# Compile TypeScript
npm run compile

# Package as VSIX
npm run package

# Install in VS Code
code --install-extension platform-copilot-github-1.0.0.vsix

# Verify installation
code --list-extensions | grep platform-copilot
```

---

## Development Workflow

### 1. Make Changes

Edit files in `src/`:
- `extension.ts` - Extension activation
- `chatParticipant.ts` - Chat handler
- `services/mcpClient.ts` - MCP server client
- `config.ts` - Configuration management

### 2. Compile & Test

```bash
# Compile TypeScript
npm run compile

# Watch mode (auto-compile on save)
npm run watch

# Open Extension Development Host
# Press F5 in VS Code
# OR
code --extensionDevelopmentPath=.
```

### 3. Test in Development Host

- Extension Development Host window opens
- Open GitHub Copilot Chat
- Test with `@platform <your prompt>`
- Check Output panel: "Platform Engineering Copilot"

### 4. Package & Install

```bash
# Create VSIX package
npm run package

# Install locally
code --install-extension platform-copilot-github-1.0.0.vsix

# Reload VS Code
# Cmd+Shift+P → "Developer: Reload Window"
```

---

## NPM Scripts

| Command | Description |
|---------|-------------|
| `npm install` | Install dependencies |
| `npm run compile` | Compile TypeScript to JavaScript |
| `npm run watch` | Watch mode - auto-compile on changes |
| `npm run lint` | Lint TypeScript code |
| `npm run package` | Create VSIX package |
| `npm run clean` | Remove compiled files |

---

## File Structure

```
platform-engineering-copilot-github/
├── src/
│   ├── extension.ts              # Entry point, activation
│   ├── chatParticipant.ts        # @platform chat handler
│   ├── config.ts                 # Configuration & logging
│   └── services/
│       ├── mcpClient.ts          # MCP server HTTP client
│       └── exportService.ts      # Export/share utilities
├── out/                          # Compiled JavaScript (gitignored)
├── media/                        # Icons and assets
├── package.json                  # Extension manifest
├── tsconfig.json                 # TypeScript config
├── .vscodeignore                 # Files to exclude from VSIX
├── README.md                     # Documentation
├── AGENT-CAPABILITIES.md         # Agent reference
└── QUICKSTART.md                 # This file
```

---

## Common Tasks

### Add New MCP Endpoint

1. **Add method to `mcpClient.ts`:**

```typescript
async myNewOperation(param: string): Promise<McpChatResponse> {
    return this.sendChatMessage(
        `Execute my new operation: ${param}`,
        undefined,
        { operation: 'my-operation', param }
    );
}
```

2. **Use in `chatParticipant.ts`:**

```typescript
if (userMessage.includes('my trigger')) {
    const result = await this.apiClient.myNewOperation(extractedParam);
    // Handle response
}
```

### Add Action Button

In `chatParticipant.ts`:

```typescript
stream.button({
    command: 'vscode.open',           // VS Code command
    title: 'Open Resource',           // Button text
    arguments: [resourceUri]          // Command arguments
});
```

### Add New Configuration Setting

1. **Update `package.json`:**

```json
{
  "configuration": {
    "properties": {
      "platform-copilot.myNewSetting": {
        "type": "string",
        "default": "value",
        "description": "Description of setting"
      }
    }
  }
}
```

2. **Access in code:**

```typescript
import * as vscode from 'vscode';

const config = vscode.workspace.getConfiguration('platform-copilot');
const mySetting = config.get<string>('myNewSetting');
```

---

## Debugging

### Extension Debug

1. Open extension project in VS Code
2. Set breakpoints in `src/` files
3. Press `F5` to launch Extension Development Host
4. Use `@platform` in the dev window
5. Breakpoints will hit in original window

### View Logs

```typescript
// In code
import { config } from './config';
config.log('Debug message');
config.info('Info message');
config.error('Error message', error);
```

View in VS Code:
- `View → Output`
- Select: "Platform Engineering Copilot" from dropdown

### Test MCP Server Directly

```bash
# Test health endpoint
curl http://localhost:5100/health | jq .

# Test chat endpoint
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "List resources in East US",
    "conversationId": "test-123"
  }' | jq .
```

---

## Troubleshooting Build Issues

### TypeScript Compilation Errors

```bash
# Clean and rebuild
npm run clean
rm -rf node_modules package-lock.json
npm install
npm run compile
```

### VSIX Packaging Fails

```bash
# Check vsce is installed
npm install -g @vscode/vsce

# Package with verbose output
vsce package --verbose
```

### Extension Won't Load

1. Check VS Code version: `code --version`
2. Check extension manifest: `package.json` → `engines.vscode`
3. Review extension activation: Output panel → Extensions
4. Check for errors: Developer Tools → Console (`Help → Toggle Developer Tools`)

---

## Testing Checklist

Before releasing:

- [ ] All TypeScript compiles without errors
- [ ] Extension activates successfully
- [ ] `@platform` participant appears in GitHub Copilot Chat
- [ ] Health check works: Settings → Check Platform API Health
- [ ] Test all 7 agent types with sample prompts
- [ ] Code analysis works with open file
- [ ] Repository analysis works
- [ ] Action buttons render correctly
- [ ] Error handling displays helpful messages
- [ ] Settings update applies without reload
- [ ] No errors in Output panel or Developer Tools

---

## Publishing

### Create Release VSIX

```bash
# Update version in package.json
npm version patch  # or minor, major

# Clean build
npm run clean
npm install
npm run compile

# Package
npm run package
# Creates: platform-copilot-github-X.Y.Z.vsix
```

### Install from VSIX

```bash
# Local install
code --install-extension platform-copilot-github-1.0.0.vsix

# Or through VS Code UI
# Extensions panel → ... → Install from VSIX
```

### Publish to Marketplace (Future)

```bash
# Login to publisher account
vsce login YOUR-PUBLISHER-ID

# Publish
vsce publish
```

---

## Quick Commands

```bash
# Full rebuild and install
npm run clean && npm install && npm run compile && npm run package && code --install-extension platform-copilot-github-1.0.0.vsix

# Development mode (watch + debug)
npm run watch &
code --extensionDevelopmentPath=.

# Test extension in separate VS Code instance
code --extensionDevelopmentPath=. --disable-extensions
```

---

## Environment Variables

Create `.env` file (optional):

```bash
MCP_API_URL=http://localhost:5100
MCP_API_KEY=your-api-key-here
LOG_LEVEL=debug
```

Access in code:
```typescript
process.env.MCP_API_URL
```

---

## Resources

- **VS Code Extension API**: https://code.visualstudio.com/api
- **GitHub Copilot Chat API**: https://code.visualstudio.com/api/extension-guides/chat
- **TypeScript**: https://www.typescriptlang.org/docs
- **Axios**: https://axios-http.com/docs/intro
- **MCP Server Docs**: ../../docs/

---

## Support

- GitHub Issues: https://github.com/azurenoops/platform-engineering-copilot/issues
- Main Docs: ../../docs/
- Extension README: ./README.md
- Agent Capabilities: ./AGENT-CAPABILITIES.md

---

**Last Updated:** November 7, 2024
