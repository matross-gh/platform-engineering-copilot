import * as vscode from 'vscode';
import * as path from 'path';

/**
 * Interface for file creation requests
 */
export interface FileToCreate {
    /** Relative path from workspace root (e.g., 'infra/main.bicep') */
    relativePath: string;
    /** File content */
    content: string;
    /** Whether to open this file after creation */
    openAfterCreate?: boolean;
}

/**
 * Interface for workspace creation requests
 */
export interface WorkspaceCreationRequest {
    /** Display name for the workspace/project */
    projectName?: string;
    /** Files to create */
    files: FileToCreate[];
    /** Whether to create in new folder */
    createInNewFolder?: boolean;
    /** Folder name if createInNewFolder is true */
    folderName?: string;
}

/**
 * Service for creating files and workspace structures from agent responses
 */
export class WorkspaceService {
    
    /**
     * Create a single file in the workspace
     */
    async createFile(
        fileName: string,
        content: string,
        openAfterCreate: boolean = true
    ): Promise<void> {
        try {
            // Get workspace folder
            const workspaceFolder = await this.getWorkspaceFolder();
            if (!workspaceFolder) {
                return;
            }

            // Create file URI
            const fileUri = vscode.Uri.joinPath(workspaceFolder, fileName);

            // Check if file exists
            const exists = await this.fileExists(fileUri);
            if (exists) {
                const overwrite = await vscode.window.showWarningMessage(
                    `File ${fileName} already exists. Overwrite?`,
                    { modal: true },
                    'Overwrite',
                    'Cancel'
                );

                if (overwrite !== 'Overwrite') {
                    return;
                }
            }

            // Create parent directories if needed
            await this.ensureDirectoryExists(fileUri);

            // Write file
            await vscode.workspace.fs.writeFile(
                fileUri,
                Buffer.from(content, 'utf8')
            );

            vscode.window.showInformationMessage(
                `✅ Created ${fileName}`
            );

            // Open file if requested
            if (openAfterCreate) {
                await this.openFile(fileUri);
            }
        } catch (error) {
            vscode.window.showErrorMessage(
                `Failed to create file: ${error instanceof Error ? error.message : String(error)}`
            );
            throw error;
        }
    }

    /**
     * Create multiple files (workspace/project structure)
     */
    async createWorkspace(request: WorkspaceCreationRequest): Promise<void> {
        try {
            // Get base folder
            let baseFolder = await this.getWorkspaceFolder();
            if (!baseFolder) {
                return;
            }

            // Create new folder if requested
            if (request.createInNewFolder && request.folderName) {
                const newFolderUri = vscode.Uri.joinPath(baseFolder, request.folderName);
                await vscode.workspace.fs.createDirectory(newFolderUri);
                baseFolder = newFolderUri;
                
                vscode.window.showInformationMessage(
                    `📁 Created project folder: ${request.folderName}`
                );
            }

            // Create all files
            const createdFiles: vscode.Uri[] = [];
            let firstFileToOpen: vscode.Uri | undefined;

            for (const file of request.files) {
                const fileUri = vscode.Uri.joinPath(baseFolder, file.relativePath);

                // Create parent directories
                await this.ensureDirectoryExists(fileUri);

                // Write file
                await vscode.workspace.fs.writeFile(
                    fileUri,
                    Buffer.from(file.content, 'utf8')
                );

                createdFiles.push(fileUri);

                // Track first file marked for opening
                if (file.openAfterCreate && !firstFileToOpen) {
                    firstFileToOpen = fileUri;
                }
            }

            // Show success message
            const projectName = request.projectName || request.folderName || 'Project';
            vscode.window.showInformationMessage(
                `✅ Created ${projectName} with ${createdFiles.length} file(s)`
            );

            // Open first file if specified
            if (firstFileToOpen) {
                await this.openFile(firstFileToOpen);
            } else if (createdFiles.length > 0) {
                // Open first file by default
                await this.openFile(createdFiles[0]);
            }

            // Show explorer view to see created files
            await vscode.commands.executeCommand('workbench.view.explorer');
        } catch (error) {
            vscode.window.showErrorMessage(
                `Failed to create workspace: ${error instanceof Error ? error.message : String(error)}`
            );
            throw error;
        }
    }

