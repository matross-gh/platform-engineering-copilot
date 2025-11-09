import * as vscode from 'vscode';
import { McpClient } from './services/mcpClient';
import { PlatformChatParticipant } from './chatParticipant';
import { config } from './config';
import { showShareMenu, copyToClipboard, exportReportWithPrompt } from './services/exportService';
import { WorkspaceService } from './services/workspaceService';

let apiClient: McpClient;
let chatParticipant: PlatformChatParticipant;

/**
 * Extension activation
 */
export function activate(context: vscode.ExtensionContext) {
    config.info('🚀 Platform Engineering Copilot extension is activating...');

    try {
        // Validate configuration
        const validation = config.validate();
        if (!validation.valid) {
            config.warn('Configuration issues detected:', validation.errors);
            vscode.window.showWarningMessage(
                `Platform Copilot: ${validation.errors.join(', ')}`
            );
        }

        // Initialize API client
        apiClient = new McpClient();
        config.info('✅ Platform API client initialized');

        // Initialize chat participant
        chatParticipant = new PlatformChatParticipant(context, apiClient);
        context.subscriptions.push(chatParticipant);
        config.info('✅ Chat participant registered');

        // Register commands
        registerCommands(context);

        // Listen for configuration changes
        context.subscriptions.push(
            vscode.workspace.onDidChangeConfiguration(e => {
                if (e.affectsConfiguration('platform-copilot')) {
                    config.reload();
                    apiClient.updateConfig();
                    config.info('Configuration updated');
                }
            })
        );

        // Perform initial health check
        performHealthCheck();

        vscode.window.showInformationMessage('✅ Platform Engineering Copilot activated!');
        config.info('🎉 Extension activation complete');

    } catch (error) {
        config.error('Failed to activate extension:', error);
        vscode.window.showErrorMessage(
            `Platform Copilot activation failed: ${error instanceof Error ? error.message : 'Unknown error'}`
        );
    }
}

/**
 * Extension deactivation
 */
export function deactivate() {
    config.info('👋 Platform Engineering Copilot extension is deactivating...');
}

/**
 * Register all commands
 */
