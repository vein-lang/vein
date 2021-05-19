import {
    ChildProcessInfo,
    MessageTransports,
    StreamInfo,
    TransportKind,
    LanguageClient,
    LanguageClientOptions,
    ServerOptions

} from "vscode-languageclient/node";
import * as path from "path";
import { Trace } from "vscode-jsonrpc/node";
import { commands, languages, Hover, workspace, window } from "vscode";
import { ChildProcess } from "child_process";

let client: LanguageClient;

function startOptions(): Promise<ChildProcess | StreamInfo | MessageTransports | ChildProcessInfo> {
    return Promise.reject();
}

export function deactivate() {
    if (client) {
      return client.stop();
    }
  }
function activate(context) {
    let serverExe = "dotnet";
    let serverOptions: ServerOptions = {
        // run: { command: serverExe, args: ['-lsp', '-d'] },
        run: {
            command: serverExe,
            args: ["O:/mana_lang/lsp/bin/Debug/net6.0/manalsp.dll"],
            transport: TransportKind.pipe,
        },
        // debug: { command: serverExe, args: ['-lsp', '-d'] }
        debug: {
            command: serverExe,
            args: ["O:/mana_lang/lsp/bin/Debug/net6.0/manalsp.dll"],
            transport: TransportKind.pipe,
            runtime: "",
        },
    };
    let clientOptions: LanguageClientOptions = {
        // Register the server for plain text documents
        documentSelector: [
            {
                pattern: "**/*.mana",
            }
        ],
        progressOnInitialization: true,
        synchronize: {
            // Synchronize the setting section 'languageServerExample' to the server
            configurationSection: "ManaLSP",
            fileEvents: workspace.createFileSystemWatcher("**/*.mana"),
        },
    };

    // Create the language client and start the client.
    client = new LanguageClient("ManaLSP", "Mana Language Server", serverOptions, clientOptions);
    client.registerProposedFeatures();
    client.trace = Trace.Verbose;
    let disposable = client.start();

    // Push the disposable to the context's subscriptions so that the
    // client can be deactivated on extension deactivation
    context.subscriptions.push(disposable);
    const commandHandler = () => {
        client.stop();
    };
    context.subscriptions.push(commands.registerCommand("editor.action.shutdownManaLSP", commandHandler));
}

exports.activate = activate;