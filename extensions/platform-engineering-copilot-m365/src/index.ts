import express, { Request, Response } from 'express';
import { config } from './config';
import { PlatformApiClient } from './services/platformApiClient';
import { MessageHandler } from './services/messageHandler';
import { AdaptiveCardBuilder } from './services/adaptiveCardBuilder';

const app = express();
app.use(express.json());

// Initialize services
const platformClient = new PlatformApiClient(config.platformApiUrl, config.platformApiKey);
const cardBuilder = new AdaptiveCardBuilder();
const messageHandler = new MessageHandler(platformClient, cardBuilder);

// Health check endpoint
app.get('/health', (req: Request, res: Response) => {
    res.json({ 
        status: 'healthy', 
        service: 'Platform Copilot M365 Extension',
        version: '1.0.0',
        timestamp: new Date().toISOString()
    });
});

// M365 Copilot webhook endpoint
app.post('/api/messages', async (req: Request, res: Response) => {
    try {
        console.log('📨 Received message:', JSON.stringify(req.body, null, 2));

        const userMessage = req.body.text || req.body.message;
        const conversationId = req.body.conversation?.id || `m365-${Date.now()}`;
        const userId = req.body.from?.id || 'anonymous';

        if (!userMessage) {
            return res.status(400).json({ error: 'No message text provided' });
        }

        // Process message through Platform Copilot
        const response = await messageHandler.handleMessage({
            message: userMessage,
            conversationId,
            userId,
            source: 'm365-copilot'
        });

        // Return adaptive card response
        res.json({
            type: 'message',
            attachments: [{
                contentType: 'application/vnd.microsoft.card.adaptive',
                content: response.card
            }],
            text: response.text
        });

    } catch (error) {
        console.error('❌ Error processing message:', error);
        
        const errorCard = cardBuilder.buildErrorCard(
            'Processing Error',
            error instanceof Error ? error.message : 'An unexpected error occurred'
        );

        res.status(500).json({
            type: 'message',
            attachments: [{
                contentType: 'application/vnd.microsoft.card.adaptive',
                content: errorCard
            }]
        });
    }
});

// OpenAPI spec endpoint (for M365 Copilot discovery)
app.get('/openapi.json', (req: Request, res: Response) => {
    res.sendFile('openapi/openapi.json', { root: __dirname });
});

// AI Plugin manifest endpoint
app.get('/ai-plugin.json', (req: Request, res: Response) => {
    res.sendFile('appPackage/ai-plugin.json', { root: __dirname });
});

// Start server
const PORT = config.port;
app.listen(PORT, () => {
    console.log(`
    ╔════════════════════════════════════════════════════╗
    ║  Platform Engineering Copilot - M365 Extension     ║
    ║                                                    ║
    ║  🚀 Server running on port ${PORT}                    ║
    ║  🔗 Platform API: ${config.platformApiUrl.padEnd(28)}║
    ║  🎯 Environment: ${config.nodeEnv.padEnd(34)}║
    ║                                                    ║
    ║  📡 Endpoints:                                     ║
    ║     POST /api/messages  - M365 webhook            ║
    ║     GET  /health        - Health check            ║
    ║     GET  /openapi.json  - API specification       ║
    ║                                                    ║
    ╚════════════════════════════════════════════════════╝
    `);
});

// Graceful shutdown
process.on('SIGINT', () => {
    console.log('\n⏹️  Shutting down gracefully...');
    process.exit(0);
});

process.on('SIGTERM', () => {
    console.log('\n⏹️  Shutting down gracefully...');
    process.exit(0);
});
