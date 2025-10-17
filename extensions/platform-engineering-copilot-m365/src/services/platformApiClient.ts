import axios, { AxiosInstance } from 'axios';

export interface IntelligentChatRequest {
    message: string;
    conversationId: string;
    context?: Record<string, any>;
}

export interface IntelligentChatResponse {
    response: string;
    intentType: string;
    toolExecuted: boolean;
    requiresFollowUp: boolean;
    followUpPrompt?: string;
    missingFields?: string[];
    quickReplies?: string[];
    generatedCode?: string;
    resourceId?: string;
    success: boolean;
    metadata?: Record<string, any>;
}

export class PlatformApiClient {
    private readonly client: AxiosInstance;

    constructor(baseUrl: string, apiKey?: string) {
        this.client = axios.create({
            baseURL: baseUrl,
            timeout: 60000, // 60 seconds for complex operations
            headers: {
                'Content-Type': 'application/json',
                ...(apiKey ? { 'X-API-Key': apiKey } : {})
            }
        });

        // Add request logging
        this.client.interceptors.request.use(
            (config) => {
                console.log(`🔵 API Request: ${config.method?.toUpperCase()} ${config.url}`);
                return config;
            },
            (error) => {
                console.error('❌ API Request Error:', error);
                return Promise.reject(error);
            }
        );

        // Add response logging
        this.client.interceptors.response.use(
            (response) => {
                console.log(`✅ API Response: ${response.status} ${response.config.url}`);
                return response;
            },
            (error) => {
                console.error('❌ API Response Error:', error.response?.status, error.message);
                return Promise.reject(error);
            }
        );
    }

    async sendIntelligentQuery(request: IntelligentChatRequest): Promise<IntelligentChatResponse> {
        try {
            const response = await this.client.post<IntelligentChatResponse>(
                '/api/chat/intelligent-query',
                {
                    ...request,
                    context: {
                        ...request.context,
                        source: 'm365-copilot',
                        timestamp: new Date().toISOString()
                    }
                }
            );

            return response.data;
        } catch (error) {
            if (axios.isAxiosError(error)) {
                throw new Error(
                    `Platform API Error: ${error.response?.status} - ${error.response?.data?.message || error.message}`
                );
            }
            throw error;
        }
    }

    async provisionInfrastructure(query: string): Promise<any> {
        try {
            const response = await this.client.post('/api/infrastructure/provision', {
                query
            });
            return response.data;
        } catch (error) {
            if (axios.isAxiosError(error)) {
                throw new Error(
                    `Infrastructure Provisioning Error: ${error.response?.data?.message || error.message}`
                );
            }
            throw error;
        }
    }

    async runComplianceAssessment(subscriptionId: string, resourceGroupName?: string): Promise<any> {
        try {
            const response = await this.client.post('/api/compliance/assess', {
                subscriptionId,
                resourceGroupName
            });
            return response.data;
        } catch (error) {
            if (axios.isAxiosError(error)) {
                throw new Error(
                    `Compliance Assessment Error: ${error.response?.data?.message || error.message}`
                );
            }
            throw error;
        }
    }

    async estimateCost(resourceDefinition: any): Promise<any> {
        try {
            const response = await this.client.post('/api/cost/estimate', {
                resourceDefinition
            });
            return response.data;
        } catch (error) {
            if (axios.isAxiosError(error)) {
                throw new Error(
                    `Cost Estimation Error: ${error.response?.data?.message || error.message}`
                );
            }
            throw error;
        }
    }

    async listResources(resourceGroupName: string): Promise<any> {
        try {
            const response = await this.client.get('/api/resources/list', {
                params: { resourceGroupName }
            });
            return response.data;
        } catch (error) {
            if (axios.isAxiosError(error)) {
                throw new Error(
                    `List Resources Error: ${error.response?.data?.message || error.message}`
                );
            }
            throw error;
        }
    }

    async healthCheck(): Promise<boolean> {
        try {
            const response = await this.client.get('/health');
            return response.status === 200;
        } catch (error) {
            console.error('Platform API health check failed:', error);
            return false;
        }
    }
}
