#!/bin/bash

echo "🚀 Starting Platform Engineering Copilot - Complete System"
echo "==========================================================="

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$SCRIPT_DIR/.."

# Set environment variable for mock mode
export INTELLIGENT_CHAT_MOCK_MODE=true

echo ""
echo "Step 1: Starting Platform API on port 7001..."
echo "----------------------------------------------"
cd "$PROJECT_ROOT"

# Start Platform API in background
dotnet run --project src/Platform.Engineering.Copilot.API > /tmp/platform-api.log 2>&1 &
API_PID=$!
echo "✅ Platform API started with PID: $API_PID"
echo "   Logs: tail -f /tmp/platform-api.log"

echo ""
echo "⏳ Waiting for Platform API to initialize (10 seconds)..."
sleep 10

echo ""
echo "Step 2: Starting Chat App Backend on port 5001..."
echo "--------------------------------------------------"
cd "$PROJECT_ROOT/src/Platform.Engineering.Copilot.Chat.App"

# Start Chat App backend in background
dotnet run > /tmp/chat-backend.log 2>&1 &
CHAT_BACKEND_PID=$!
echo "✅ Chat Backend started with PID: $CHAT_BACKEND_PID"
echo "   Logs: tail -f /tmp/chat-backend.log"

echo ""
echo "⏳ Waiting for Chat Backend to initialize (10 seconds)..."
sleep 10

echo ""
echo "Step 3: Starting Chat App Frontend (React)..."
echo "---------------------------------------------"
cd "$PROJECT_ROOT/src/Platform.Engineering.Copilot.Chat.App/ClientApp"

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo "📥 Installing npm dependencies..."
    npm install
fi

echo ""
echo "==========================================================="
echo "🎉 All Services Started!"
echo "==========================================================="
echo ""
echo "Platform API:    http://localhost:7001"
echo "Chat Backend:    http://localhost:5001"
echo "Chat Frontend:   http://localhost:3000 (will open automatically)"
echo ""
echo "Logs:"
echo "  Platform API:  tail -f /tmp/platform-api.log"
echo "  Chat Backend:  tail -f /tmp/chat-backend.log"
echo ""
echo "Press Ctrl+C to stop all services"
echo "==========================================================="
echo ""

# Start the frontend (this will block)
npm run start

# Cleanup function
cleanup() {
    echo ""
    echo "🛑 Shutting down all services..."
    echo "  Stopping Platform API (PID: $API_PID)..."
    kill $API_PID 2>/dev/null
    echo "  Stopping Chat Backend (PID: $CHAT_BACKEND_PID)..."
    kill $CHAT_BACKEND_PID 2>/dev/null
    echo "✅ All services stopped"
    exit 0
}

# Trap Ctrl+C
trap cleanup INT TERM

# Wait for frontend to exit
wait
