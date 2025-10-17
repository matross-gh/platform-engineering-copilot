import { PlatformApiClient, IntelligentChatResponse } from './platformApiClient';
import { AdaptiveCardBuilder } from './adaptiveCardBuilder';

export interface MessageContext {
    message: string;
    conversationId: string;
    userId: string;
    source: string;
}

export interface MessageResponse {
    text: string;
    card: any;
}

export class MessageHandler {
    constructor(
        private readonly apiClient: PlatformApiClient,
        private readonly cardBuilder: AdaptiveCardBuilder
    ) {}

    async handleMessage(context: MessageContext): Promise<MessageResponse> {
        try {
            console.log(`📝 Processing message from user ${context.userId}: ${context.message}`);

            // Send to Platform Copilot API
            const response = await this.apiClient.sendIntelligentQuery({
                message: context.message,
                conversationId: context.conversationId,
                context: {
                    source: context.source,
                    userId: context.userId
                }
            });

            // Handle follow-up questions
            if (response.requiresFollowUp && response.followUpPrompt) {
                return {
                    text: response.followUpPrompt,
                    card: this.cardBuilder.buildFollowUpCard(
                        response.followUpPrompt,
                        response.missingFields || [],
                        response.quickReplies
                    )
                };
            }

            // Handle successful responses based on intent type
            return this.buildResponseForIntent(response);

        } catch (error) {
            console.error('❌ Error handling message:', error);
            
            return {
                text: 'Sorry, I encountered an error processing your request.',
                card: this.cardBuilder.buildErrorCard(
                    'Processing Error',
                    error instanceof Error ? error.message : 'Unknown error occurred'
                )
            };
        }
    }

    private buildResponseForIntent(response: IntelligentChatResponse): MessageResponse {
        const intentType = response.intentType?.toLowerCase() || 'unknown';

        switch (intentType) {
            case 'infrastructure':
            case 'provisioning':
                return {
                    text: response.response,
                    card: this.cardBuilder.buildInfrastructureResultCard(response)
                };

            case 'compliance':
            case 'security':
                return {
                    text: response.response,
                    card: this.cardBuilder.buildComplianceResultCard(response)
                };

            case 'cost':
            case 'estimation':
                return {
                    text: response.response,
                    card: this.cardBuilder.buildCostEstimateCard(response)
                };

            case 'deployment':
                return {
                    text: response.response,
                    card: this.cardBuilder.buildDeploymentResultCard(response)
                };

            case 'resource_discovery':
            case 'listing':
                return {
                    text: response.response,
                    card: this.cardBuilder.buildResourceListCard(response)
                };

            default:
                return {
                    text: response.response,
                    card: this.cardBuilder.buildGenericResponseCard(response)
                };
        }
    }
}
