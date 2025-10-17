# Integrating Platform Engineering Copilot with Existing Chat Applications

**Last Updated**: October 17, 2025  
**Status**: Implementation Guide

---

## Overview

This guide explains how to integrate the Platform Engineering Copilot with existing chat applications, enabling AI-powered Azure infrastructure management through popular chat platforms. Whether you're using Slack, Discord, Telegram, or a custom chat application, this guide provides implementation patterns and best practices.

---

## 🎯 Supported Chat Platforms

### Quick Reference Table

| Platform | Complexity | Best Use Case | Authentication |
|----------|-----------|---------------|----------------|
| **Slack** | Medium | Enterprise teams | OAuth 2.0 |
| **Microsoft Teams** | Medium | M365 organizations | Azure AD |
| **Discord** | Low | Developer communities | Bot tokens |
| **Telegram** | Low | Individual users | Bot tokens |
| **Mattermost** | Medium | Self-hosted teams | Webhooks/OAuth |
| **Rocket.Chat** | Medium | Self-hosted teams | REST API |
| **Custom Web Chat** | Variable | Embedded in apps | Custom auth |
| **WhatsApp Business** | High | Customer support | Meta API |

---

## 🏗️ Architecture Patterns

### Pattern 1: Webhook-Based Integration (Recommended)

**Best For**: Slack, Discord, Mattermost, Telegram

```
Chat Platform
    ↓ (webhook)
Chat Integration Service (Node.js/Python)
    ↓ (HTTP)
Platform Engineering Copilot API (:7001)
    ↓
Azure Resources
```

**Advantages**:
- Simple to implement
- Real-time message processing
- Scalable with cloud functions
- Easy to debug

### Pattern 2: WebSocket Integration

**Best For**: Custom web chat, real-time applications

```
Web Client (WebSocket)
    ↓
WebSocket Server
    ↓
Platform Engineering Copilot API
    ↓
Azure Resources
```

**Advantages**:
- Bi-directional communication
- Low latency
- Perfect for web apps
- Supports streaming responses

### Pattern 3: Bot Framework Integration

**Best For**: Multi-platform deployment

```
Multiple Chat Platforms
    ↓
Microsoft Bot Framework / Botpress
    ↓
Platform Engineering Copilot API
    ↓
Azure Resources
```

**Advantages**:
- Single codebase for multiple platforms
- Rich card support
- Built-in state management
- Enterprise-grade features

---

## 📱 Platform-Specific Integrations

## 1. Slack Integration

