# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install Node.js and npm using official binary distribution
RUN apt-get update && \
    apt-get install -y curl xz-utils && \
    curl -fsSL https://nodejs.org/dist/v20.18.1/node-v20.18.1-linux-arm64.tar.xz -o node.tar.xz && \
    tar -xf node.tar.xz -C /usr/local --strip-components=1 && \
    rm node.tar.xz && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Verify installation
RUN node --version && npm --version

WORKDIR /src

# Copy solution file and all project files
COPY Platform.Engineering.Copilot.sln ./
COPY src/Platform.Engineering.Copilot.Admin.Client/*.csproj ./src/Platform.Engineering.Copilot.Admin.Client/
COPY src/Platform.Engineering.Copilot.Core/*.csproj ./src/Platform.Engineering.Copilot.Core/

# Restore dependencies
RUN dotnet restore ./src/Platform.Engineering.Copilot.Admin.Client/Platform.Engineering.Copilot.Admin.Client.csproj

# Copy all source code
COPY src/Platform.Engineering.Copilot.Admin.Client/. ./src/Platform.Engineering.Copilot.Admin.Client/
COPY src/Platform.Engineering.Copilot.Core/. ./src/Platform.Engineering.Copilot.Core/

# Build and publish
WORKDIR /src/src/Platform.Engineering.Copilot.Admin.Client
RUN dotnet publish Platform.Engineering.Copilot.Admin.Client.csproj \
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

EXPOSE 5003

ENTRYPOINT ["dotnet", "Platform.Engineering.Copilot.Admin.Client.dll"]
