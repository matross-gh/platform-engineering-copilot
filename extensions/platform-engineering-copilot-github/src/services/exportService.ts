import * as vscode from 'vscode';

/**
 * Export a compliance report to a file in the specified format
 */
export async function exportReport(
    content: string,
    format: 'markdown' | 'json' | 'html' = 'markdown'
): Promise<void> {
    try {
        // Generate filename with timestamp
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, -5);
        const extension = format === 'markdown' ? 'md' : format;
        const defaultFilename = `compliance-report-${timestamp}.${extension}`;

        // Let user choose save location
        const uri = await vscode.window.showSaveDialog({
            defaultUri: vscode.Uri.file(defaultFilename),
            filters: {
                'Markdown': ['md'],
                'JSON': ['json'],
                'HTML': ['html'],
                'All Files': ['*']
            }
        });

        if (!uri) {
            // User cancelled
            return;
        }

        // Convert content to desired format
        let exportContent = content;
        
        if (format === 'json') {
            // Wrap content in JSON structure
            exportContent = JSON.stringify({
                timestamp: new Date().toISOString(),
                reportType: 'compliance-assessment',
                content: content
            }, null, 2);
        } else if (format === 'html') {
            // Convert markdown to simple HTML
            exportContent = convertMarkdownToHtml(content);
        }

        // Write file
        await vscode.workspace.fs.writeFile(
            uri,
            Buffer.from(exportContent, 'utf8')
        );

        // Show success message with option to open file
        const openAction = 'Open File';
        const result = await vscode.window.showInformationMessage(
            `Report exported to ${uri.fsPath}`,
            openAction
        );

        if (result === openAction) {
            const doc = await vscode.workspace.openTextDocument(uri);
            await vscode.window.showTextDocument(doc);
        }
    } catch (error) {
        vscode.window.showErrorMessage(
            `Failed to export report: ${error instanceof Error ? error.message : String(error)}`
        );
    }
}

/**
 * Copy compliance report content to clipboard
 */
export async function copyToClipboard(content: string): Promise<void> {
    try {
        await vscode.env.clipboard.writeText(content);
        vscode.window.showInformationMessage('Report copied to clipboard');
    } catch (error) {
        vscode.window.showErrorMessage(
            `Failed to copy to clipboard: ${error instanceof Error ? error.message : String(error)}`
        );
    }
}

/**
 * Share report via email (opens default email client)
 */
export async function shareViaEmail(content: string, subject: string = 'Compliance Report'): Promise<void> {
    try {
        // Create mailto link with content
        const body = encodeURIComponent(content);
        const mailtoLink = `mailto:?subject=${encodeURIComponent(subject)}&body=${body}`;
        
        await vscode.env.openExternal(vscode.Uri.parse(mailtoLink));
        
        vscode.window.showInformationMessage('Email client opened with report content');
    } catch (error) {
        vscode.window.showErrorMessage(
            `Failed to open email client: ${error instanceof Error ? error.message : String(error)}`
        );
    }
}

/**
 * Simple markdown to HTML converter
 */
function convertMarkdownToHtml(markdown: string): string {
    let html = markdown
        // Headers
        .replace(/^### (.*$)/gim, '<h3>$1</h3>')
        .replace(/^## (.*$)/gim, '<h2>$1</h2>')
        .replace(/^# (.*$)/gim, '<h1>$1</h1>')
        // Bold
        .replace(/\*\*(.*?)\*\*/gim, '<strong>$1</strong>')
        // Italic
        .replace(/\*(.*?)\*/gim, '<em>$1</em>')
        // Code blocks
        .replace(/```([\s\S]*?)```/gim, '<pre><code>$1</code></pre>')
        // Inline code
        .replace(/`(.*?)`/gim, '<code>$1</code>')
        // Links
        .replace(/\[([^\]]+)\]\(([^)]+)\)/gim, '<a href="$2">$1</a>')
        // Line breaks
        .replace(/\n\n/g, '</p><p>')
        .replace(/\n/g, '<br>');

    return `<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Compliance Report</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            max-width: 900px;
            margin: 0 auto;
            padding: 20px;
            color: #333;
        }
        h1, h2, h3 { color: #2c3e50; }
        code {
            background: #f4f4f4;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
        }
        pre {
            background: #f4f4f4;
            padding: 15px;
            border-radius: 5px;
            overflow-x: auto;
        }
        a { color: #0366d6; }
        strong { color: #d73a49; }
    </style>
</head>
<body>
    <p>${html}</p>
</body>
</html>`;
}

/**
 * Export report with format selection prompt
 */
export async function exportReportWithPrompt(content: string): Promise<void> {
    const format = await vscode.window.showQuickPick(
        [
            { label: 'Markdown', value: 'markdown', description: 'Export as .md file' },
            { label: 'JSON', value: 'json', description: 'Export as .json file' },
            { label: 'HTML', value: 'html', description: 'Export as .html file' }
        ],
        {
            placeHolder: 'Select export format'
        }
    );

    if (format) {
        await exportReport(content, format.value as 'markdown' | 'json' | 'html');
    }
}

/**
 * Show share menu with all available options
 */
export async function showShareMenu(content: string): Promise<void> {
    const option = await vscode.window.showQuickPick(
        [
            { label: '📋 Copy to Clipboard', value: 'clipboard' },
            { label: '💾 Export to File', value: 'export' },
            { label: '📧 Share via Email', value: 'email' }
        ],
        {
            placeHolder: 'Choose how to share this report'
        }
    );

    if (!option) {
        return;
    }

    switch (option.value) {
        case 'clipboard':
            await copyToClipboard(content);
            break;
        case 'export':
            await exportReportWithPrompt(content);
            break;
        case 'email':
            await shareViaEmail(content);
            break;
    }
}
