# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file and all project files
COPY Platform.Engineering.Copilot.sln ./
COPY src/Platform.Engineering.Copilot.API/*.csproj ./src/Platform.Engineering.Copilot.API/
COPY src/Platform.Engineering.Copilot.Core/*.csproj ./src/Platform.Engineering.Copilot.Core/
COPY src/Platform.Engineering.Copilot.Data/*.csproj ./src/Platform.Engineering.Copilot.Data/

# Restore dependencies
RUN dotnet restore ./src/Platform.Engineering.Copilot.API/Platform.Engineering.Copilot.API.csproj

# Copy all source code
COPY src/Platform.Engineering.Copilot.API/. ./src/Platform.Engineering.Copilot.API/
COPY src/Platform.Engineering.Copilot.Core/. ./src/Platform.Engineering.Copilot.Core/
COPY src/Platform.Engineering.Copilot.Data/. ./src/Platform.Engineering.Copilot.Data/

# Build and publish
WORKDIR /src/src/Platform.Engineering.Copilot.API
RUN dotnet publish Platform.Engineering.Copilot.API.csproj \
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

EXPOSE 7001

ENTRYPOINT ["dotnet", "Platform.Engineering.Copilot.API.dll"]
