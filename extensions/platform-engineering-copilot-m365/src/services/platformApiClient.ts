import axios, { AxiosInstance, AxiosError } from 'axios';

export interface IntelligentChatRequest {
    message: string;
    conversationId?: string;
    context?: Record<string, any>;
}

export interface IntelligentChatResponse {
    success: boolean;
    response: string;
    conversationId: string;
    intentType?: string;
    confidence?: number;
    toolExecuted?: string;
    executionTimeMs?: number;
    requiresFollowUp?: boolean;
    followUpPrompt?: string;
    missingFields?: string[];
    quickReplies?: string[];
    generatedCode?: string;
    metadata?: {
        resourceId?: string;
        complianceScore?: number;
        [key: string]: any;
    };
    // Convenience accessors for common metadata fields
    resourceId?: string;
    error?: string;
}

export interface McpHealthResponse {
    status: string;
    mode: string;
    server: string;
    version: string;
}

export class PlatformApiClient {
    private readonly client: AxiosInstance;
    private readonly baseUrl: string;

    constructor(baseUrl: string, apiKey?: string) {
        this.baseUrl = baseUrl;
        
        this.client = axios.create({
            baseURL: baseUrl,
            timeout: 300000, // 5 minutes for long-running operations
            headers: {
                'Content-Type': 'application/json',
                'User-Agent': 'Platform-Copilot-M365-Extension/1.0.0',
                ...(apiKey ? { 'X-API-Key': apiKey } : {})
            }
        });

        // Request interceptor
        this.client.interceptors.request.use(
            (config) => {
                console.log(`🔵 API Request: ${config.method?.toUpperCase()} ${config.url}`);
                if (config.data) {
                    console.log('  Request data:', config.data);
                }
                return config;
            },
            (error) => {
                console.error('❌ API Request Error:', error);
                return Promise.reject(error);
            }
        );

        // Response interceptor
        this.client.interceptors.response.use(
            (response) => {
                console.log(`✅ API Response: ${response.status} ${response.config.url}`);
                return response;
            },
            (error: AxiosError) => {
                if (error.response) {
                    console.error(`❌ API Response Error: ${error.response.status} ${error.config?.url}`, error.response.data);
                } else if (error.request) {
                    console.error('❌ No response received:', error.message);
                } else {
                    console.error('❌ Request setup error:', error.message);
                }
                return Promise.reject(error);
            }
        );
    }

    /**
     * Send a chat message to the MCP server
     */
    async sendChatMessage(
        message: string,
        conversationId?: string,
        context?: Record<string, any>
    ): Promise<IntelligentChatResponse> {
        try {
            const request: IntelligentChatRequest = {
                message,
                conversationId: conversationId || this.generateConversationId(),
                context: {
                    source: 'm365-copilot',
                    platform: 'M365',
                    ...context
                }
            };

            const response = await this.client.post<IntelligentChatResponse>('/mcp/chat', request);
            
            if (!response.data.success) {
                throw new Error(response.data.error || 'MCP server returned unsuccessful response');
            }

            return response.data;
        } catch (error) {
            throw this.handleError(error, 'Failed to send chat message to MCP server');
        }
    }

    /**
     * Send intelligent query (alias for sendChatMessage for backward compatibility)
     */
    async sendIntelligentQuery(request: IntelligentChatRequest): Promise<IntelligentChatResponse> {
        return this.sendChatMessage(
            request.message,
            request.conversationId,
            request.context
        );
    }

    /**
     * Provision infrastructure
     */
    async provisionInfrastructure(query: string): Promise<IntelligentChatResponse> {
        return this.sendChatMessage(query, undefined, {
            operation: 'infrastructure-provisioning',
            intent: 'infrastructure-provisioning'
        });
    }

    /**
     * Run compliance assessment
     */
    async runComplianceAssessment(
        subscriptionId: string,
        resourceGroupName?: string
    ): Promise<IntelligentChatResponse> {
        const message = resourceGroupName 
            ? `Run compliance assessment for subscription ${subscriptionId} resource group ${resourceGroupName}`
            : `Run compliance assessment for subscription ${subscriptionId}`;
        
        return this.sendChatMessage(message, undefined, {
            operation: 'compliance-assessment',
            intent: 'compliance-assessment',
            subscriptionId,
            resourceGroupName
        });
    }

    /**
     * Estimate cost
     */
    async estimateCost(resourceDefinition: any): Promise<IntelligentChatResponse> {
        const message = `Estimate cost for the following resource: ${JSON.stringify(resourceDefinition)}`;
        
        return this.sendChatMessage(message, undefined, {
            operation: 'cost-estimation',
            intent: 'cost-estimation',
            resourceDefinition
        });
    }

    /**
     * List resources
     */
    async listResources(resourceGroupName: string): Promise<IntelligentChatResponse> {
        const message = `List all resources in resource group ${resourceGroupName}`;
        
        return this.sendChatMessage(message, undefined, {
            operation: 'list-resources',
            intent: 'list-resources',
            resourceGroupName
        });
    }

    /**
     * Check MCP server health
     */
    async healthCheck(): Promise<boolean> {
        try {
            const response = await this.client.get<McpHealthResponse>('/health');
            return response.status === 200 && response.data.status === 'healthy';
        } catch (error) {
            console.error('Platform API health check failed:', error);
            return false;
        }
    }

    /**
     * Generate a unique conversation ID
     */
    private generateConversationId(): string {
        return `m365-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Handle and format errors
     */
    private handleError(error: any, contextMessage: string): Error {
        if (error instanceof AxiosError) {
            if (error.response) {
                const statusCode = error.response.status;
                const responseData = error.response.data;
                
                return new Error(
                    `${contextMessage}: HTTP ${statusCode} - ${
                        responseData?.error || responseData?.message || 'Unknown server error'
                    }`
                );
            } else if (error.request) {
                return new Error(
                    `${contextMessage}: Could not reach MCP server at ${this.baseUrl}. Please verify the server is running.`
                );
            } else {
                return new Error(`${contextMessage}: Request configuration error - ${error.message}`);
            }
        }

        return new Error(
            `${contextMessage}: ${error instanceof Error ? error.message : 'Unknown error'}`
        );
    }
}
