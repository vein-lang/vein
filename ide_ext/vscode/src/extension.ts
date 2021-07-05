'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { LanguageServer } from './languageServer';

/**
 * Returns the root folder for the current workspace.
 */
 function findRootFolder() : string {
    // FIXME: handle multiple workspace folders here.
    let workspaceFolders = vscode.workspace.workspaceFolders;
    if (workspaceFolders) {
        return workspaceFolders[0].uri.fsPath;
    } else {
        return '';
    }
}

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export async function activate(context: vscode.ExtensionContext) {

    // Use the console to output diagnostic information (console.log) and errors (console.error)
    // This line of code will only be executed once when your extension is activated
    console.error('[mana-lsp] Activated!');
    process.env['VSCODE_LOG_LEVEL'] = 'trace';


    /*
    // Register commands that use the .NET Core SDK.
    // We do so as early as possible so that we can handle if someone calls
    // a command before we found the .NET Core SDK.
    registerCommand(
        context,
        "quantum.newProject",
        () => {
            createNewProject(context);
        }
    );

    registerCommand(
        context,
        "quantum.installTemplates",
        () => {
            requireDotNetSdk(dotNetSdkVersion).then(
                dotNetSdk => installTemplates(dotNetSdk, packageInfo)
            );
        }
    );

    registerCommand(
        context,
        "quantum.openDocumentation",
        openDocumentationHome
    );

    registerCommand(
        context,
        "quantum.installIQSharp",
        () => {
            requireDotNetSdk(dotNetSdkVersion).then(
                dotNetSdk => installOrUpdateIQSharp(
                    dotNetSdk,
                    packageInfo ? packageInfo.nugetVersion : undefined
                )
            );
        }
    );*/

    let rootFolder = findRootFolder();

    // Start the language server client.
    let languageServer = new LanguageServer(context, rootFolder);
    await languageServer
        .start()
        .catch(
            err => {
                console.log(`[mana-lsp] Language server failed to start: ${err}`);
                let reportFeedbackItem = "Report feedback...";
                vscode.window.showErrorMessage(
                    `Language server failed to start: ${err}`,
                    reportFeedbackItem
                ).then(
                    item => {
                        vscode.env.openExternal(vscode.Uri.parse(
                            "https://github.com/microsoft/qsharp-compiler/issues/new?assignees=&labels=bug,Area-IDE&template=bug_report.md&title="
                        ));
                    }
                );
            }
        );

}

// this method is called when your extension is deactivated
export function deactivate() {
}