### Prerequisites
- Slack workspace admin access
- Slack App created at [api.slack.com/apps](https://api.slack.com/apps)
- Node.js 18+ or Python 3.10+

### Quick Start

#### Step 1: Create Slack App

1. Go to [api.slack.com/apps](https://api.slack.com/apps)
2. Click **Create New App** → **From scratch**
3. Name: "Platform Copilot"
4. Choose your workspace

#### Step 2: Configure Bot Permissions

Add these Bot Token Scopes:
- `chat:write` - Send messages
- `commands` - Handle slash commands
- `app_mentions:read` - Respond to @mentions
- `im:history` - Read DMs
- `im:write` - Send DMs

#### Step 3: Enable Event Subscriptions

Request URL: `https://your-domain.com/slack/events`

Subscribe to bot events:
- `app_mention` - When bot is mentioned
- `message.im` - Direct messages to bot

#### Step 4: Implementation (Node.js)

```javascript
// slack-integration.js
const { App } = require('@slack/bolt');
const axios = require('axios');

const app = new App({
  token: process.env.SLACK_BOT_TOKEN,
  signingSecret: process.env.SLACK_SIGNING_SECRET,
  socketMode: false,
  port: process.env.PORT || 3000
});

// Platform Copilot API client
const platformApi = axios.create({
  baseURL: process.env.PLATFORM_API_URL || 'http://localhost:7001',
  timeout: 60000
});

// Handle @mentions
app.event('app_mention', async ({ event, say }) => {
  try {
    // Remove bot mention from message
    const userMessage = event.text.replace(/<@[A-Z0-9]+>/g, '').trim();
    
    console.log(`📨 Slack message from ${event.user}: ${userMessage}`);

    // Send to Platform Copilot
    const response = await platformApi.post('/api/chat/intelligent-query', {
      message: userMessage,
      conversationId: `slack-${event.channel}-${event.user}`,
      context: {
        source: 'slack',
        userId: event.user,
        channelId: event.channel
      }
    });

    const platformResponse = response.data;

    // Handle follow-up questions
    if (platformResponse.requiresFollowUp) {
      await say({
        text: platformResponse.followUpPrompt,
        blocks: buildFollowUpBlocks(platformResponse)
      });
      return;
    }

    // Send success response
    await say({
      text: platformResponse.response,
      blocks: buildResponseBlocks(platformResponse)
    });

  } catch (error) {
    console.error('❌ Error:', error);
    await say({
      text: `Sorry, I encountered an error: ${error.message}`,
      blocks: buildErrorBlocks(error)
    });
  }
});

// Slash command: /platform-copilot
app.command('/platform-copilot', async ({ command, ack, say }) => {
  await ack();

  try {
    const response = await platformApi.post('/api/chat/intelligent-query', {
      message: command.text,
      conversationId: `slack-cmd-${command.user_id}`,
      context: {
        source: 'slack-command',
        userId: command.user_id
      }
    });

    await say({
      text: response.data.response,
      blocks: buildResponseBlocks(response.data)
    });

  } catch (error) {
    await say(`Error: ${error.message}`);
  }
});

// Build Slack Block Kit UI
function buildResponseBlocks(platformResponse) {
  const blocks = [
    {
      type: 'header',
      text: {
        type: 'plain_text',
        text: '🤖 Platform Copilot',
        emoji: true
      }
    },
    {
      type: 'section',
      text: {
        type: 'mrkdwn',
        text: platformResponse.response
      }
    }
  ];

  // Add resource link if available
  if (platformResponse.resourceId) {
    blocks.push({
      type: 'actions',
      elements: [
        {
          type: 'button',
          text: {
            type: 'plain_text',
            text: 'View in Azure Portal',
            emoji: true
          },
          url: `https://portal.azure.us/#resource${platformResponse.resourceId}`,
          style: 'primary'
        }
      ]
    });
  }

  return blocks;
}

function buildFollowUpBlocks(platformResponse) {
  const blocks = [
    {
      type: 'section',
      text: {
        type: 'mrkdwn',
        text: `*❓ ${platformResponse.followUpPrompt}*`
      }
    }
  ];

  // Add missing fields
  if (platformResponse.missingFields && platformResponse.missingFields.length > 0) {
    blocks.push({
      type: 'section',
      text: {
        type: 'mrkdwn',
        text: '*Missing Information:*\n' + 
              platformResponse.missingFields.map((f, i) => `${i + 1}. ${f}`).join('\n')
      }
    });
  }

  return blocks;
}

function buildErrorBlocks(error) {
  return [
    {
      type: 'section',
      text: {
        type: 'mrkdwn',
        text: `*❌ Error*\n${error.message}`
      }
    }
  ];
}

// Start the app
(async () => {
  await app.start();
  console.log('⚡️ Slack integration running!');
})();
```

#### Step 5: Deploy

```bash
# Install dependencies
npm install @slack/bolt axios dotenv

# Create .env file
cat > .env << EOF
SLACK_BOT_TOKEN=xoxb-your-bot-token
SLACK_SIGNING_SECRET=your-signing-secret
PLATFORM_API_URL=http://localhost:7001
PORT=3000
EOF

# Run
node slack-integration.js
```

#### Step 6: Test in Slack

```
@Platform Copilot create a storage account named testdata001 in rg-dev

