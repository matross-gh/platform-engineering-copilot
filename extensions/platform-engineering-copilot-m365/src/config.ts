import dotenv from 'dotenv';

dotenv.config();

export const config = {
    // MCP HTTP endpoint (legacy env names retained)
    platformApiUrl: process.env.PLATFORM_API_URL || 'http://localhost:5100',
    platformApiKey: process.env.PLATFORM_API_KEY || '',

    // Azure AD
    azureAdTenantId: process.env.AZURE_AD_TENANT_ID || '',
    azureAdClientId: process.env.AZURE_AD_CLIENT_ID || '',
    azureAdClientSecret: process.env.AZURE_AD_CLIENT_SECRET || '',

    // Server
    port: parseInt(process.env.PORT || '3978', 10),
    nodeEnv: process.env.NODE_ENV || 'development',

    // M365 Copilot
    botId: process.env.BOT_ID || '',
    botPassword: process.env.BOT_PASSWORD || '',

    // Logging
    logLevel: process.env.LOG_LEVEL || 'info'
};

// Validate required configuration
export function validateConfig(): void {
    const requiredVars = [
        'PLATFORM_API_URL'
    ];

    const missing = requiredVars.filter(varName => 
        !process.env[varName] || process.env[varName] === ''
    );

    if (missing.length > 0) {
        console.warn(`⚠️  Warning: Missing environment variables: ${missing.join(', ')}`);
        console.warn('   Using default values. Check .env file for production deployment.');
    }
}

validateConfig();
