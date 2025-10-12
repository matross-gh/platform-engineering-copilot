# 🚀 Quick Start Guide - Chat App with Onboarding

## Problem You Were Having

The Chat App was showing these errors:
- ❌ `ERR_CONNECTION_REFUSED` to `localhost:5001/chathub`
- ❌ SignalR connection failed
- ❌ Chat App couldn't send messages

**Root Cause**: The Chat App needs **TWO** services running:
1. **Platform API** (port 7001) - The backend API with intelligent chat
2. **Chat App Backend** (port 5001) - The SignalR hub for real-time chat

You were only running the React frontend, which couldn't connect to either service.

---

## ✅ Solution: Use the New Startup Script

### Option 1: Start Everything at Once (RECOMMENDED)

```bash
cd /Users/johnspinella/repos/platform-engineering-copilot
./scripts/start-all.sh
```

This will:
1. ✅ Start Platform API on port 7001
2. ✅ Start Chat App Backend on port 5001  
3. ✅ Start Chat App Frontend on port 3000
4. ✅ Open your browser automatically

**Then test with**: "I need to onboard a mission for NAVWAR"

### Option 2: Start Services Separately

**Terminal 1 - Platform API:**
```bash
cd /Users/johnspinella/repos/platform-engineering-copilot
export INTELLIGENT_CHAT_MOCK_MODE=true
dotnet run --project src/Platform.Engineering.Copilot.API
```

**Terminal 2 - Chat App Backend:**
```bash
cd /Users/johnspinella/repos/platform-engineering-copilot/src/Platform.Engineering.Copilot.Chat.App
export INTELLIGENT_CHAT_MOCK_MODE=true
dotnet run
```

**Terminal 3 - Chat App Frontend:**
```bash
cd /Users/johnspinella/repos/platform-engineering-copilot/src/Platform.Engineering.Copilot.Chat.App/ClientApp
npm start
```

---

## 🎯 What the Mock Mode Does

Since you don't have Azure OpenAI credentials, I added a **mock mode** that:

✅ Returns simulated AI responses without calling Azure OpenAI  
✅ Detects onboarding-related messages  
✅ Provides realistic test responses  
✅ Lets you test the full flow immediately  

When `INTELLIGENT_CHAT_MOCK_MODE=true`:
- **Onboarding messages** → Mock onboarding response
- **Other messages** → Generic helpful response
- **No Azure OpenAI calls** → No hanging or timeouts

---

## 📝 Test Messages

Once everything is running, try these in the Chat App:

### Onboarding Tests
```
I need to onboard a mission for NAVWAR
Create a SECRET mission called Phoenix
What's the status of request abc-123?
```

### Expected Mock Response
```
✅ Mock Onboarding Response

I can help you with mission onboarding for NAVWAR.

Based on your message, I've identified:
- Organization: NAVWAR
- Intent: Create new mission onboarding request

To proceed, I'll need:
1. Mission Name
2. Classification Level (UNCLASS, SECRET, TS, TS/SCI)
3. Mission Owner (Name and Email)
4. Required Azure Services

Would you like to provide these details?

💡 Suggestions:
- Specify classification level
- List required Azure services
- Provide mission owner contact
```

---

## 🔍 Troubleshooting

### Issue: Platform API port 7001 already in use
```bash
# Find and kill the process
lsof -ti:7001 | xargs kill -9
```

### Issue: Chat Backend port 5001 already in use
```bash
# Find and kill the process
lsof -ti:5001 | xargs kill -9
```

### Issue: React frontend port 3000 already in use
```bash
# Find and kill the process
lsof -ti:3000 | xargs kill -9
```

### Check if services are running
```bash
# Platform API
curl http://localhost:7001/health

# Chat Backend
curl http://localhost:5001/health

# Check logs
tail -f /tmp/platform-api.log
tail -f /tmp/chat-backend.log
```

---

## 🎉 Success Indicators

When everything is working, you should see:

✅ **Platform API logs** showing:
```
Now listening on: http://localhost:7001
Database initialized successfully
```

✅ **Chat Backend logs** showing:
```
Now listening on: http://localhost:5001
SignalR hub registered
```

✅ **Browser** opens to `http://localhost:3000`

✅ **Chat App** shows "Connected" status

✅ **Test message** gets a response without errors

---

## 🔄 Switching to Real Azure OpenAI

When you're ready to use real Azure OpenAI instead of mock responses:

1. **Stop all services** (Ctrl+C in the terminal running `start-all.sh`)

2. **Unset mock mode**:
   ```bash
   unset INTELLIGENT_CHAT_MOCK_MODE
   ```

3. **Configure Azure OpenAI** in `appsettings.json`:
   ```json
   "AzureOpenAI": {
     "ApiKey": "your-real-api-key",
     "Endpoint": "https://your-endpoint.openai.azure.com/",
     "DeploymentName": "gpt-4o"
   }
   ```

4. **Restart** with `./scripts/start-all.sh`

---

## 📚 Architecture

```
┌─────────────────────────────────────────────────────┐
│  Browser (localhost:3000)                           │
│    - React Chat UI                                  │
│    - SignalR client                                 │
└─────────────────────────────────────────────────────┘
                      ↓ WebSocket
┌─────────────────────────────────────────────────────┐
│  Chat App Backend (localhost:5001)                  │
│    - .NET 9 SignalR Hub                             │
│    - ChatService.cs                                 │
└─────────────────────────────────────────────────────┘
                      ↓ HTTP
┌─────────────────────────────────────────────────────┐
│  Platform API (localhost:7001)                      │
│    - ChatController.cs                              │
│    - /api/chat/intelligent-query                    │
└─────────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────────┐
│  IntelligentChatService                             │
│    - Mock Mode OR Azure OpenAI                      │
│    - Semantic Kernel                                │
│    - OnboardingPlugin                               │
└─────────────────────────────────────────────────────┘
```

---

## 🎯 Summary

**Before**: 
- ❌ Only React frontend running
- ❌ No backend services
- ❌ Connection refused errors

**After**: 
- ✅ All 3 services running
- ✅ Mock mode enabled (no Azure OpenAI needed)
- ✅ Full chat functionality works
- ✅ Can test onboarding immediately

**Next Steps**:
1. Run `./scripts/start-all.sh`
2. Wait for browser to open
3. Type "I need to onboard a mission for NAVWAR"
4. Get mock response and test the flow!

🚀 **You're ready to go!**