/platform-copilot run compliance scan on my production subscription
```

---

## 2. Discord Integration

### Prerequisites
- Discord server with admin access
- Discord Bot created at [discord.com/developers](https://discord.com/developers)
- Node.js 18+

### Quick Start

#### Step 1: Create Discord Bot

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Click **New Application**
3. Go to **Bot** tab → **Add Bot**
4. Enable **Message Content Intent**
5. Copy bot token

#### Step 2: Implementation (Node.js)

```javascript
// discord-integration.js
const { Client, GatewayIntentBits, EmbedBuilder } = require('discord.js');
const axios = require('axios');
require('dotenv').config();

const client = new Client({
  intents: [
    GatewayIntentBits.Guilds,
    GatewayIntentBits.GuildMessages,
    GatewayIntentBits.MessageContent,
    GatewayIntentBits.DirectMessages
  ]
});

const platformApi = axios.create({
  baseURL: process.env.PLATFORM_API_URL || 'http://localhost:7001',
  timeout: 60000
});

client.on('ready', () => {
  console.log(`✅ Logged in as ${client.user.tag}`);
});

client.on('messageCreate', async (message) => {
  // Ignore bot messages
  if (message.author.bot) return;

  // Only respond to mentions or DMs
  const isMentioned = message.mentions.has(client.user);
  const isDM = message.channel.type === 'DM';

  if (!isMentioned && !isDM) return;

  try {
    // Remove bot mention
    let userMessage = message.content.replace(/<@!?\d+>/g, '').trim();

    // Show typing indicator
    await message.channel.sendTyping();

    console.log(`📨 Discord message from ${message.author.tag}: ${userMessage}`);

    // Send to Platform Copilot
    const response = await platformApi.post('/api/chat/intelligent-query', {
      message: userMessage,
      conversationId: `discord-${message.channel.id}-${message.author.id}`,
      context: {
        source: 'discord',
        userId: message.author.id,
        channelId: message.channel.id,
        guildId: message.guild?.id
      }
    });

    const platformResponse = response.data;

    // Build Discord embed
    const embed = buildDiscordEmbed(platformResponse);

    await message.reply({ embeds: [embed] });

  } catch (error) {
    console.error('❌ Error:', error);
    
    const errorEmbed = new EmbedBuilder()
      .setColor(0xFF0000)
      .setTitle('❌ Error')
      .setDescription(error.message)
      .setTimestamp();

    await message.reply({ embeds: [errorEmbed] });
  }
});

function buildDiscordEmbed(platformResponse) {
  const color = platformResponse.success !== false ? 0x00FF00 : 0xFF0000;
  const title = platformResponse.requiresFollowUp 
    ? '❓ Additional Information Needed'
    : '🤖 Platform Copilot';

  const embed = new EmbedBuilder()
    .setColor(color)
    .setTitle(title)
    .setDescription(platformResponse.response)
    .setTimestamp();

  // Add missing fields
  if (platformResponse.missingFields && platformResponse.missingFields.length > 0) {
    embed.addFields({
      name: 'Missing Information',
      value: platformResponse.missingFields.map((f, i) => `${i + 1}. ${f}`).join('\n')
    });
  }

  // Add metadata
  if (platformResponse.metadata) {
    if (platformResponse.metadata.resourceId) {
      embed.addFields({
        name: 'Resource ID',
        value: `\`${platformResponse.metadata.resourceId}\``
      });
    }
  }

  return embed;
}

// Login
client.login(process.env.DISCORD_BOT_TOKEN);
```

#### Step 3: Deploy

```bash
# Install dependencies
npm install discord.js axios dotenv

# Create .env file
cat > .env << EOF
DISCORD_BOT_TOKEN=your-bot-token
PLATFORM_API_URL=http://localhost:7001
EOF

# Run
node discord-integration.js
```

#### Step 4: Invite Bot to Server

Generate OAuth2 URL with these scopes:
- `bot`
- `applications.commands`

Bot permissions:
- Read Messages
- Send Messages
- Embed Links
- Read Message History

---

## 3. Telegram Integration

### Prerequisites
- Telegram account
- Bot created via [@BotFather](https://t.me/BotFather)
- Python 3.10+ or Node.js 18+

### Quick Start

#### Step 1: Create Telegram Bot

1. Message [@BotFather](https://t.me/BotFather)
2. Send `/newbot`
3. Choose name and username
4. Copy bot token

#### Step 2: Implementation (Python)

```python
# telegram-integration.py
import os
import logging
import requests
from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import Application, CommandHandler, MessageHandler, filters, ContextTypes

