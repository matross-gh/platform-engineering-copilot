# GitHub Copilot File Attachment Integration

## Overview

The Platform Engineering Copilot now supports file attachments directly in GitHub Copilot chat. Upload security documents, architecture diagrams, and compliance artifacts to analyze them using the DocumentPlugin.

## How It Works

1. **Attach files in GitHub Copilot chat** (supports PDF, Word, PowerPoint, Visio, Excel)
2. **Files are base64-encoded** and sent to MCP HTTP bridge at `/mcp/chat`
3. **Server decodes and saves** to temporary storage (`/tmp/mcp-attachments/{guid}/`)
4. **DocumentPlugin processes** files using their server-side paths
5. **Automatic cleanup** after analysis completes

## Supported File Types

- **Documents**: `.pdf`, `.docx`, `.doc`, `.txt`
- **Spreadsheets**: `.xlsx`, `.xls`, `.csv`
- **Presentations**: `.pptx`, `.ppt`
- **Diagrams**: `.vsdx`, `.vsd` (Visio)
- **Security Plans**: SSPs, POA&Ms, RMF documents

## Usage Examples

### 1. SSP Analysis

```
User: @platform [attaches SSP.pdf] analyze this System Security Plan and extract NIST 800-53 controls

Response: Analyzing uploaded file: SSP.pdf
Found the following NIST 800-53 controls:
- AC-2: Account Management
- AC-3: Access Enforcement
- AU-2: Audit Events
...
```

### 2. Architecture Diagram Review

```
User: @platform [attaches architecture.vsdx] review this architecture diagram for security boundaries

Response: Analyzing uploaded file: architecture.vsdx
Identified components:
- Web Application (DMZ)
- Database (Private Subnet)
- API Gateway (Public)

Security recommendations:
1. Add network segmentation between tiers
2. Implement Azure Firewall for egress filtering
...
```

### 3. Multiple File Comparison

```
User: @platform [attaches SSP-v1.pdf, SSP-v2.pdf] compare these two versions and show changes

Response: Analyzing uploaded files: SSP-v1.pdf, SSP-v2.pdf
Version comparison:
- Added 12 new controls (AC-17, IA-5, SC-7...)
- Updated risk rating from Moderate to High
- New encryption requirements in SC family
...
```

### 4. Compliance Mapping

```
User: @platform [attaches security-plan.docx] generate FedRAMP compliance mapping for this document

Response: Analyzing uploaded file: security-plan.docx
FedRAMP High Baseline Mapping:
- AC Family: 15/23 controls implemented
- AU Family: 8/9 controls implemented
- SC Family: 12/13 controls implemented
Gaps: AC-2(12), AU-6(3), SC-13
```

## Technical Details

### Request Format

```json
POST /mcp/chat
{
  "message": "analyze this security plan",
  "context": {},
  "attachments": [
    {
      "fileName": "SSP.pdf",
      "contentType": "application/pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MK...",
      "size": 1048576
    }
  ]
}
```

### File Size Limits

- **Per-file limit**: 50 MB
- **Total request**: Unlimited (each file processed independently)
- **Storage**: Temporary directory cleaned up after processing

### Temp Storage

- **Location**: `/tmp/mcp-attachments/{guid}/`
- **Lifecycle**: Created before processing → Deleted after response sent
- **Permissions**: Application user only
- **Sanitization**: Filenames validated to prevent directory traversal

### Error Handling

If a file fails to process:
- Error logged but doesn't block other files
- User receives error message for specific file
- Processing continues for remaining attachments

Example error:
```
Warning: Failed to process file "large-file.pdf": File size exceeds 50MB limit.
Successfully processed: diagram.vsdx, ssp.docx
```

## DocumentPlugin Functions

All functions work seamlessly with attached files:

1. **upload_security_document** - Process SSPs, security plans
2. **extract_security_controls** - Find NIST 800-53 controls
3. **analyze_architecture_diagram** - Parse Visio diagrams
4. **compare_documents** - Version comparison, gap analysis
5. **generate_compliance_mapping** - Map to FedRAMP/NIST frameworks

