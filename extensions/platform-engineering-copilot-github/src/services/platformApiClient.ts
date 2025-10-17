import axios, { AxiosInstance, AxiosRequestConfig, AxiosError } from 'axios';
import { config } from '../config';

/**
 * Request and Response interfaces for Platform Copilot API
 */
export interface IntelligentChatRequest {
    message: string;
    conversationId?: string;
    context?: {
        source: string;
        userId?: string;
        [key: string]: any;
    };
}

export interface IntelligentChatResponse {
    response: string;
    intentType?: string;
    toolExecuted?: string;
    requiresFollowUp?: boolean;
    followUpPrompt?: string;
    missingFields?: string[];
    quickReplies?: string[];
    success: boolean;
    metadata?: {
        resourceId?: string;
        cost?: number;
        complianceScore?: number;
        [key: string]: any;
    };
}

export interface ProvisionRequest {
    resourceType: string;
    resourceName: string;
    resourceGroup: string;
    location?: string;
    parameters?: Record<string, any>;
}

export interface ComplianceRequest {
    targetType: 'subscription' | 'resourceGroup' | 'resource';
    targetId: string;
    framework?: 'NIST-800-53' | 'NIST-800-171' | 'ISO-27001';
}

export interface ComplianceResponse {
    overallScore: number;
    passedControls: number;
    failedControls: number;
    warningControls: number;
    controls: Array<{
        id: string;
        name: string;
        status: 'passed' | 'failed' | 'warning';
        description?: string;
    }>;
}

export interface CostEstimate {
    monthlyEstimate: number;
    annualEstimate: number;
    breakdown: Array<{
        service: string;
        cost: number;
    }>;
}

export interface ResourceListResponse {
    resources: Array<{
        id: string;
        name: string;
        type: string;
        location: string;
        resourceGroup: string;
    }>;
    count: number;
}

/**
 * Platform Copilot API Client
 * Handles all HTTP communication with the Platform Copilot API
 */
export class PlatformApiClient {
    private client: AxiosInstance;

    constructor() {
        this.client = axios.create({
            baseURL: config.apiUrl,
            timeout: config.timeout,
            headers: {
                'Content-Type': 'application/json',
                'User-Agent': 'Platform-Copilot-GitHub-Extension/1.0.0'
            }
        });

        // Add API key if configured
        if (config.apiKey) {
            this.client.defaults.headers.common['X-API-Key'] = config.apiKey;
        }

        // Request interceptor
        this.client.interceptors.request.use(
            (requestConfig) => {
                config.log(`→ ${requestConfig.method?.toUpperCase()} ${requestConfig.url}`);
                if (requestConfig.data) {
                    config.log('  Request data:', requestConfig.data);
                }
                return requestConfig;
            },
            (error) => {
                config.error('Request error:', error);
                return Promise.reject(error);
            }
        );

        // Response interceptor
        this.client.interceptors.response.use(
            (response) => {
                config.log(`← ${response.status} ${response.config.url}`);
                return response;
            },
            (error: AxiosError) => {
                if (error.response) {
                    config.error(`← ${error.response.status} ${error.config?.url}`, error.response.data);
                } else if (error.request) {
                    config.error('No response received:', error.message);
                } else {
                    config.error('Request setup error:', error.message);
                }
                return Promise.reject(error);
            }
        );
    }

    /**
     * Send an intelligent chat query to the Platform API
     */
    async sendIntelligentQuery(request: IntelligentChatRequest): Promise<IntelligentChatResponse> {
        try {
            const response = await this.client.post<IntelligentChatResponse>(
                '/api/chat/intelligent-query',
                request
            );
            return response.data;
        } catch (error) {
            throw this.handleError(error, 'Failed to send intelligent query');
        }
    }

    /**
     * Provision Azure infrastructure
     */
    async provisionInfrastructure(request: ProvisionRequest): Promise<any> {
        try {
            const response = await this.client.post('/api/infrastructure/provision', request);
            return response.data;
        } catch (error) {
            throw this.handleError(error, 'Failed to provision infrastructure');
        }
    }

    /**
     * Run compliance assessment
     */
    async runComplianceAssessment(request: ComplianceRequest): Promise<ComplianceResponse> {
        try {
            const response = await this.client.post<ComplianceResponse>(
                '/api/compliance/assess',
                request
            );
            return response.data;
        } catch (error) {
            throw this.handleError(error, 'Failed to run compliance assessment');
        }
    }

    /**
     * Estimate costs
     */
    async estimateCost(resourceType: string, parameters: Record<string, any>): Promise<CostEstimate> {
        try {
            const response = await this.client.post<CostEstimate>('/api/cost/estimate', {
                resourceType,
                parameters
            });
            return response.data;
        } catch (error) {
            throw this.handleError(error, 'Failed to estimate cost');
        }
    }

    /**
     * List Azure resources
     */
    async listResources(filters?: {
        resourceGroup?: string;
        resourceType?: string;
        location?: string;
    }): Promise<ResourceListResponse> {
        try {
            const response = await this.client.get<ResourceListResponse>('/api/resources/list', {
                params: filters
            });
            return response.data;
        } catch (error) {
            throw this.handleError(error, 'Failed to list resources');
        }
    }

    /**
     * Health check
     */
    async healthCheck(): Promise<{ healthy: boolean; version?: string; message?: string }> {
        try {
            const response = await this.client.get('/health');
            return {
                healthy: true,
                ...response.data
            };
        } catch (error) {
            return {
                healthy: false,
                message: error instanceof Error ? error.message : 'Unknown error'
            };
        }
    }

    /**
     * Update configuration (called when VS Code settings change)
     */
    updateConfig(): void {
        this.client.defaults.baseURL = config.apiUrl;
        this.client.defaults.timeout = config.timeout;

        if (config.apiKey) {
            this.client.defaults.headers.common['X-API-Key'] = config.apiKey;
        } else {
            delete this.client.defaults.headers.common['X-API-Key'];
        }

        config.info('API client configuration updated');
    }

    /**
     * Error handler
     */
    private handleError(error: unknown, context: string): Error {
        if (axios.isAxiosError(error)) {
            const axiosError = error as AxiosError;
            
            if (axiosError.response) {
                // Server responded with error
                const data = axiosError.response.data as any;
                const message = data?.message || data?.error || axiosError.message;
                return new Error(`${context}: ${message} (HTTP ${axiosError.response.status})`);
            } else if (axiosError.request) {
                // No response received
                return new Error(`${context}: Could not reach Platform API at ${config.apiUrl}. Is the API running?`);
            }
        }
        
        // Generic error
        return new Error(`${context}: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
}
