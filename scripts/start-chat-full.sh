#!/bin/bash

echo "🚀 Starting Platform Chat App (Full Stack)..."

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$SCRIPT_DIR/.."

# Start the .NET backend in the background
echo "📦 Starting Chat App Backend on port 5001..."
cd "$PROJECT_ROOT/src/Platform.Engineering.Copilot.Chat.App"

# Set environment variable for mock mode
export INTELLIGENT_CHAT_MOCK_MODE=true

# Start backend in background
dotnet run &
BACKEND_PID=$!

echo "✅ Backend started with PID: $BACKEND_PID"
echo "⏳ Waiting for backend to initialize..."
sleep 5

# Start the React frontend
echo "📦 Starting Chat App Frontend..."
cd "$PROJECT_ROOT/src/Platform.Engineering.Copilot.Chat.App/ClientApp"

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo "📥 Installing dependencies..."
    npm install
fi

# Start the frontend
echo "✅ Starting React development server..."
npm run start

# Cleanup function
cleanup() {
    echo ""
    echo "🛑 Shutting down Chat App..."
    kill $BACKEND_PID 2>/dev/null
    exit 0
}

# Trap Ctrl+C
trap cleanup INT TERM

# Wait for frontend to exit
wait
