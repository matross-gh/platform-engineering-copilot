import { IntelligentChatResponse } from './platformApiClient';

export class AdaptiveCardBuilder {
    
    buildFollowUpCard(prompt: string, missingFields: string[], quickReplies?: string[]): any {
        const card: any = {
            type: 'AdaptiveCard',
            $schema: 'http://adaptivecards.io/schemas/adaptive-card.json',
            version: '1.5',
            body: [
                {
                    type: 'TextBlock',
                    text: '❓ Additional Information Needed',
                    weight: 'Bolder',
                    size: 'Large',
                    color: 'Attention'
                },
                {
                    type: 'TextBlock',
                    text: prompt,
                    wrap: true,
                    spacing: 'Medium'
                }
            ]
        };

        // Add missing fields as a list
        if (missingFields.length > 0) {
            card.body.push({
                type: 'TextBlock',
                text: '**Missing Information:**',
                weight: 'Bolder',
                spacing: 'Medium'
            });

            card.body.push({
                type: 'FactSet',
                facts: missingFields.map((field, index) => ({
                    title: `${index + 1}.`,
                    value: field
                }))
            });
        }

        // Add quick reply buttons if provided
        if (quickReplies && quickReplies.length > 0) {
            card.actions = quickReplies.map(reply => ({
                type: 'Action.Submit',
                title: reply,
                data: { message: reply }
            }));
        }

        return card;
    }

    buildInfrastructureResultCard(response: IntelligentChatResponse): any {
        const isSuccess = response.success !== false;
        
        return {
            type: 'AdaptiveCard',
            $schema: 'http://adaptivecards.io/schemas/adaptive-card.json',
            version: '1.5',
            body: [
                {
                    type: 'TextBlock',
                    text: isSuccess ? '✅ Infrastructure Operation Complete' : '❌ Infrastructure Operation Failed',
                    weight: 'Bolder',
                    size: 'Large',
                    color: isSuccess ? 'Good' : 'Attention'
                },
                {
                    type: 'TextBlock',
                    text: response.response,
                    wrap: true,
                    spacing: 'Medium'
                }
            ],
            actions: isSuccess ? [
                {
                    type: 'Action.OpenUrl',
                    title: 'View in Azure Portal',
                    url: response.resourceId 
                        ? `https://portal.azure.us/#resource${response.resourceId}`
                        : 'https://portal.azure.us'
                }
            ] : []
        };
    }