    /**
     * Create IaC template structure (Bicep, Terraform, etc.)
     */
    async createInfrastructureTemplate(
        templateType: 'bicep' | 'terraform' | 'kubernetes',
        files: Map<string, string>,
        projectName?: string
    ): Promise<void> {
        const folderName = projectName || `${templateType}-project-${Date.now()}`;
        
        // Define folder structure based on template type
        const fileStructure: FileToCreate[] = [];

        if (templateType === 'bicep') {
            // Organize Bicep files
            for (const [fileName, content] of files.entries()) {
                const relativePath = fileName.endsWith('.json') 
                    ? fileName // parameters file in root
                    : fileName.includes('module') 
                        ? `modules/${fileName}` // modules in subfolder
                        : fileName; // main files in root

                fileStructure.push({
                    relativePath,
                    content,
                    openAfterCreate: fileName === 'main.bicep'
                });
            }

            // Add README if not exists
            if (!files.has('README.md')) {
                fileStructure.push({
                    relativePath: 'README.md',
                    content: this.generateBicepReadme(projectName || folderName),
                    openAfterCreate: false
                });
            }
        } else if (templateType === 'terraform') {
            // Organize Terraform files
            for (const [fileName, content] of files.entries()) {
                const relativePath = fileName.includes('module')
                    ? `modules/${fileName}`
                    : fileName;

                fileStructure.push({
                    relativePath,
                    content,
                    openAfterCreate: fileName === 'main.tf'
                });
            }

            // Add .gitignore
            fileStructure.push({
                relativePath: '.gitignore',
                content: this.generateTerraformGitignore(),
                openAfterCreate: false
            });

            // Add README
            if (!files.has('README.md')) {
                fileStructure.push({
                    relativePath: 'README.md',
                    content: this.generateTerraformReadme(projectName || folderName),
                    openAfterCreate: false
                });
            }
        } else if (templateType === 'kubernetes') {
            // Organize Kubernetes manifests
            for (const [fileName, content] of files.entries()) {
                const relativePath = `manifests/${fileName}`;
                fileStructure.push({
                    relativePath,
                    content,
                    openAfterCreate: fileName.includes('deployment')
                });
            }
        }

        await this.createWorkspace({
            projectName: `${templateType.toUpperCase()} Infrastructure`,
            files: fileStructure,
            createInNewFolder: true,
            folderName
        });
    }

    /**
     * Get workspace folder (prompt if multiple or none)
     */
    private async getWorkspaceFolder(): Promise<vscode.Uri | undefined> {
        const workspaceFolders = vscode.workspace.workspaceFolders;

        if (!workspaceFolders || workspaceFolders.length === 0) {
            // No workspace open - prompt to select folder
            const folderUris = await vscode.window.showOpenDialog({
                canSelectFiles: false,
                canSelectFolders: true,
                canSelectMany: false,
                title: 'Select folder to create files in',
                openLabel: 'Select Folder'
            });

            if (!folderUris || folderUris.length === 0) {
                vscode.window.showWarningMessage('No folder selected. Operation cancelled.');
                return undefined;
            }

            return folderUris[0];
        }

        if (workspaceFolders.length === 1) {
            return workspaceFolders[0].uri;
        }

        // Multiple workspace folders - let user choose
        const selected = await vscode.window.showQuickPick(
            workspaceFolders.map(f => ({
                label: f.name,
                description: f.uri.fsPath,
                uri: f.uri
            })),
            {
                placeHolder: 'Select workspace folder to create files in'
            }
        );

        return selected?.uri;
    }

    /**
     * Check if file exists
     */
    private async fileExists(uri: vscode.Uri): Promise<boolean> {
        try {
            await vscode.workspace.fs.stat(uri);
            return true;
        } catch {
            return false;
        }
    }

    /**
     * Ensure parent directory exists
     */
    private async ensureDirectoryExists(fileUri: vscode.Uri): Promise<void> {
        const dirPath = path.dirname(fileUri.fsPath);
        const dirUri = vscode.Uri.file(dirPath);
        
        try {
            await vscode.workspace.fs.stat(dirUri);
        } catch {
            // Directory doesn't exist, create it
            await vscode.workspace.fs.createDirectory(dirUri);
        }
    }

    /**
     * Open file in editor
     */
    private async openFile(uri: vscode.Uri): Promise<void> {
        const doc = await vscode.workspace.openTextDocument(uri);
        await vscode.window.showTextDocument(doc, {
            preview: false,
            preserveFocus: false
        });
    }

    /**
     * Generate Bicep README
     */
    private generateBicepReadme(projectName: string): string {
        return `# ${projectName}

Azure Bicep Infrastructure Template

## Prerequisites

- Azure CLI installed
- Azure subscription

## Deployment

\`\`\`bash
# Login to Azure
az login

# Create resource group
az group create --name <resource-group> --location <location>

# Deploy template
az deployment group create \\
  --resource-group <resource-group> \\
  --template-file main.bicep \\
  --parameters main.parameters.json
\`\`\`

## Files

- \`main.bicep\` - Main template file
- \`main.parameters.json\` - Parameters file
- \`modules/\` - Reusable Bicep modules

## Generated by

Platform Engineering Copilot - Infrastructure Agent
`;
    }

    /**
     * Generate Terraform README
     */
    private generateTerraformReadme(projectName: string): string {
        return `# ${projectName}

Terraform Infrastructure as Code

## Prerequisites

- Terraform >= 1.0
- Azure CLI installed and authenticated

## Usage

\`\`\`bash
# Initialize Terraform
terraform init

# Plan changes
terraform plan

# Apply infrastructure
terraform apply

# Destroy infrastructure
terraform destroy
\`\`\`

## Files

- \`main.tf\` - Main configuration
- \`variables.tf\` - Input variables
- \`outputs.tf\` - Output values
- \`modules/\` - Reusable modules

## Generated by

Platform Engineering Copilot - Infrastructure Agent
`;
    }

    /**
     * Generate Terraform .gitignore
     */
    private generateTerraformGitignore(): string {
        return `# Terraform files
.terraform/
*.tfstate
*.tfstate.*
crash.log
crash.*.log
*.tfvars
override.tf
override.tf.json
*_override.tf
*_override.tf.json
.terraformrc
terraform.rc

# OS files
.DS_Store
Thumbs.db
`;
    }
}