# Configure logging
logging.basicConfig(
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    level=logging.INFO
)
logger = logging.getLogger(__name__)

# Platform Copilot API
PLATFORM_API_URL = os.getenv('PLATFORM_API_URL', 'http://localhost:7001')
BOT_TOKEN = os.getenv('TELEGRAM_BOT_TOKEN')

async def start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Handle /start command"""
    await update.message.reply_text(
        "👋 Hi! I'm Platform Copilot, your Azure infrastructure assistant.\n\n"
        "I can help you:\n"
        "• Provision Azure resources\n"
        "• Run compliance assessments\n"
        "• Estimate costs\n"
        "• List resources\n\n"
        "Just send me a message like:\n"
        "'Create a storage account named testdata001'"
    )

async def help_command(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Handle /help command"""
    await update.message.reply_text(
        "🤖 Platform Copilot Commands:\n\n"
        "• Just message me naturally!\n"
        "• Example: 'Create a storage account'\n"
        "• Example: 'Run compliance scan'\n"
        "• Example: 'List resources in rg-prod'\n\n"
        "/start - Show welcome message\n"
        "/help - Show this help"
    )

async def handle_message(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Handle regular messages"""
    user_message = update.message.text
    user_id = update.message.from_user.id
    chat_id = update.message.chat_id

    logger.info(f"📨 Message from {user_id}: {user_message}")

    # Show typing indicator
    await context.bot.send_chat_action(chat_id=chat_id, action="typing")

    try:
        # Send to Platform Copilot
        response = requests.post(
            f"{PLATFORM_API_URL}/api/chat/intelligent-query",
            json={
                "message": user_message,
                "conversationId": f"telegram-{chat_id}-{user_id}",
                "context": {
                    "source": "telegram",
                    "userId": str(user_id),
                    "chatId": str(chat_id)
                }
            },
            timeout=60
        )
        response.raise_for_status()
        
        platform_response = response.json()

        # Handle follow-up questions
        if platform_response.get('requiresFollowUp'):
            await send_followup_message(update, platform_response)
        else:
            await send_response_message(update, platform_response)

    except Exception as e:
        logger.error(f"❌ Error: {e}")
        await update.message.reply_text(
            f"❌ Sorry, I encountered an error:\n{str(e)}"
        )

async def send_followup_message(update: Update, platform_response: dict):
    """Send follow-up question to user"""
    message = f"❓ *Additional Information Needed*\n\n{platform_response['followUpPrompt']}"
    
    # Add missing fields
    if platform_response.get('missingFields'):
        message += "\n\n*Missing Information:*\n"
        for i, field in enumerate(platform_response['missingFields'], 1):
            message += f"{i}. {field}\n"

    await update.message.reply_text(
        message,
        parse_mode='Markdown'
    )

async def send_response_message(update: Update, platform_response: dict):
    """Send Platform Copilot response to user"""
    icon = "✅" if platform_response.get('success', True) else "❌"
    message = f"{icon} *Platform Copilot*\n\n{platform_response['response']}"

    # Add buttons if resource created
    keyboard = None
    if platform_response.get('resourceId'):
        keyboard = InlineKeyboardMarkup([
            [InlineKeyboardButton(
                "View in Azure Portal", 
                url=f"https://portal.azure.us/#resource{platform_response['resourceId']}"
            )]
        ])

    await update.message.reply_text(
        message,
        parse_mode='Markdown',
        reply_markup=keyboard
    )

def main():
    """Start the bot"""
    # Create application
    application = Application.builder().token(BOT_TOKEN).build()

    # Register handlers
    application.add_handler(CommandHandler("start", start))
    application.add_handler(CommandHandler("help", help_command))
    application.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, handle_message))

    # Start polling
    logger.info("✅ Telegram bot started")
    application.run_polling(allowed_updates=Update.ALL_TYPES)

if __name__ == '__main__':
    main()
```

#### Step 3: Deploy

```bash
# Install dependencies
pip install python-telegram-bot requests

# Set environment variables
export TELEGRAM_BOT_TOKEN=your-bot-token
export PLATFORM_API_URL=http://localhost:7001

# Run
python telegram-integration.py
```

---

## 4. Custom Web Chat Integration

### WebSocket Implementation

```javascript
// websocket-chat-server.js
const WebSocket = require('ws');
const axios = require('axios');

const wss = new WebSocket.Server({ port: 8080 });

const platformApi = axios.create({
  baseURL: process.env.PLATFORM_API_URL || 'http://localhost:7001',
  timeout: 60000
});

wss.on('connection', (ws, req) => {
  const userId = req.headers['user-id'] || 'anonymous';
  const conversationId = `websocket-${userId}-${Date.now()}`;

  console.log(`✅ New connection: ${userId}`);

  ws.on('message', async (data) => {
    try {
      const message = JSON.parse(data);
      
      console.log(`📨 Message from ${userId}: ${message.text}`);

      // Send to Platform Copilot
      const response = await platformApi.post('/api/chat/intelligent-query', {
        message: message.text,
        conversationId,
        context: {
          source: 'websocket',
          userId
        }
      });

      // Send response back to client
      ws.send(JSON.stringify({
        type: 'response',
        data: response.data
      }));

    } catch (error) {
      console.error('❌ Error:', error);
      ws.send(JSON.stringify({
        type: 'error',
        message: error.message
      }));
    }
  });

  ws.on('close', () => {
    console.log(`❌ Connection closed: ${userId}`);
  });

  // Send welcome message
  ws.send(JSON.stringify({
    type: 'welcome',
    message: 'Connected to Platform Copilot!'
  }));
});

console.log('✅ WebSocket server running on port 8080');
```

### Client Implementation

```html
<!-- chat-client.html -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Platform Copilot Chat</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background: #f5f5f5;
        }
        #chat-container {
            max-width: 800px;
            margin: 0 auto;
            background: white;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            overflow: hidden;
        }
        #messages {
            height: 500px;
            overflow-y: auto;
            padding: 20px;
            background: #fafafa;
        }
        .message {
            margin-bottom: 15px;
            padding: 10px 15px;
            border-radius: 8px;
            max-width: 70%;
        }
        .user-message {
            background: #007bff;
            color: white;
            margin-left: auto;
            text-align: right;
        }
        .bot-message {
            background: white;
            border: 1px solid #e0e0e0;
        }
        .followup-message {
            background: #fff3cd;
            border: 1px solid #ffc107;
        }
        #input-container {
            padding: 20px;
            border-top: 1px solid #e0e0e0;
            display: flex;
            gap: 10px;
        }
        #message-input {
            flex: 1;
            padding: 10px 15px;
            border: 1px solid #ddd;
            border-radius: 5px;
            font-size: 16px;
        }
        #send-button {
            padding: 10px 30px;
            background: #007bff;
            color: white;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-size: 16px;
        }
        #send-button:hover {
            background: #0056b3;
        }
        #send-button:disabled {
            background: #ccc;
            cursor: not-allowed;
        }
        .status {
            padding: 10px 20px;
            text-align: center;
            font-size: 14px;
            color: #666;
        }
        .status.connected {
            background: #d4edda;
            color: #155724;
        }
        .status.disconnected {
            background: #f8d7da;
            color: #721c24;
        }
    </style>
</head>
<body>
    <div id="chat-container">
        <div id="status" class="status">Connecting...</div>
        <div id="messages"></div>
        <div id="input-container">
            <input 
                type="text" 
                id="message-input" 
                placeholder="Ask me anything about Azure infrastructure..."
                disabled
            />
            <button id="send-button" disabled>Send</button>
        </div>
    </div>

    <script>
        let ws;
        const messagesDiv = document.getElementById('messages');
        const statusDiv = document.getElementById('status');
        const messageInput = document.getElementById('message-input');
        const sendButton = document.getElementById('send-button');

        // Connect to WebSocket
        function connect() {
            ws = new WebSocket('ws://localhost:8080', {
                headers: {
                    'user-id': 'demo-user-' + Math.random().toString(36).substr(2, 9)
                }
            });

            ws.onopen = () => {
                statusDiv.textContent = '✅ Connected';
                statusDiv.className = 'status connected';
                messageInput.disabled = false;
                sendButton.disabled = false;
                messageInput.focus();
            };

            ws.onmessage = (event) => {
                const data = JSON.parse(event.data);
                
                if (data.type === 'response') {
                    addBotMessage(data.data);
                } else if (data.type === 'welcome') {
                    addSystemMessage(data.message);
                } else if (data.type === 'error') {
                    addErrorMessage(data.message);
                }
            };

            ws.onclose = () => {
                statusDiv.textContent = '❌ Disconnected';
                statusDiv.className = 'status disconnected';
                messageInput.disabled = true;
                sendButton.disabled = true;
                
                // Reconnect after 3 seconds
                setTimeout(connect, 3000);
            };

            ws.onerror = (error) => {
                console.error('WebSocket error:', error);
            };
        }

        // Send message
        function sendMessage() {
            const text = messageInput.value.trim();
            if (!text || !ws || ws.readyState !== WebSocket.OPEN) return;

            addUserMessage(text);
            
            ws.send(JSON.stringify({ text }));
            
            messageInput.value = '';
        }

        // Add user message to chat
        function addUserMessage(text) {
            const div = document.createElement('div');
            div.className = 'message user-message';
            div.textContent = text;
            messagesDiv.appendChild(div);
            messagesDiv.scrollTop = messagesDiv.scrollHeight;
        }

        // Add bot message to chat
        function addBotMessage(response) {
            const div = document.createElement('div');
            div.className = response.requiresFollowUp 
                ? 'message followup-message' 
                : 'message bot-message';
            
            let html = `<strong>🤖 Platform Copilot</strong><br><br>${response.response}`;
            
            // Add missing fields
            if (response.missingFields && response.missingFields.length > 0) {
                html += '<br><br><strong>Missing Information:</strong><ul>';
                response.missingFields.forEach(field => {
                    html += `<li>${field}</li>`;
                });
                html += '</ul>';
            }
            
            div.innerHTML = html;
            messagesDiv.appendChild(div);
            messagesDiv.scrollTop = messagesDiv.scrollHeight;
        }

        // Add system message
        function addSystemMessage(text) {
            const div = document.createElement('div');
            div.className = 'message bot-message';
            div.innerHTML = `<em>${text}</em>`;
            messagesDiv.appendChild(div);
            messagesDiv.scrollTop = messagesDiv.scrollHeight;
        }

        // Add error message
        function addErrorMessage(text) {
            const div = document.createElement('div');
            div.className = 'message bot-message';
            div.innerHTML = `<strong>❌ Error:</strong> ${text}`;
            messagesDiv.appendChild(div);
            messagesDiv.scrollTop = messagesDiv.scrollHeight;
        }

        // Event listeners
        sendButton.addEventListener('click', sendMessage);
        messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') sendMessage();
        });

        // Connect on load
        connect();
    </script>
</body>
</html>
```

---

## 🔧 Common Integration Patterns

### 1. Authentication Middleware

```javascript
// auth-middleware.js
async function authenticateUser(userId, platform) {
  // Verify user has access to Platform Copilot
  const response = await axios.post(`${PLATFORM_API_URL}/api/auth/verify`, {
    userId,
    platform
  });
  
  return response.data.authorized;
}

// Usage in your integration
if (!await authenticateUser(userId, 'slack')) {
  return sendUnauthorizedMessage();
}
```

### 2. Rate Limiting

```javascript
// rate-limiter.js
const rateLimit = new Map();

function checkRateLimit(userId, maxRequests = 10, windowMs = 60000) {
  const now = Date.now();
  const userRequests = rateLimit.get(userId) || [];
  
  // Remove old requests
  const recentRequests = userRequests.filter(time => now - time < windowMs);
  
  if (recentRequests.length >= maxRequests) {
    return false; // Rate limit exceeded
  }
  
  recentRequests.push(now);
  rateLimit.set(userId, recentRequests);
  
  return true; // OK to proceed
}

// Usage
if (!checkRateLimit(userId)) {
  return sendRateLimitMessage();
}
```

### 3. Context Persistence

```javascript
// context-manager.js
const conversations = new Map();

function saveContext(conversationId, context) {
  conversations.set(conversationId, {
    ...context,
    lastUpdated: Date.now()
  });
}

function getContext(conversationId) {
  return conversations.get(conversationId);
}

function clearOldContexts(maxAgeMs = 3600000) {
  const now = Date.now();
  for (const [id, context] of conversations.entries()) {
    if (now - context.lastUpdated > maxAgeMs) {
      conversations.delete(id);
    }
  }
}

// Run cleanup every 10 minutes
setInterval(clearOldContexts, 600000);
```

### 4. Error Handling

```javascript
// error-handler.js
function handlePlatformError(error, chatPlatform) {
  const errorResponse = {
    title: 'Platform Copilot Error',
    message: 'Sorry, something went wrong.',
    details: null
  };

  if (error.response) {
    // API error
    errorResponse.details = error.response.data.message;
  } else if (error.request) {
    // Network error
    errorResponse.message = 'Could not connect to Platform Copilot API';
  } else {
    // Other error
    errorResponse.details = error.message;
  }

  return formatErrorForPlatform(errorResponse, chatPlatform);
}

function formatErrorForPlatform(error, platform) {
  switch (platform) {
    case 'slack':
      return buildSlackErrorBlock(error);
    case 'discord':
      return buildDiscordErrorEmbed(error);
    case 'telegram':
      return buildTelegramErrorMessage(error);
    default:
      return error.message;
  }
}
```

---

## 📊 Monitoring & Analytics

### Application Insights Integration

```javascript
// telemetry.js
const appInsights = require('applicationinsights');

appInsights.setup(process.env.APPINSIGHTS_KEY).start();
const client = appInsights.defaultClient;

function trackChatInteraction(platform, userId, message, response) {
  client.trackEvent({
    name: 'ChatInteraction',
    properties: {
      platform,
      userId,
      messageLength: message.length,
      intentType: response.intentType,
      toolExecuted: response.toolExecuted,
      requiresFollowUp: response.requiresFollowUp,
      success: response.success
    },
    measurements: {
      responseTime: response.processingTimeMs
    }
  });
}

function trackError(platform, userId, error) {
  client.trackException({
    exception: error,
    properties: {
      platform,
      userId
    }
  });
}
```

---

## 🚀 Deployment Options

### Option 1: Cloud Functions (Serverless)

**AWS Lambda**:
```yaml
# serverless.yml
service: platform-copilot-slack

provider:
  name: aws
  runtime: nodejs18.x
  environment:
    PLATFORM_API_URL: ${env:PLATFORM_API_URL}
    SLACK_BOT_TOKEN: ${env:SLACK_BOT_TOKEN}

functions:
  slack-webhook:
    handler: slack-integration.handler
    events:
      - http:
          path: slack/events
          method: post
```

**Azure Functions**:
```json
{
  "bindings": [
    {
      "type": "httpTrigger",
      "direction": "in",
      "name": "req",
      "methods": ["post"],
      "route": "slack/events"
    },
    {
      "type": "http",
      "direction": "out",
      "name": "res"
    }
  ]
}
```

### Option 2: Container Deployment

```dockerfile
# Dockerfile
FROM node:18-alpine

WORKDIR /app

COPY package*.json ./
RUN npm ci --production

COPY . .

EXPOSE 3000

CMD ["node", "slack-integration.js"]
```

```bash
# Build and run
docker build -t platform-copilot-slack .
docker run -p 3000:3000 \
  -e SLACK_BOT_TOKEN=your-token \
  -e PLATFORM_API_URL=http://api:7001 \
  platform-copilot-slack
```

### Option 3: Kubernetes

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: platform-copilot-slack
spec:
  replicas: 3
  selector:
    matchLabels:
      app: platform-copilot-slack
  template:
    metadata:
      labels:
        app: platform-copilot-slack
    spec:
      containers:
      - name: slack-integration
        image: platform-copilot-slack:latest
        ports:
        - containerPort: 3000
        env:
        - name: SLACK_BOT_TOKEN
          valueFrom:
            secretKeyRef:
              name: slack-secrets
              key: bot-token
        - name: PLATFORM_API_URL
          value: "http://platform-copilot-api:7001"
---
apiVersion: v1
kind: Service
metadata:
  name: platform-copilot-slack
spec:
  selector:
    app: platform-copilot-slack
  ports:
  - port: 80
    targetPort: 3000
  type: LoadBalancer
```

---

## 🔒 Security Best Practices

### 1. Verify Webhook Signatures

**Slack**:
```javascript
const crypto = require('crypto');

function verifySlackRequest(req) {
  const timestamp = req.headers['x-slack-request-timestamp'];
  const signature = req.headers['x-slack-signature'];
  
  const time = Math.floor(new Date().getTime() / 1000);
  if (Math.abs(time - timestamp) > 300) {
    return false; // Request too old
  }
  
  const sigBasestring = `v0:${timestamp}:${req.rawBody}`;
  const mySignature = 'v0=' + crypto
    .createHmac('sha256', process.env.SLACK_SIGNING_SECRET)
    .update(sigBasestring)
    .digest('hex');
  
  return crypto.timingSafeEqual(
    Buffer.from(mySignature),
    Buffer.from(signature)
  );
}
```

### 2. Input Sanitization

```javascript
function sanitizeInput(text) {
  // Remove potentially dangerous characters
  return text
    .replace(/<script>/gi, '')
    .replace(/javascript:/gi, '')
    .trim()
    .substring(0, 4000); // Limit length
}
```

### 3. API Key Rotation

```javascript
// Use Azure Key Vault for secrets
const { SecretClient } = require('@azure/keyvault-secrets');

const client = new SecretClient(
  process.env.KEY_VAULT_URL,
  new DefaultAzureCredential()
);

async function getPlatformApiKey() {
  const secret = await client.getSecret('platform-api-key');
  return secret.value;
}
```

---

## 📚 Resources

- [Slack API Documentation](https://api.slack.com/)
- [Discord.js Guide](https://discordjs.guide/)
- [python-telegram-bot Documentation](https://docs.python-telegram-bot.org/)
- [Microsoft Bot Framework](https://dev.botframework.com/)
- [WebSocket API](https://developer.mozilla.org/en-US/docs/Web/API/WebSocket)

---

## 🎯 Success Metrics

Track these KPIs for chat integrations:

- **Adoption Rate**: % of team members using the chat bot
- **Query Volume**: Messages per day/week
- **Response Time**: Average time from query to response
- **Success Rate**: % of queries successfully handled
- **User Satisfaction**: Feedback scores or ratings
- **Follow-up Rate**: % of conversations requiring clarification

---

## 🆘 Troubleshooting

### Bot doesn't respond

1. Check bot is running: `curl http://localhost:3000/health`
2. Verify Platform API is accessible
3. Check webhook URL is correct
4. Review logs for errors
5. Test with curl to isolate issue

### Rate limiting errors

1. Implement exponential backoff
2. Add request queuing
3. Use Redis for distributed rate limiting
4. Contact platform support for increased limits

### Authentication failures

1. Verify tokens are correct
2. Check token expiration
3. Ensure proper scopes/permissions
4. Review OAuth configuration

---

**Status**: Ready for implementation  
**Next Steps**:
1. Choose your chat platform(s)
2. Follow platform-specific integration guide
3. Deploy integration service
4. Test with real users
5. Monitor and iterate