## GitHub Copilot Extension Setup

**Note**: If GitHub Copilot doesn't automatically encode attachments as base64, you may need to configure the extension.

Create `.github/copilot-extensions.json` (if needed):

```json
{
  "extensions": {
    "file-attachments": {
      "enabled": true,
      "encoding": "base64",
      "maxFileSize": 52428800
    }
  }
}
```

## Troubleshooting

### File Not Processed

**Issue**: Upload file but plugin doesn't see it

**Solution**:
- Verify file is under 50MB
- Check container logs: `docker logs plaform-engineering-copilot-mcp`
- Ensure file type is supported

### Base64 Decode Error

**Issue**: "Invalid base64 content" error

**Solution**:
- GitHub Copilot may not be encoding file properly
- Try smaller file or different format
- Check for corrupted file

### Out of Memory

**Issue**: Container crashes when processing large files

**Solution**:
- Increase Docker memory limit in docker-compose.yml:
  ```yaml
  platform-mcp:
    mem_limit: 4g
  ```
- Split large files into smaller sections

### Temp Storage Full

**Issue**: "/tmp/mcp-attachments: No space left on device"

**Solution**:
- Cleanup script runs automatically, but manual cleanup:
  ```bash
  docker exec plaform-engineering-copilot-mcp rm -rf /tmp/mcp-attachments/*
  ```
- Check for orphaned directories from crashed processes

## Security Considerations

1. **File Validation**: All filenames sanitized to prevent directory traversal
2. **Size Limits**: 50MB per file prevents DoS attacks
3. **Temp Storage**: Isolated per-request, automatic cleanup
4. **No Persistence**: Files never stored permanently on server
5. **Access Control**: Temp files readable only by application user

## Development Testing

### Test with cURL

```bash
# Create base64-encoded file
base64 -i SSP.pdf -o ssp.b64

# Send to MCP server
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "analyze this security plan",
    "context": {},
    "attachments": [{
      "fileName": "SSP.pdf",
      "contentType": "application/pdf",
      "base64Content": "'$(cat ssp.b64)'",
      "size": '$(wc -c < SSP.pdf)'
    }]
  }'
```

### Check Container Logs

```bash
docker logs plaform-engineering-copilot-mcp --tail 100 -f
```

Look for:
```
Processing 1 attachment(s)...
Saved file: /tmp/mcp-attachments/{guid}/SSP.pdf
File processing complete, cleaning up temp directory
```

## Integration with Compliance Workflow

File attachments complete the end-to-end compliance workflow:

1. **Runtime Scanning**: `assess_nist_compliance` → Get live Azure findings
2. **Document Analysis**: Upload SSP → Extract documented controls
3. **Gap Analysis**: Compare runtime vs documented controls
4. **Evidence Collection**: `collect_compliance_evidence` → Generate POA&M
5. **eMASS Export**: Generate eMASS XML with findings + document data

Example combined workflow:

```
User: @platform assess subscription abc-123 for NIST 800-53

Response: [102 findings from runtime scan]

User: @platform [attaches SSP.pdf] compare these findings with my documented controls

Response: Gap analysis:
- 23 controls documented but failing in runtime
- 8 controls passing but not documented
- 71 controls fully compliant
Recommendation: Update SSP sections 3.1, 3.5, 3.12

User: @platform generate POA&M for the 23 failing controls and include evidence

Response: [Generated POA&M with runtime evidence + SSP references]
```

## Future Enhancements

- [ ] Support for archive files (.zip with multiple documents)
- [ ] OCR for scanned PDFs
- [ ] Real-time collaboration (shared file upload sessions)
- [ ] File caching for repeated analysis
- [ ] Integration with Azure Blob Storage for persistence

## Support

For issues or feature requests, contact the Platform Engineering Copilot team or file a GitHub issue.
