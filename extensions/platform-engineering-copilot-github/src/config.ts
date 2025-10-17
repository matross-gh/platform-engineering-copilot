import * as vscode from 'vscode';

/**
 * Configuration management for Platform Engineering Copilot GitHub Extension
 */
export class Config {
    private static instance: Config;
    private config: vscode.WorkspaceConfiguration;

    private constructor() {
        this.config = vscode.workspace.getConfiguration('platform-copilot');
    }

    public static getInstance(): Config {
        if (!Config.instance) {
            Config.instance = new Config();
        }
        return Config.instance;
    }

    public reload(): void {
        this.config = vscode.workspace.getConfiguration('platform-copilot');
    }

    // Platform API Configuration
    public get apiUrl(): string {
        return this.config.get<string>('apiUrl', 'http://localhost:7001');
    }

    public get apiKey(): string {
        return this.config.get<string>('apiKey', '');
    }

    public get timeout(): number {
        return this.config.get<number>('timeout', 60000);
    }

    public get enableLogging(): boolean {
        return this.config.get<boolean>('enableLogging', true);
    }

    // Validation
    public validate(): { valid: boolean; errors: string[] } {
        const errors: string[] = [];

        if (!this.apiUrl) {
            errors.push('Platform API URL is not configured');
        }

        if (this.timeout < 1000) {
            errors.push('Timeout must be at least 1000ms');
        }

        return {
            valid: errors.length === 0,
            errors
        };
    }

    // Logging helper
    public log(message: string, ...args: any[]): void {
        if (this.enableLogging) {
            console.log(`[Platform Copilot] ${message}`, ...args);
        }
    }

    public error(message: string, ...args: any[]): void {
        console.error(`[Platform Copilot] ❌ ${message}`, ...args);
    }

    public warn(message: string, ...args: any[]): void {
        console.warn(`[Platform Copilot] ⚠️  ${message}`, ...args);
    }

    public info(message: string, ...args: any[]): void {
        if (this.enableLogging) {
            console.info(`[Platform Copilot] ℹ️  ${message}`, ...args);
        }
    }
}

// Export singleton instance
export const config = Config.getInstance();
