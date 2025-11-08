# Deployment Guide

**Last Updated:** October 29, 2025

This guide covers all deployment options for the Platform Engineering Copilot MCP Server and associated services, including Docker, Kubernetes, and cloud deployments.

## 📋 Table of Contents

1. [Docker Deployment](#docker-deployment)
2. [Kubernetes Deployment](#kubernetes-deployment)
3. [Azure Deployment](#azure-deployment)
4. [AI Client Integration](#ai-client-integration)
5. [Production Configuration](#production-configuration)
6. [Monitoring & Observability](#monitoring--observability)
7. [Security Considerations](#security-considerations)

---

## 🐳 Docker Deployment

### Architecture Overview

The Docker setup includes multiple configuration options:

**Essentials Configuration (MCP Server Only)**:
- **MCP Server**: Multi-Agent Orchestrator (Port 5100)
- **SQL Server**: Database backend (Port 1433)

**Full Configuration (All Services)**:
- **MCP Server**: Multi-Agent Orchestrator (Port 5100)
- **Platform Chat**: Web chat interface (Port 5001)
- **Admin API**: Administrative backend (Port 5002)
- **Admin Client**: Admin web console (Port 5003)
- **SQL Server**: Database (Port 1433)
- **Nginx**: Reverse proxy (Port 80/443) - Optional
- **Redis**: Caching service (Port 6379) - Optional

### Quick Docker Start

#### Prerequisites

- Docker Desktop installed and running (or Docker + Docker Compose on Linux)
- Git (to clone the repository)
- At least 8GB RAM available for Docker
- Azure subscription (for Azure resources)
- Azure CLI (for authentication)

#### 1. Clone and Setup

```bash
git clone https://github.com/azurenoops/platform-engineering-copilot.git
cd platform-engineering-copilot

# Copy environment template
cp .env.example .env
```

#### 2. Configure Environment

Edit `.env` file with your Azure credentials:

```bash
# Azure Configuration
AZURE_SUBSCRIPTION_ID=your-subscription-id
AZURE_TENANT_ID=your-tenant-id
AZURE_CLOUD_ENVIRONMENT=AzureGovernment  # or AzureCloud
AZURE_USE_MANAGED_IDENTITY=false
AZURE_ENABLED=true

# Azure OpenAI Configuration
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_ENDPOINT=https://your-endpoint.openai.azure.us/
AZURE_OPENAI_DEPLOYMENT=gpt-4o
AZURE_OPENAI_CHAT_DEPLOYMENT=gpt-4o
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-ada-002

# Database
SA_PASSWORD=YourStrong@Passw0rd123!

# NIST Controls (Optional)
NIST_CONTROLS_BASE_URL=https://raw.githubusercontent.com/usnistgov/oscal-content/main/nist.gov/SP800-53/rev5/json
```

**📖 See [DOCKER.md](../DOCKER.md) for complete environment variable reference**

#### 3. Start Services

**Option 1: MCP Server Only (Recommended for AI Client Development)**
```bash
# Start essentials
docker compose -f docker-compose.essentials.yml up -d

# With development hot reload
docker compose -f docker-compose.essentials.yml -f docker-compose.dev.yml up -d

# With production settings
docker compose -f docker-compose.essentials.yml -f docker-compose.prod.yml up -d
```

**Option 2: All Services (Complete Platform)**
```bash
# Start all services
docker compose up -d

# Development with hot reload
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Production with scaling
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Production with reverse proxy
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile proxy up -d
```

**📖 See [DOCKER-COMPOSE-GUIDE.md](../DOCKER-COMPOSE-GUIDE.md) for all configuration options**

#### 4. Verify Deployment

```bash
# Check service status
docker compose ps

# View logs
docker compose logs -f platform-mcp
docker compose logs -f sqlserver

# Test MCP Server health
curl http://localhost:5100/health

# Test all services (if running full platform)
curl http://localhost:5001/health  # Chat
curl http://localhost:5002/health  # Admin API
curl http://localhost:5003/health  # Admin Client
```

### Docker Compose Configurations

The platform now runs across four ASP.NET Core services plus optional infrastructure containers. The base compose file builds each service from the `src/` directory and binds the expected local development ports.

#### Base (`docker-compose.yml`)

```yaml
version: "3.9"

services:
  platform-mcp:
    build:
      context: .
      dockerfile: src/Platform.Engineering.Copilot.Mcp/Dockerfile
    ports:
      - "5100:5100"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://0.0.0.0:5100
      - ASPNETCORE_HTTP_PORTS=5100
      - ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=plaform-engineering-copilotDb;User=sa;Password=plaform-engineering-copilotDB123!;TrustServerCertificate=true;MultipleActiveResultSets=true;Encrypt=false
    depends_on:
      sqlserver:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:5100/health || exit 1"]

  platform-chat:
    build:
      context: .
      dockerfile: src/Platform.Engineering.Copilot.Chat/Dockerfile
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://0.0.0.0:5001
      - ASPNETCORE_HTTP_PORTS=5001
      - McpServer__BaseUrl=http://platform-mcp:5100
    depends_on:
      platform-mcp:
        condition: service_healthy
      sqlserver:
        condition: service_healthy

  admin-api:
    build:
      context: .
      dockerfile: src/Platform.Engineering.Copilot.Admin.API/Dockerfile
    ports:
      - "5002:5002"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://0.0.0.0:5002
      - ASPNETCORE_HTTP_PORTS=5002
      - ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=plaform-engineering-copilotAdminDb;User=sa;Password=plaform-engineering-copilotDB123!;TrustServerCertificate=true;MultipleActiveResultSets=true;Encrypt=false
    depends_on:
      sqlserver:
        condition: service_healthy

  admin-client:
    build:
      context: .
      dockerfile: src/Platform.Engineering.Copilot.Admin.Client/Dockerfile
    ports:
      - "5003:5003"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://0.0.0.0:5003
      - ASPNETCORE_HTTP_PORTS=5003
      - AdminApi__BaseUrl=http://admin-api:5002
    depends_on:
      admin-api:
        condition: service_healthy

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports:
      - "1433:1433"
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=plaform-engineering-copilotDB123!
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P plaform-engineering-copilotDB123! -C -Q 'SELECT 1' || exit 1"]

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    command: redis-server /etc/redis/redis.conf
    profiles:
      - cache

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
    profiles:
      - proxy
```

#### Production Overrides (`docker-compose.prod.yml`)

```yaml
services:
  platform-mcp:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://0.0.0.0:5100
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '1.0'
          memory: 1G

  platform-chat:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    deploy:
      replicas: 1

  admin-api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production

  admin-client:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production

  sqlserver:
    environment:
      - MSSQL_PID=Standard

  nginx:
    volumes:
      - ./nginx/nginx.prod.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro

  redis:
    deploy:
      resources:
        limits:
          memory: 512M
```

> Apply production settings with `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d` to layer environment-specific knobs on top of the base file.

#### Development Overrides (`docker-compose.dev.yml`)

Use the development override when you want bind-mounted source code or extra tooling such as Adminer:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up
```

### Docker Management Commands

```bash
# View running containers
docker compose ps

# View logs
docker compose logs -f platform-mcp

# Restart specific service
docker compose restart platform-chat

# Update and restart services
docker compose pull
docker compose up -d

# Stop all services
docker compose down

# Stop and remove volumes (destructive)
docker compose down -v

# Build specific service
docker compose build admin-api

# Redeploy a single service after rebuilding
docker compose up -d admin-api

# Scale services
docker compose up -d --scale platform-mcp=3
```

---

## ☸️ Kubernetes Deployment

### Prerequisites

- Kubernetes cluster (AKS, EKS, GKE, or local)
- kubectl configured
- Helm 3.x installed

### 1. Namespace Setup

```yaml
# namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: platform-supervisor
  labels:
    name: platform-supervisor
```

```bash
kubectl apply -f namespace.yaml
```

### 2. Secrets Configuration

```bash
# Create secrets
kubectl create secret generic azure-config \
  --from-literal=subscription-id=$AZURE_SUBSCRIPTION_ID \
  --from-literal=tenant-id=$AZURE_TENANT_ID \
  --from-literal=environment=$AZURE_ENVIRONMENT \
  -n platform-supervisor

kubectl create secret generic database-config \
  --from-literal=connection-string=$CONNECTION_STRING \
  -n platform-supervisor
```

### 3. Deploy with Helm

```bash
# Add Helm repository (if you publish to a Helm repo)
helm repo add platform-supervisor https://your-helm-repo.com
helm repo update

# Install with Helm
helm install platform-supervisor ./helm/platform-supervisor \
  --namespace platform-supervisor \
  --values values.prod.yaml
```

### 4. Manual Kubernetes Deployment

#### Platform MCP Deployment

```yaml
# platform-mcp-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: platform-mcp
  namespace: platform-supervisor
spec:
  replicas: 3
  selector:
    matchLabels:
      app: platform-mcp
  template:
    metadata:
      labels:
        app: platform-mcp
    spec:
      containers:
      - name: platform-mcp
        image: your-registry/platform-mcp:latest
        ports:
        - containerPort: 5100
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: database-config
              key: connection-string
        - name: Azure__SubscriptionId
          valueFrom:
            secretKeyRef:
              name: azure-config
              key: subscription-id
        - name: ASPNETCORE_URLS
          value: "http://0.0.0.0:5100"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5100
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5100
          initialDelaySeconds: 5
          periodSeconds: 5

---
apiVersion: v1
kind: Service
metadata:
  name: platform-mcp-service
  namespace: platform-supervisor
spec:
  selector:
    app: platform-mcp
  ports:
  - port: 5100
    targetPort: 5100
  type: ClusterIP
```

> Create similar manifests for `platform-chat` (port 5001), `admin-api` (port 5002), and `admin-client` (port 5003). Reference the MCP service (`platform-mcp-service`) from downstream deployments via environment variables such as `McpServer__BaseUrl=http://platform-mcp-service:5100`.

#### Ingress Configuration

```yaml
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: platform-supervisor-ingress
  namespace: platform-supervisor
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - platform.yourdomain.com
    secretName: platform-supervisor-tls
  rules:
  - host: platform.yourdomain.com
    http:
      paths:
      - path: /mcp
        pathType: Prefix
        backend:
          service:
            name: platform-mcp-service
            port:
              number: 5100
      - path: /chat
        pathType: Prefix
        backend:
          service:
            name: platform-chat-service
            port:
              number: 5001
      - path: /admin
        pathType: Prefix
        backend:
          service:
            name: admin-client-service
            port:
              number: 5003
```

---

## ☁️ Azure Deployment

### Azure Container Instances (ACI)

```bash
# Create resource group
az group create --name platform-supervisor-rg --location usgovvirginia

# Deploy MCP orchestrator
az container create \
  --resource-group platform-supervisor-rg \
  --name platform-mcp \
  --image your-registry/platform-mcp:latest \
  --ports 5100 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:5100 \
  --secure-environment-variables \
    ConnectionStrings__DefaultConnection=$CONNECTION_STRING \
  --cpu 2 \
  --memory 4

# Deploy chat experience (optional)
az container create \
  --resource-group platform-supervisor-rg \
  --name platform-chat \
  --image your-registry/platform-chat:latest \
  --ports 5001 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:5001 \
    McpServer__BaseUrl=http://platform-mcp:5100 \
  --cpu 1 \
  --memory 2
```

### Azure Kubernetes Service (AKS)

```bash
# Create AKS cluster
az aks create \
  --resource-group platform-supervisor-rg \
  --name platform-supervisor-aks \
  --node-count 3 \
  --node-vm-size Standard_D2s_v3 \
  --enable-addons monitoring \
  --generate-ssh-keys

# Get credentials
az aks get-credentials --resource-group platform-supervisor-rg --name platform-supervisor-aks

# Deploy to AKS
kubectl apply -f k8s/
```

### Azure Container Apps

```bash
# Create Container Apps environment
az containerapp env create \
  --name platform-supervisor-env \
  --resource-group platform-supervisor-rg \
  --location usgovvirginia

# Deploy MCP container app
az containerapp create \
  --name platform-mcp \
  --resource-group platform-supervisor-rg \
  --environment platform-supervisor-env \
  --image your-registry/platform-mcp:latest \
  --target-port 5100 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 10 \
  --cpu 1.0 \
  --memory 2Gi

# Deploy web workloads
az containerapp create \
  --name platform-chat \
  --resource-group platform-supervisor-rg \
  --environment platform-supervisor-env \
  --image your-registry/platform-chat:latest \
  --target-port 5001 \
  --ingress external \
  --env-vars McpServer__BaseUrl=https://platform-mcp.yourdomain.com
```

---

## 🔧 Production Configuration

### Environment Variables

```bash
# Production environment variables
export ASPNETCORE_ENVIRONMENT=Production
export AZURE_SUBSCRIPTION_ID=your-subscription-id
export AZURE_TENANT_ID=your-tenant-id
export AZURE_ENVIRONMENT=AzureUSGovernment
export CONNECTION_STRING="Server=prod-sql-server;Database=PlatformCopilot;..."

# Public endpoints
export MCP_BASE_URL="https://mcp.yourdomain.com"
export CHAT_BASE_URL="https://chat.yourdomain.com"
export ADMIN_API_BASE_URL="https://admin-api.yourdomain.com"
export ADMIN_CLIENT_BASE_URL="https://admin.yourdomain.com"
export JWT_SECRET=your-jwt-secret
export ENCRYPTION_KEY=your-encryption-key

# Performance
export REDIS_CONNECTION_STRING=your-redis-connection
export MAX_CONCURRENT_REQUESTS=100
export REQUEST_TIMEOUT_SECONDS=30
```

### Application Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Azure": {
    "SubscriptionId": "#{AZURE_SUBSCRIPTION_ID}#",
    "TenantId": "#{AZURE_TENANT_ID}#",
    "CloudEnvironment": "AzureUSGovernment"
  },
  "McpServer": {
    "PublicBaseUrl": "https://mcp.yourdomain.com",
    "EnableStdioBridge": true
  },
  "Chat": {
    "BaseUrl": "https://chat.yourdomain.com",
    "EnableCaching": true,
    "CacheExpirationMinutes": 15
  },
  "Admin": {
    "ApiBaseUrl": "https://admin-api.yourdomain.com",
    "ClientBaseUrl": "https://admin.yourdomain.com"
  },
  "Security": {
    "EnableHttpsRedirection": true,
    "EnableHsts": true,
    "RequireHttps": true
  },
  "Performance": {
    "MaxConcurrentRequests": 100,
    "RequestTimeoutSeconds": 30,
    "EnableCompression": true
  }
}
```

---

## 📊 Monitoring & Observability

### Application Insights Integration

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;IngestionEndpoint=..."
  },
  "Logging": {
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

### Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddRedis(redisConnectionString)
    .AddAzureKeyVault(options => { ... });

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Prometheus Metrics

```yaml
# prometheus-config.yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'platform-supervisor'
    static_configs:
      - targets: ['platform-mcp:5100']
    metrics_path: /metrics
    scrape_interval: 5s
```

---

## 🔒 Security Considerations

### HTTPS Configuration

```nginx
# nginx.conf for production
server {
    listen 443 ssl http2;
    server_name yourdomain.com;
    
    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES128-GCM-SHA256:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    
    add_header Strict-Transport-Security "max-age=63072000" always;
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    
    location /chat/ {
        proxy_pass http://platform-chat:5001/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
    
    location /mcp/ {
        proxy_pass http://platform-mcp:5100/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location /admin/ {
        proxy_pass http://admin-client:5003/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### Secrets Management

```bash
# Using Azure Key Vault
az keyvault create --name platform-supervisor-kv --resource-group platform-supervisor-rg

# Store secrets
az keyvault secret set --vault-name platform-supervisor-kv --name "ConnectionString" --value "$CONNECTION_STRING"
az keyvault secret set --vault-name platform-supervisor-kv --name "AzureSubscriptionId" --value "$AZURE_SUBSCRIPTION_ID"
```

### Network Security

```yaml
# network-policy.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: platform-supervisor-network-policy
  namespace: platform-supervisor
spec:
  podSelector:
    matchLabels:
      app: platform-mcp
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - podSelector:
        matchLabels:
          app: platform-chat
    ports:
    - protocol: TCP
      port: 5001
  egress:
  - to: []
    ports:
    - protocol: TCP
      port: 443  # HTTPS
    - protocol: TCP
      port: 1433 # SQL Server
```

---

## 🚀 Deployment Automation

### GitHub Actions

```yaml
# .github/workflows/deploy.yml
name: Deploy to Production

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Login to Azure
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
    
    - name: Build and push Docker images
      run: |
        docker build -t ${{ secrets.REGISTRY_URL }}/platform-mcp:${{ github.sha }} -f src/Platform.Engineering.Copilot.Mcp/Dockerfile .
        docker push ${{ secrets.REGISTRY_URL }}/platform-mcp:${{ github.sha }}
        docker build -t ${{ secrets.REGISTRY_URL }}/platform-chat:${{ github.sha }} -f src/Platform.Engineering.Copilot.Chat/Dockerfile .
        docker push ${{ secrets.REGISTRY_URL }}/platform-chat:${{ github.sha }}
        docker build -t ${{ secrets.REGISTRY_URL }}/admin-api:${{ github.sha }} -f src/Platform.Engineering.Copilot.Admin.API/Dockerfile .
        docker push ${{ secrets.REGISTRY_URL }}/admin-api:${{ github.sha }}
        docker build -t ${{ secrets.REGISTRY_URL }}/admin-client:${{ github.sha }} -f src/Platform.Engineering.Copilot.Admin.Client/Dockerfile .
        docker push ${{ secrets.REGISTRY_URL }}/admin-client:${{ github.sha }}
    
    - name: Deploy to AKS
      run: |
        az aks get-credentials --resource-group platform-supervisor-rg --name platform-supervisor-aks
        kubectl set image deployment/platform-mcp platform-mcp=${{ secrets.REGISTRY_URL }}/platform-mcp:${{ github.sha }}
        kubectl set image deployment/platform-chat platform-chat=${{ secrets.REGISTRY_URL }}/platform-chat:${{ github.sha }}
        kubectl set image deployment/admin-api admin-api=${{ secrets.REGISTRY_URL }}/admin-api:${{ github.sha }}
        kubectl set image deployment/admin-client admin-client=${{ secrets.REGISTRY_URL }}/admin-client:${{ github.sha }}
```

### Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

stages:
- stage: Build
  jobs:
  - job: Build
    steps:
    - task: Docker@2
      displayName: 'Build and Push MCP Image'
      inputs:
        command: 'buildAndPush'
        repository: 'platform-mcp'
        dockerfile: 'src/Platform.Engineering.Copilot.Mcp/Dockerfile'
        tags: '$(Build.BuildId)'

    - task: Docker@2
      displayName: 'Build and Push Chat Image'
      inputs:
        command: 'buildAndPush'
        repository: 'platform-chat'
        dockerfile: 'src/Platform.Engineering.Copilot.Chat/Dockerfile'
        tags: '$(Build.BuildId)'

    - task: Docker@2
      displayName: 'Build and Push Admin API Image'
      inputs:
        command: 'buildAndPush'
        repository: 'admin-api'
        dockerfile: 'src/Platform.Engineering.Copilot.Admin.API/Dockerfile'
        tags: '$(Build.BuildId)'

    - task: Docker@2
      displayName: 'Build and Push Admin Client Image'
      inputs:
        command: 'buildAndPush'
        repository: 'admin-client'
        dockerfile: 'src/Platform.Engineering.Copilot.Admin.Client/Dockerfile'
        tags: '$(Build.BuildId)'

- stage: Deploy
  jobs:
  - deployment: Deploy
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: KubernetesManifest@0
            displayName: 'Deploy to Kubernetes'
            inputs:
              action: 'deploy'
              manifests: 'k8s/*.yaml'
```

---

## 📝 Troubleshooting

### Common Deployment Issues

#### Container Won't Start

```bash
# Check container logs
docker logs platform-mcp

# Check resource usage
docker stats

# Verify environment variables
docker exec platform-mcp env | grep ASPNETCORE
```

#### Service Discovery Issues

```bash
# Check DNS resolution
kubectl exec -it platform-mcp -- nslookup sqlserver

# Check service endpoints
kubectl get endpoints -n platform-supervisor

# Test connectivity
kubectl exec -it platform-mcp -- telnet sqlserver 1433
```

#### Performance Issues

```bash
# Monitor resource usage
kubectl top pods -n platform-supervisor

# Check health endpoints
curl http://localhost:5100/health
curl http://localhost:5100/health/ready

# Review application metrics
curl http://localhost:5100/metrics
```

### Rollback Procedures

```bash
# Kubernetes rollback
kubectl rollout undo deployment/platform-mcp -n platform-supervisor

# Docker Compose rollback
docker compose down
git checkout previous-tag
docker compose up -d

# Azure Container Apps rollback
az containerapp revision list --name platform-mcp --resource-group platform-supervisor-rg
az containerapp revision activate --revision revision-name
```

---

*For additional support, see the [main documentation](DOCUMENTATION.md) or create an issue on GitHub.*