    buildComplianceResultCard(response: IntelligentChatResponse): any {
        const metadata = response.metadata || {};
        const overallScore = metadata.overallScore || 0;
        const findings = metadata.findings || [];

        return {
            type: 'AdaptiveCard',
            $schema: 'http://adaptivecards.io/schemas/adaptive-card.json',
            version: '1.5',
            body: [
                {
                    type: 'TextBlock',
                    text: '🔍 Compliance Assessment Complete',
                    weight: 'Bolder',
                    size: 'Large'
                },
                {
                    type: 'TextBlock',
                    text: response.response,
                    wrap: true,
                    spacing: 'Medium'
                },
                {
                    type: 'ColumnSet',
                    columns: [
                        {
                            type: 'Column',
                            width: 'auto',
                            items: [
                                {
                                    type: 'TextBlock',
                                    text: `${overallScore}%`,
                                    size: 'ExtraLarge',
                                    weight: 'Bolder',
                                    color: overallScore >= 80 ? 'Good' : overallScore >= 60 ? 'Warning' : 'Attention'
                                },
                                {
                                    type: 'TextBlock',
                                    text: 'Overall Score',
                                    spacing: 'None'
                                }
                            ]
                        },
                        {
                            type: 'Column',
                            width: 'stretch',
                            items: [
                                {
                                    type: 'FactSet',
                                    facts: [
                                        {
                                            title: '✅ Passed',
                                            value: metadata.passedControls?.toString() || '0'
                                        },
                                        {
                                            title: '⚠️  Warnings',
                                            value: metadata.warnings?.toString() || '0'
                                        },
                                        {
                                            title: '❌ Failed',
                                            value: metadata.failedControls?.toString() || '0'
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ],
            actions: [
                {
                    type: 'Action.OpenUrl',
                    title: 'View Full Report',
                    url: metadata.reportUrl || '#'
                },
                {
                    type: 'Action.Submit',
                    title: 'Generate Remediation Plan',
                    data: { action: 'remediate' }
                }
            ]
        };
    }

    buildCostEstimateCard(response: IntelligentChatResponse): any {
        const metadata = response.metadata || {};
        const monthlyEstimate = metadata.monthlyEstimate || 0;
        const annualEstimate = metadata.annualEstimate || 0;

        return {
            type: 'AdaptiveCard',
            $schema: 'http://adaptivecards.io/schemas/adaptive-card.json',
            version: '1.5',
            body: [
                {
                    type: 'TextBlock',
                    text: '💰 Cost Estimate',
                    weight: 'Bolder',
                    size: 'Large'
                },
                {
                    type: 'TextBlock',
                    text: response.response,
                    wrap: true,
                    spacing: 'Medium'
                },
                {
                    type: 'ColumnSet',
                    columns: [
                        {
                            type: 'Column',
                            width: 'stretch',
                            items: [
                                {
                                    type: 'TextBlock',
                                    text: `$${monthlyEstimate.toFixed(2)}`,
                                    size: 'Large',
                                    weight: 'Bolder',
                                    color: 'Accent'
                                },
                                {
                                    type: 'TextBlock',
                                    text: 'Monthly',
                                    spacing: 'None'
                                }
                            ]
                        },
                        {
                            type: 'Column',
                            width: 'stretch',
                            items: [
                                {
                                    type: 'TextBlock',
                                    text: `$${annualEstimate.toFixed(2)}`,
                                    size: 'Large',
                                    weight: 'Bolder',
                                    color: 'Accent'
                                },
                                {
                                    type: 'TextBlock',
                                    text: 'Annually',
                                    spacing: 'None'
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    buildDeploymentResultCard(response: IntelligentChatResponse): any {
        return {
            type: 'AdaptiveCard',
            $schema: 'http://adaptivecards.io/schemas/adaptive-card.json',
            version: '1.5',
            body: [
                {
                    type: 'TextBlock',
                    text: '🚀 Deployment Complete',
                    weight: 'Bolder',
                    size: 'Large',
                    color: 'Good'
                },
                {
                    type: 'TextBlock',
                    text: response.response,
                    wrap: true,
                    spacing: 'Medium'
                }
            ]
        };
    }

    buildResourceListCard(response: IntelligentChatResponse): any {
        const metadata = response.metadata || {};
        const resources = metadata.resources || [];

        return {
            type: 'AdaptiveCard',
            $schema: 'http://adaptivecards.io/schemas/adaptive-card.json',
            version: '1.5',
            body: [
                {
                    type: 'TextBlock',
                    text: '📊 Resource List',
                    weight: 'Bolder',
                    size: 'Large'
                },
                {
                    type: 'TextBlock',
                    text: response.response,
                    wrap: true,
                    spacing: 'Medium'
                },
                {
                    type: 'TextBlock',
                    text: `**Total Resources:** ${resources.length}`,
                    weight: 'Bolder',
                    spacing: 'Medium'
                }
            ]
        };
    }

    buildGenericResponseCard(response: IntelligentChatResponse): any {
        return {
            type: 'AdaptiveCard',
            $schema: 'http://adaptivecards.io/schemas/adaptive-card.json',
            version: '1.5',
            body: [
                {
                    type: 'TextBlock',
                    text: '🤖 Platform Copilot',
                    weight: 'Bolder',
                    size: 'Large'
                },
                {
                    type: 'TextBlock',
                    text: response.response,
                    wrap: true,
                    spacing: 'Medium'
                }
            ]
        };
    }

    buildErrorCard(title: string, message: string): any {
        return {
            type: 'AdaptiveCard',
            $schema: 'http://adaptivecards.io/schemas/adaptive-card.json',
            version: '1.5',
            body: [
                {
                    type: 'TextBlock',
                    text: `❌ ${title}`,
                    weight: 'Bolder',
                    size: 'Large',
                    color: 'Attention'
                },
                {
                    type: 'TextBlock',
                    text: message,
                    wrap: true,
                    spacing: 'Medium'
                },
                {
                    type: 'TextBlock',
                    text: 'Please try again or contact support if the issue persists.',
                    isSubtle: true,
                    spacing: 'Medium'
                }
            ]
        };
    }
}