function registerCommands(context: vscode.ExtensionContext) {
    // Health check command
    context.subscriptions.push(
        vscode.commands.registerCommand('platform.checkHealth', async () => {
            const statusBarMessage = vscode.window.setStatusBarMessage('$(sync~spin) Checking Platform API...');
            
            try {
                const health = await apiClient.healthCheck();
                
                if (health.status === 'healthy') {
                    vscode.window.showInformationMessage(
                        `✅ Platform API is healthy\n\n` +
                        `Version: ${health.version || 'Unknown'}\n` +
                        `Server: ${health.server}\n` +
                        `URL: ${config.apiUrl}`
                    );
                } else {
                    vscode.window.showWarningMessage(
                        `⚠️  Platform API health check failed\n\n` +
                        `Status: ${health.status}\n` +
                        `URL: ${config.apiUrl}`
                    );
                }
            } catch (error) {
                vscode.window.showErrorMessage(
                    `❌ Could not reach Platform API\n\n` +
                    `${error instanceof Error ? error.message : 'Unknown error'}\n` +
                    `URL: ${config.apiUrl}`
                );
            } finally {
                statusBarMessage.dispose();
            }
        })
    );

    // Configuration command
    context.subscriptions.push(
        vscode.commands.registerCommand('platform.configure', async () => {
            const action = await vscode.window.showQuickPick([
                {
                    label: '$(globe) Change API URL',
                    description: `Current: ${config.apiUrl}`,
                    action: 'url'
                },
                {
                    label: '$(key) Set API Key',
                    description: config.apiKey ? 'Currently configured' : 'Not configured',
                    action: 'key'
                },
                {
                    label: '$(settings-gear) Open Settings',
                    description: 'Configure all options',
                    action: 'settings'
                },
                {
                    label: '$(pulse) Test Connection',
                    description: 'Verify API connectivity',
                    action: 'test'
                }
            ], {
                placeHolder: 'Configure Platform Engineering Copilot'
            });

            if (!action) {
                return;
            }

            switch (action.action) {
                case 'url':
                    const newUrl = await vscode.window.showInputBox({
                        prompt: 'Enter Platform API URL',
                        value: config.apiUrl,
                        validateInput: (value) => {
                            try {
                                new URL(value);
                                return null;
                            } catch {
                                return 'Please enter a valid URL';
                            }
                        }
                    });
                    if (newUrl) {
                        await vscode.workspace.getConfiguration('platform-copilot').update(
                            'apiUrl',
                            newUrl,
                            vscode.ConfigurationTarget.Global
                        );
                        vscode.window.showInformationMessage(`✅ API URL updated to: ${newUrl}`);
                    }
                    break;

                case 'key':
                    const newKey = await vscode.window.showInputBox({
                        prompt: 'Enter Platform API Key (leave empty to remove)',
                        value: config.apiKey,
                        password: true
                    });
                    if (newKey !== undefined) {
                        await vscode.workspace.getConfiguration('platform-copilot').update(
                            'apiKey',
                            newKey,
                            vscode.ConfigurationTarget.Global
                        );
                        vscode.window.showInformationMessage(
                            newKey ? '✅ API Key configured' : '✅ API Key removed'
                        );
                    }
                    break;

                case 'settings':
                    vscode.commands.executeCommand(
                        'workbench.action.openSettings',
                        'platform-copilot'
                    );
                    break;

                case 'test':
                    vscode.commands.executeCommand('platform.checkHealth');
                    break;
            }
        })
    );

    // Share/Export commands for compliance reports
    context.subscriptions.push(
        vscode.commands.registerCommand('platform-copilot.shareReport', async (content: string) => {
            await showShareMenu(content);
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('platform-copilot.exportReport', async (content: string) => {
            await exportReportWithPrompt(content);
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('platform-copilot.copyToClipboard', async (content: string) => {
            await copyToClipboard(content);
        })
    );

    // Workspace creation commands for infrastructure templates
    const workspaceService = new WorkspaceService();

    context.subscriptions.push(
        vscode.commands.registerCommand('platform-copilot.createWorkspace', async (templates: Map<string, string>, templateType: 'bicep' | 'terraform' | 'kubernetes' | 'arm' | null) => {
            try {
                if (!templates || templates.size === 0) {
                    vscode.window.showWarningMessage('No templates to save');
                    return;
                }

                // Prompt for project name
                const projectName = await vscode.window.showInputBox({
                    prompt: 'Enter project name',
                    placeHolder: 'my-infrastructure-project',
                    validateInput: (value) => {
                        if (!value || value.trim().length === 0) {
                            return 'Project name cannot be empty';
                        }
                        if (!/^[a-zA-Z0-9_-]+$/.test(value)) {
                            return 'Project name can only contain letters, numbers, hyphens, and underscores';
                        }
                        return null;
                    }
                });

                if (!projectName) {
                    return; // User cancelled
                }

                // Create workspace with detected template type
                if (templateType && (templateType === 'bicep' || templateType === 'terraform' || templateType === 'kubernetes')) {
                    await workspaceService.createInfrastructureTemplate(templateType, templates, projectName);
                } else {
                    // Convert Map to FileToCreate array for generic workspace creation
                    const files = Array.from(templates.entries()).map(([fileName, content]) => ({
                        relativePath: fileName,
                        content: content,
                        openAfterCreate: false
                    }));

                    await workspaceService.createWorkspace({
                        projectName: projectName,
                        files: files,
                        createInNewFolder: true,
                        folderName: projectName
                    });
                }

                vscode.window.showInformationMessage(`✅ Created ${projectName} in workspace`);
            } catch (error) {
                vscode.window.showErrorMessage(
                    `Failed to create workspace: ${error instanceof Error ? error.message : 'Unknown error'}`
                );
                config.error('Workspace creation failed:', error);
            }
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('platform-copilot.saveTemplate', async (templates: Map<string, string>) => {
            try {
                if (!templates || templates.size === 0) {
                    vscode.window.showWarningMessage('No templates to save');
                    return;
                }

                // If multiple templates, let user choose which one to save
                let fileName: string;
                let content: string;

                if (templates.size === 1) {
                    const entry = Array.from(templates.entries())[0];
                    if (!entry) {
                        vscode.window.showWarningMessage('No template content found');
                        return;
                    }
                    fileName = entry[0];
                    content = entry[1];
                } else {
                    const selectedFile = await vscode.window.showQuickPick(
                        Array.from(templates.keys()),
                        {
                            placeHolder: 'Select file to save'
                        }
                    );

                    if (!selectedFile) {
                        return; // User cancelled
                    }

                    fileName = selectedFile;
                    content = templates.get(selectedFile) || '';
                }

                await workspaceService.createFile(fileName, content, true);
                vscode.window.showInformationMessage(`✅ Saved ${fileName}`);
            } catch (error) {
                vscode.window.showErrorMessage(
                    `Failed to save template: ${error instanceof Error ? error.message : 'Unknown error'}`
                );
                config.error('Template save failed:', error);
            }
        })
    );

    // Code analysis commands
    context.subscriptions.push(
        vscode.commands.registerCommand('platform.analyzeCurrentFile', async () => {
            const editor = vscode.window.activeTextEditor;
            if (!editor) {
                vscode.window.showWarningMessage('No file is currently open for analysis');
                return;
            }

            const document = editor.document;
            const codeContent = document.getText();
            const fileName = document.fileName;
            
            if (codeContent.trim().length === 0) {
                vscode.window.showWarningMessage('Current file is empty');
                return;
            }

            const statusBarMessage = vscode.window.setStatusBarMessage('$(sync~spin) Analyzing code for compliance...');
            
            try {
                const result = await apiClient.analyzeCodeForCompliance(
                    codeContent,
                    fileName,
                    vscode.workspace.workspaceFolders?.[0]?.uri.toString()
                );

                if (result.success) {
                    // Show results in a new document
                    const resultsDoc = await vscode.workspace.openTextDocument({
                        content: formatComplianceResults(result),
                        language: 'markdown'
                    });
                    await vscode.window.showTextDocument(resultsDoc);
                    
                    vscode.window.showInformationMessage(
                        `✅ Code analysis complete - Risk Level: ${result.riskLevel}`
                    );
                } else {
                    vscode.window.showErrorMessage(
                        `❌ Code analysis failed: ${result.errors.join(', ')}`
                    );
                }
            } catch (error) {
                vscode.window.showErrorMessage(
                    `❌ Code analysis failed: ${error instanceof Error ? error.message : 'Unknown error'}`
                );
            } finally {
                statusBarMessage.dispose();
            }
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('platform.analyzeWorkspace', async () => {
            const workspaceFolder = vscode.workspace.workspaceFolders?.[0];
            if (!workspaceFolder) {
                vscode.window.showWarningMessage('No workspace is open for analysis');
                return;
            }

            // Try to get repository URL from git
            let repositoryUrl = workspaceFolder.uri.toString();
            
            const statusBarMessage = vscode.window.setStatusBarMessage('$(sync~spin) Analyzing workspace for compliance...');
            
            try {
                const result = await apiClient.analyzeRepositoryForCompliance(repositoryUrl);

                if (result.success) {
                    // Show results in a new document
                    const resultsDoc = await vscode.workspace.openTextDocument({
                        content: formatRepositoryComplianceResults(result),
                        language: 'markdown'
                    });
                    await vscode.window.showTextDocument(resultsDoc);
                    
                    vscode.window.showInformationMessage(
                        `✅ Workspace analysis complete - Risk Level: ${result.overallRiskLevel}`
                    );
                } else {
                    vscode.window.showErrorMessage(
                        `❌ Workspace analysis failed: ${result.errors.join(', ')}`
                    );
                }
            } catch (error) {
                vscode.window.showErrorMessage(
                    `❌ Workspace analysis failed: ${error instanceof Error ? error.message : 'Unknown error'}`
                );
            } finally {
                statusBarMessage.dispose();
            }
        })
    );

    config.info('✅ Commands registered');
}

/**
 * Perform initial health check
 */
async function performHealthCheck() {
    try {
        const health = await apiClient.healthCheck();
        if (health.status === 'healthy') {
            config.info(`Platform API is healthy (version: ${health.version || 'unknown'})`);
        } else {
            config.warn('Platform API health check failed. Status:', health.status);
        }
    } catch (error) {
        config.warn('Could not perform initial health check:', error);
    }
}

/**
 * Format code compliance results for display
 */
function formatComplianceResults(result: any): string {
    return `# Code Compliance Analysis Results

## Analysis Summary
- **File**: ${result.fileName || 'Unknown'}
- **Framework**: ${result.framework}
- **Risk Level**: ${result.riskLevel}
- **Analysis ID**: ${result.analysisId}
- **Analyzed At**: ${new Date(result.analyzedAt).toLocaleString()}

## Compliance Report

${result.complianceReport}

## Key Findings

${result.findings.length > 0 ? result.findings.map((f: string) => `- ${f}`).join('\n') : 'No specific findings reported.'}

## Recommendations

${result.recommendations.length > 0 ? result.recommendations.map((r: string) => `- ${r}`).join('\n') : 'No specific recommendations provided.'}

${result.requiresFollowUp ? `\n## Follow-up Required\n\n${result.followUpPrompt}` : ''}

---
*Generated by Platform Engineering Copilot*
`;
}

/**
 * Format repository compliance results for display
 */
function formatRepositoryComplianceResults(result: any): string {
    return `# Repository Compliance Analysis Results

## Analysis Summary
- **Repository**: ${result.repositoryUrl}
- **Branch**: ${result.branch}
- **Framework**: ${result.framework}
- **Overall Risk Level**: ${result.overallRiskLevel}
- **Analysis ID**: ${result.analysisId}
- **Analyzed At**: ${new Date(result.analyzedAt).toLocaleString()}

## Compliance Report

${result.complianceReport}

${result.requiresFollowUp ? `\n## Follow-up Required\n\n${result.followUpPrompt}` : ''}

---
*Generated by Platform Engineering Copilot*
`;
}
