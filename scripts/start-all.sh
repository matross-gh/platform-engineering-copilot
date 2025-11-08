#!/bin/bash

echo "🚀 Starting Platform Engineering Copilot - Complete System"
echo "==========================================================="

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$SCRIPT_DIR/.."

# Set environment variable for mock mode
export INTELLIGENT_CHAT_MOCK_MODE=true

echo ""
echo "Step 1: Starting MCP Server (HTTP mode) on port 5100..."
echo "-------------------------------------------------------"
cd "$PROJECT_ROOT"

# Start MCP server in HTTP mode
dotnet run --project src/Platform.Engineering.Copilot.Mcp -- --http > /tmp/mcp-http.log 2>&1 &
MCP_PID=$!
echo "✅ MCP server started with PID: $MCP_PID"
echo "   Logs: tail -f /tmp/mcp-http.log"

echo ""
echo "⏳ Waiting for MCP server to initialize (10 seconds)..."
sleep 10

echo ""
echo "Step 2: Starting Chat App Backend on port 5001..."
echo "--------------------------------------------------"
cd "$PROJECT_ROOT/src/Platform.Engineering.Copilot.Chat"

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
cd "$PROJECT_ROOT/src/Platform.Engineering.Copilot.Chat/ClientApp"

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
echo "MCP Server:      http://localhost:5100"
echo "Chat Backend:    http://localhost:5001"
echo "Chat Frontend:   http://localhost:3000 (will open automatically)"
echo ""
echo "Logs:"
echo "  MCP Server:    tail -f /tmp/mcp-http.log"
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
    echo "  Stopping MCP server (PID: $MCP_PID)..."
    kill $MCP_PID 2>/dev/null
    echo "  Stopping Chat Backend (PID: $CHAT_BACKEND_PID)..."
    kill $CHAT_BACKEND_PID 2>/dev/null
    echo "✅ All services stopped"
    exit 0
}

# Trap Ctrl+C
trap cleanup INT TERM

# Wait for frontend to exit
wait
