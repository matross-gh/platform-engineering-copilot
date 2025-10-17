import * as vscode from 'vscode';
import { PlatformApiClient, IntelligentChatResponse } from './services/platformApiClient';
import { config } from './config';

/**
 * Platform Chat Participant Handler
 * Integrates Platform Engineering Copilot with GitHub Copilot Chat
 */
export class PlatformChatParticipant implements vscode.Disposable {
    private participant: vscode.ChatParticipant;
    private conversationHistory: Map<string, string[]> = new Map();

    constructor(
        private context: vscode.ExtensionContext,
        private apiClient: PlatformApiClient
    ) {
        // Create chat participant
        this.participant = vscode.chat.createChatParticipant(
            'platform',
            this.handleChatRequest.bind(this)
        );

        // Set metadata
        this.participant.iconPath = vscode.Uri.joinPath(
            context.extensionUri,
            'media',
            'icon.png'
        );

        config.info('Platform chat participant initialized');
    }

    async handleChatRequest(
        request: vscode.ChatRequest,
        chatContext: vscode.ChatContext,
        stream: vscode.ChatResponseStream,
        token: vscode.CancellationToken
    ): Promise<void> {
        try {
            const userMessage = request.prompt.trim();
            const conversationId = this.getConversationId(chatContext);

            config.log(`Chat request: "${userMessage}"`);

            // Add thinking indicator
            stream.progress('Analyzing your request...');

            // Send to Platform API
            const response = await this.apiClient.sendIntelligentQuery({
                message: userMessage,
                conversationId,
                context: {
                    source: 'github-copilot-chat',
                    vscodeVersion: vscode.version,
                    hasWorkspace: !!vscode.workspace.workspaceFolders
                }
            });

            // Handle response based on type
            if (response.requiresFollowUp) {
                await this.handleFollowUpResponse(response, stream);
            } else {
                await this.handleSuccessResponse(response, stream);
            }

            // Store in conversation history
            this.updateConversationHistory(conversationId, userMessage, response.response);

        } catch (error) {
            config.error('Chat request error:', error);
            
            const errorMessage = error instanceof Error ? error.message : 'Unknown error';
            stream.markdown(`## ❌ Error\n\n${errorMessage}\n\n`);
            
            if (errorMessage.includes('Could not reach Platform API')) {
                stream.markdown('**Troubleshooting:**\n');
                stream.markdown('1. Ensure Platform Copilot API is running\n');
                stream.markdown('2. Check the API URL in settings: `platform-copilot.apiUrl`\n');
                stream.markdown('3. Run command: **Platform Copilot: Check Platform API Health**\n');
            }
        }
    }

    private async handleFollowUpResponse(
        response: IntelligentChatResponse,
        stream: vscode.ChatResponseStream
    ): Promise<void> {
        stream.markdown('## ❓ Additional Information Needed\n\n');
        stream.markdown(`${response.followUpPrompt}\n\n`);

        if (response.missingFields && response.missingFields.length > 0) {
            stream.markdown('**Missing Information:**\n');
            response.missingFields.forEach((field, index) => {
                stream.markdown(`${index + 1}. ${field}\n`);
            });
            stream.markdown('\n');
        }

        if (response.quickReplies && response.quickReplies.length > 0) {
            stream.markdown('**Quick Replies:**\n');
            response.quickReplies.forEach(reply => {
                stream.markdown(`- ${reply}\n`);
            });
        }
    }

    private async handleSuccessResponse(
        response: IntelligentChatResponse,
        stream: vscode.ChatResponseStream
    ): Promise<void> {
        // Determine icon based on intent type
        let icon = '✅';
        if (response.intentType === 'compliance') {
            icon = '🔒';
        } else if (response.intentType === 'cost') {
            icon = '💰';
        } else if (response.intentType === 'provisioning' || response.intentType === 'infrastructure') {
            icon = '🏗️';
        } else if (response.intentType === 'deployment') {
            icon = '🚀';
        }

        stream.markdown(`## ${icon} ${this.getIntentTitle(response.intentType)}\n\n`);
        stream.markdown(`${response.response}\n\n`);

        // Add metadata if available
        if (response.metadata) {
            await this.renderMetadata(response.metadata, stream);
        }

        // Add action buttons if applicable
        if (response.metadata?.resourceId) {
            const portalUrl = this.getAzurePortalUrl(response.metadata.resourceId);
            stream.button({
                command: 'vscode.open',
                title: 'View in Azure Portal',
                arguments: [vscode.Uri.parse(portalUrl)]
            });
        }
    }

    private async renderMetadata(
        metadata: Record<string, any>,
        stream: vscode.ChatResponseStream
    ): Promise<void> {
        // Render cost information
        if (metadata.cost !== undefined) {
            stream.markdown(`**Estimated Cost:** $${metadata.cost.toFixed(2)}/month\n\n`);
        }

        // Render compliance score
        if (metadata.complianceScore !== undefined) {
            const score = Math.round(metadata.complianceScore);
            const emoji = score >= 90 ? '🟢' : score >= 70 ? '🟡' : '🔴';
            stream.markdown(`**Compliance Score:** ${emoji} ${score}%\n\n`);
        }

        // Render resource details
        if (metadata.resourceId) {
            stream.markdown(`**Resource ID:**\n\`\`\`\n${metadata.resourceId}\n\`\`\`\n\n`);
        }

        if (metadata.location) {
            stream.markdown(`**Location:** ${metadata.location}\n\n`);
        }
    }

    private getIntentTitle(intentType?: string): string {
        const titles: Record<string, string> = {
            'provisioning': 'Infrastructure Provisioning',
            'infrastructure': 'Infrastructure Management',
            'compliance': 'Compliance Assessment',
            'cost': 'Cost Analysis',
            'deployment': 'Deployment',
            'resource_discovery': 'Resource Discovery',
            'monitoring': 'Monitoring'
        };

        return titles[intentType || ''] || 'Platform Copilot Response';
    }

    private getAzurePortalUrl(resourceId: string): string {
        // Determine if MAG or commercial Azure
        const isMag = resourceId.includes('.us') || config.apiUrl.includes('.us');
        const portalDomain = isMag ? 'portal.azure.us' : 'portal.azure.com';
        return `https://${portalDomain}/#resource${resourceId}`;
    }

    private getConversationId(chatContext: vscode.ChatContext): string {
        // Generate a stable conversation ID based on the chat context
        // This helps maintain context across multiple requests
        return `github-copilot-${chatContext.history.length}-${Date.now()}`;
    }

    private updateConversationHistory(
        conversationId: string,
        userMessage: string,
        response: string
    ): void {
        const history = this.conversationHistory.get(conversationId) || [];
        history.push(`User: ${userMessage}`);
        history.push(`Assistant: ${response}`);
        this.conversationHistory.set(conversationId, history);

        // Clean up old conversations (keep last 10)
        if (this.conversationHistory.size > 10) {
            const oldestKey = this.conversationHistory.keys().next().value;
            if (oldestKey) {
                this.conversationHistory.delete(oldestKey);
            }
        }
    }

    dispose(): void {
        this.participant.dispose();
        this.conversationHistory.clear();
        config.info('Platform chat participant disposed');
    }
}
