# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install Node.js 20.x
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Copy solution file and all project files
COPY Platform.Engineering.Copilot.sln ./
COPY src/Platform.Engineering.Copilot.Chat/*.csproj ./src/Platform.Engineering.Copilot.Chat/
COPY src/Platform.Engineering.Copilot.Core/*.csproj ./src/Platform.Engineering.Copilot.Core/
COPY src/Platform.Engineering.Copilot.Compliance.Agent/*.csproj ./src/Platform.Engineering.Copilot.Compliance.Agent/
COPY src/Platform.Engineering.Copilot.Infrastructure.Agent/*.csproj ./src/Platform.Engineering.Copilot.Infrastructure.Agent/
COPY src/Platform.Engineering.Copilot.CostManagement.Agent/*.csproj ./src/Platform.Engineering.Copilot.CostManagement.Agent/
COPY src/Platform.Engineering.Copilot.Environment.Agent/*.csproj ./src/Platform.Engineering.Copilot.Environment.Agent/
COPY src/Platform.Engineering.Copilot.Discovery.Agent/*.csproj ./src/Platform.Engineering.Copilot.Discovery.Agent/
COPY src/Platform.Engineering.Copilot.Security.Agent/*.csproj ./src/Platform.Engineering.Copilot.Security.Agent/
COPY src/Platform.Engineering.Copilot.Document.Agent/*.csproj ./src/Platform.Engineering.Copilot.Document.Agent/

# Restore dependencies
RUN dotnet restore ./src/Platform.Engineering.Copilot.Chat/Platform.Engineering.Copilot.Chat.csproj

# Copy all source code
COPY src/Platform.Engineering.Copilot.Chat/. ./src/Platform.Engineering.Copilot.Chat/
COPY src/Platform.Engineering.Copilot.Core/. ./src/Platform.Engineering.Copilot.Core/
COPY src/Platform.Engineering.Copilot.Compliance.Agent/. ./src/Platform.Engineering.Copilot.Compliance.Agent/
COPY src/Platform.Engineering.Copilot.Infrastructure.Agent/. ./src/Platform.Engineering.Copilot.Infrastructure.Agent/
COPY src/Platform.Engineering.Copilot.CostManagement.Agent/. ./src/Platform.Engineering.Copilot.CostManagement.Agent/
COPY src/Platform.Engineering.Copilot.Environment.Agent/. ./src/Platform.Engineering.Copilot.Environment.Agent/
COPY src/Platform.Engineering.Copilot.Discovery.Agent/. ./src/Platform.Engineering.Copilot.Discovery.Agent/
COPY src/Platform.Engineering.Copilot.Security.Agent/. ./src/Platform.Engineering.Copilot.Security.Agent/
COPY src/Platform.Engineering.Copilot.Document.Agent/. ./src/Platform.Engineering.Copilot.Document.Agent/

# Build and publish
WORKDIR /src/src/Platform.Engineering.Copilot.Chat
RUN dotnet publish Platform.Engineering.Copilot.Chat.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

EXPOSE 5001

ENTRYPOINT ["dotnet", "Platform.Engineering.Copilot.Chat.App.dll"]
