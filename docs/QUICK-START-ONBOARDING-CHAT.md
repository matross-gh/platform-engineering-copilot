# Quick Start: Onboarding Chat API

## 🚀 Get Started in 5 Minutes

### Prerequisites
- .NET 9 SDK installed
- Azure OpenAI endpoint configured in `appsettings.json`

---

## Step 1: Start the API

```bash
cd /Users/johnspinella/repos/platform-engineering-copilot/src/Platform.Engineering.Copilot.API
dotnet run
```

You should see:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:7001
```

---

## Step 2: Test with curl

### Example 1: Create Draft Request
```bash
curl -X POST http://localhost:7001/api/onboarding/chat/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "I need to onboard a mission called Phoenix for NAVWAR. I am CDR Johnson at johnson@navy.mil. We need AKS and Azure SQL."
  }'
```

**Expected**: Creates draft onboarding request with extracted entities

### Example 2: Check Status
```bash
curl -X POST http://localhost:7001/api/onboarding/chat/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "What is the status of request abc-123?"
  }'
```

**Expected**: Returns status of the specified request

### Example 3: Multi-Turn Conversation
```bash
# Turn 1
curl -X POST http://localhost:7001/api/onboarding/chat/conversation/my-session \
  -H "Content-Type: application/json" \
  -d '{"message": "I want to onboard a new mission"}'

# Turn 2
curl -X POST http://localhost:7001/api/onboarding/chat/conversation/my-session \
  -H "Content-Type: application/json" \
  -d '{"message": "It is called Operation Lighthouse"}'

# Turn 3
curl -X POST http://localhost:7001/api/onboarding/chat/conversation/my-session \
  -H "Content-Type: application/json" \
  -d '{"message": "Owner is LCDR Martinez at martinez@navy.mil"}'
```

**Expected**: Each turn builds on previous context

---

## Step 3: View Swagger UI

Open browser to: http://localhost:7001/swagger

- Browse all endpoints
- Try interactive requests
- See request/response schemas

---

## What's Happening Behind the Scenes?

```
Your Prompt
    ↓
OnboardingChatController
    ↓
IntelligentChatService
    ↓
Semantic Kernel (with Azure OpenAI)
    ↓
Auto Function Calling
    ↓
Entity Extraction (automatic)
    ↓
OnboardingPlugin Selection (automatic)
    ↓
IOnboardingService
    ↓
Database
    ↓
Response with structured data
```

**Magic**: Semantic Kernel automatically:
1. Understands your intent
2. Extracts entities (mission name, owner, services, etc.)
3. Invokes the correct plugin method
4. Returns structured response

---

## Sample Prompts to Try

### Simple
- "Create a mission for NAVWAR"
- "Show me pending requests"
- "What's the status of abc-123?"

### Detailed
- "I need to onboard a SECRET mission called Tactical Edge for SPAWAR. Owner is GS-14 Smith (smith@navy.mil). We need AKS, Azure SQL, and Redis. VNet should be 10.50.0.0/16."

### Conversational
- "I want to onboard a mission"
- "It's called Phoenix"
- "The owner is CDR Johnson"
- "We need AKS and PostgreSQL"

---

## Troubleshooting

### API doesn't start
**Check**: Azure OpenAI configuration in `appsettings.json`
```json
{
  "Gateway": {
    "AzureOpenAI": {
      "Endpoint": "https://your-endpoint.openai.azure.com/",
      "ApiKey": "your-key",
      "DeploymentName": "gpt-4o"
    }
  }
}
```

### "Prompt is required" error
**Check**: Request body has `prompt` field
```json
{"prompt": "Your message here"}
```

### No plugins found
**Check**: OnboardingPlugin is registered in `ServiceCollectionExtensions.cs` (it should be)

---

## Next Steps

1. **Read Full Documentation**: `/docs/ONBOARDING-CHAT-API-EXAMPLES.md`
2. **Understand Architecture**: `/docs/INTELLIGENT-CHAT-VS-ONBOARDING-CHAT-ANALYSIS.md`
3. **Review Implementation**: `/docs/IMPLEMENTATION-SUMMARY.md`
4. **Add Integration Tests**: Test entity extraction accuracy
5. **Integrate with UI**: Add chat widget to Admin Client

---

## Need Help?

- **Examples**: `/docs/ONBOARDING-CHAT-API-EXAMPLES.md` has 7 detailed examples
- **Analysis**: `/docs/INTELLIGENT-CHAT-VS-ONBOARDING-CHAT-ANALYSIS.md` explains the architecture
- **Summary**: `/docs/IMPLEMENTATION-SUMMARY.md` shows what was built

---

## Success Indicators

✅ API starts without errors  
✅ Swagger UI loads at http://localhost:7001/swagger  
✅ Simple prompts return responses  
✅ Entities are extracted correctly  
✅ Multi-turn conversations maintain context  

**All working? You're ready to go!** 🎉
