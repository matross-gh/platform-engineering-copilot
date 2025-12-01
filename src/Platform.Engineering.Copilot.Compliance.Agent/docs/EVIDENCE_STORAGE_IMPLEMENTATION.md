# Evidence Storage Implementation

## Overview
This document describes the implementation of the Evidence storage configuration from `appsettings.json`.

## Configuration

```json
"Evidence": {
  "StorageAccount": "complianceevidence",
  "Container": "evidence",
  "RetentionDays": 2555,
  "EnableVersioning": true,
  "EnableImmutability": true
}
```

## Implementation Details

### 1. EvidenceStorageService
**Location:** `Services/Compliance/EvidenceStorageService.cs`

A new service that handles storing compliance evidence to Azure Blob Storage with the following features:

#### Key Features:
- **Evidence Package Storage**: Stores complete `SecurityEvidencePackage` objects to blob storage
- **Scan Results Storage**: Stores individual scan results (secrets, IaC, SAST, etc.)
- **Retention Management**: Automatically cleans up evidence older than configured retention period
- **Versioning Support**: Enables blob versioning when `EnableVersioning` is true
- **Immutability Support**: Sets immutability policies when `EnableImmutability` is true
- **Evidence Retrieval**: Retrieves stored evidence packages by ID
- **Evidence Listing**: Lists all evidence packages with filtering by date range

#### Methods:
1. `StoreEvidencePackageAsync()` - Stores complete evidence package with metadata
2. `StoreScanResultsAsync()` - Stores individual scan results (secrets, IaC, etc.)
3. `RetrieveEvidencePackageAsync()` - Retrieves evidence package by ID
4. `ListEvidencePackagesAsync()` - Lists evidence packages with date filtering
5. `CleanupExpiredEvidenceAsync()` - Removes evidence older than retention period

#### Storage Structure:
```
/evidence/{packageId}/{yyyy/MM/dd}/package.json
/scans/{scanType}/{yyyy/MM/dd}/{scanId}.json
```

### 2. CodeScanningEngine Integration
**Location:** `Services/Compliance/CodeScanningEngine.cs`

Updated to use `EvidenceStorageService` for automatic storage of scan results:

#### Changes:
- Added `_evidenceStorage` field (nullable, optional dependency)
- Constructor accepts optional `EvidenceStorageService` parameter
- `CollectSecurityEvidenceAsync()` - Stores evidence packages after generation
- `DetectSecretsAsync()` - Stores secret scan results
- `ScanInfrastructureAsCodeAsync()` - Stores IaC scan results

#### Behavior:
- If `EvidenceStorageService` is registered in DI, scan results are automatically stored
- If not configured, scanning continues normally without storage
- Storage failures are logged as warnings and don't interrupt scan operations

### 3. Data Model Updates
**Location:** `Platform.Engineering.Copilot.Core/Models/CodeScanning/SecurityModels.cs`

Added new properties to support evidence tracking:

#### SecurityEvidencePackage:
- `StorageUri` - URI where the evidence package was stored

#### EvidencePackageMetadata:
New class for listing stored evidence:
- `PackageId` - Unique evidence package identifier
- `BlobName` - Name of the blob in storage
- `BlobUri` - Full URI to the blob
- `CreatedAt` - When the evidence was created
- `SizeInBytes` - Size of the evidence package
- `ComplianceFrameworks` - Associated compliance frameworks
- `ProjectPath` - Path to the scanned project

### 4. Dependency Injection
**Location:** `Extensions/ServiceCollectionExtensions.cs`

#### Registration:
```csharp
services.AddScoped<EvidenceStorageService>();
```

Registered as `Scoped` to ensure proper lifecycle management with other compliance services.

### 5. Package Dependencies
**Location:** `Platform.Engineering.Copilot.Compliance.Agent.csproj`

Added Azure Storage SDK:
```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.22.2" />
```

## Configuration Requirements

### Environment Variable:
The service requires `AZURE_STORAGE_CONNECTION_STRING` environment variable to be set with a valid Azure Storage connection string.

**Example:**
```bash
export AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=complianceevidence;AccountKey=...;EndpointSuffix=core.windows.net"
```

### Production Recommendations:
For production deployments, use **Azure Managed Identity** instead of connection strings:
- Update `GetContainerClientAsync()` to use `DefaultAzureCredential`
- Configure the service with Storage Account endpoint
- Assign appropriate RBAC roles (Storage Blob Data Contributor)

## Usage Examples

### Automatic Storage (Integrated)
```csharp
// Evidence is automatically stored when scanning
var secretResults = await codeScanningEngine.DetectSecretsAsync(workspacePath);
// Results are now in blob storage at: /scans/secrets/{date}/{scanId}.json

var evidencePackage = await codeScanningEngine.CollectSecurityEvidenceAsync(workspacePath);
// Package stored at: /evidence/{packageId}/{date}/package.json
// evidencePackage.StorageUri contains the blob URI
```

### Manual Storage
```csharp
// Store custom scan results
var storageUri = await evidenceStorage.StoreScanResultsAsync(
    scanType: "custom-scan",
    scanResults: myCustomResults,
    projectPath: "/path/to/project"
);
```

### Retrieve Evidence
```csharp
var package = await evidenceStorage.RetrieveEvidencePackageAsync(packageId);
```

### List Evidence
```csharp
var packages = await evidenceStorage.ListEvidencePackagesAsync(
    startDate: DateTime.UtcNow.AddDays(-30),
    endDate: DateTime.UtcNow
);
```

### Cleanup Old Evidence
```csharp
// Respects RetentionDays configuration
var deletedCount = await evidenceStorage.CleanupExpiredEvidenceAsync();
```

## Configuration Impact

### EnableVersioning = true
- Blob versioning is noted in logs
- Requires blob versioning to be enabled at the storage account level
- Allows point-in-time recovery of evidence

### EnableImmutability = true
- Evidence blobs cannot be deleted or modified
- Requires immutability policies configured at account/container level
- Cleanup operations will skip immutable blobs
- Ideal for regulatory compliance (SOC2, FedRAMP, CMMC)

### RetentionDays = 2555 (7 years)
- Evidence older than 7 years will be deleted during cleanup
- Immutable evidence cannot be deleted regardless of age
- Cleanup is manual via `CleanupExpiredEvidenceAsync()`

## Security Considerations

1. **Access Control**: Use Azure RBAC to restrict who can read/write evidence
2. **Encryption**: Azure Storage encrypts data at rest by default
3. **Audit Logging**: Enable Azure Storage Analytics for access auditing
4. **Network Security**: Use Private Endpoints or Service Endpoints for storage access
5. **Immutability**: Enable for WORM (Write Once, Read Many) compliance requirements

## Future Enhancements

1. Add support for Azure Managed Identity authentication
2. Implement evidence signing for tamper detection
3. Add evidence compression for large scan results
4. Create scheduled job for automatic cleanup
5. Add evidence export to external compliance systems
6. Implement evidence search and filtering capabilities
