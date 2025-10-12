#!/bin/bash

# Start Admin API and Admin Client
echo "🚀 Starting Platform Admin System..."

# Check if Admin API is already running on port 7002
if lsof -Pi :7002 -sTCP:LISTEN -t >/dev/null ; then
    echo "✅ API already running on port 7001"
else
    echo "📦 Starting API on port 7001..."
    cd /Users/johnspinella/platform-mcp-supervisor/src/Platform.Engineering.Copilot.API
    nohup dotnet run > /tmp/admin-api.log 2>&1 &
    echo "   API PID: $!"
    sleep 5
fi

echo ""
echo "==================================================="
echo "🎉 Platform API System Started!"
echo "==================================================="
echo "API:    http://localhost:7001"
echo ""
echo "Logs:"
echo "  API:    tail -f /tmp/admin-api.log"
echo "==================================================="
