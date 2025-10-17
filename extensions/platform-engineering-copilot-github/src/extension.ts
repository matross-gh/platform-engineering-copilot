import * as vscode from 'vscode';
import { PlatformApiClient } from './services/platformApiClient';
import { PlatformChatParticipant } from './chatParticipant';
import { config } from './config';

let apiClient: PlatformApiClient;
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
        apiClient = new PlatformApiClient();
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
                
                if (health.healthy) {
                    vscode.window.showInformationMessage(
                        `✅ Platform API is healthy\n\n` +
                        `Version: ${health.version || 'Unknown'}\n` +
                        `URL: ${config.apiUrl}`
                    );
                } else {
                    vscode.window.showWarningMessage(
                        `⚠️  Platform API health check failed\n\n` +
                        `${health.message}\n` +
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

    config.info('✅ Commands registered');
}

/**
 * Perform initial health check
 */
async function performHealthCheck() {
    try {
        const health = await apiClient.healthCheck();
        if (health.healthy) {
            config.info(`Platform API is healthy (version: ${health.version || 'unknown'})`);
        } else {
            config.warn('Platform API health check failed:', health.message);
        }
    } catch (error) {
        config.warn('Could not perform initial health check:', error);
    }
}
