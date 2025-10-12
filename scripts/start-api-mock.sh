#!/bin/bash

echo "🎭 Starting Platform API in MOCK MODE (no Azure OpenAI required)"
echo "=================================================="

# Set mock mode environment variable
export USE_MOCK_AI=true

cd "$(dirname "$0")/.."

echo "Starting API on port 7001..."
dotnet run --project src/Platform.Engineering.Copilot.API

