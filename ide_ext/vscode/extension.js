"use strict";

const workspace = require("vscode").workspace;
const TransportKind = require("vscode-languageclient/node").TransportKind;
const LanguageClient = require("vscode-languageclient/node").LanguageClient;
const createServerPipeTransport = require("vscode-languageclient/node").createServerPipeTransport;
const Trace = require("vscode-jsonrpc/node").Trace;
const net = require("net");
/*
import {
    LanguageClient,
    TransportKind,
} from "vscode-languageclient/node";*/
/*import { Trace } from "vscode-jsonrpc/node";*/
function listenPromise(server, port, maxPort, hostname) {
    return new Promise((resolve, reject) => {
        if (port >= maxPort) {
            reject("Could not find an open port.");
        }
        server.listen(port, hostname)
            .on('listening', () => resolve(port))
            .on('error', (err) => {
                // The 'error' callback lists that err has type Error, which
                // is not specific enough to ensure that the property "code"
                // exists. We cast through any to work around this typing
                // bug, but that's not good at all.
                //
                // To try and mitigate the impact of casting through any,
                // we check explicitly if err.code exists first. In the case
                // that it doesn't, we fail through with err as intended.
                //
                // See
                //     https://github.com/angular/angularfire2/issues/666
                // for another example of a very similar bug.
                if ("code" in err && (err).code === "EADDRINUSE") {
                    // portfinder accidentally gave us a port that was already in use,
                    // which can happen due to race conditions. Let's try the next few
                    // ports in case we get lucky.
                    resolve(listenPromise(server, port + 1, maxPort, hostname));
                }
                // If we got any other error, reject the promise here; there's
                // nothing else we can do.
                reject(err);
            });
    });
}

function startOptions() {
    return () => new Promise((resolve, reject) => {

        let server = net.createServer(socket => {
            resolve({
                reader: socket,
                writer: socket
            });
        });
        let port = 9092;
        listenPromise(server, port, port + 10, '127.0.0.1')
        return;
        // Begin by trying to find an appropriate port to pass along to the LSP executable.
        /*portfinder.getPortPromise({'port': 8091})
            .then(port => {
                console.log(`[qsharp-lsp] Found port at ${port}.`);
                // We found a port, so let's go along and use it to
                // make a socket server.
                return listenPromise(server, port, port + 10, '127.0.0.1');
            })
            .then((actualPort) => {
                console.log(`[qsharp-lsp] Successfully listening on port ${actualPort}, spawning server.`);
                
            })
            .then((childProcess) => {
                console.log(`[qsharp-lsp] started Q# Language Server as PID ${childProcess.pid}.`);
            })
            .catch(err => {
                // Could not find a port...
                console.log(`[qsharp-lsp] Could not open an unused port: ${err}.`);
                reject(err);
            });*/

    });
}

function activate(context) {
    // The server is implemented in node
    let serverExe = "dotnet";
    let serverOptions = startOptions();
    // let serverExe = "D:\\Development\\Omnisharp\\csharp-language-server-protocol\\sample\\SampleServer\\bin\\Debug\\netcoreapp2.0\\win7-x64\\SampleServer.exe";
    // let serverExe = "D:/Development/Omnisharp/omnisharp-roslyn/artifacts/publish/OmniSharp.Stdio.Driver/win7-x64/OmniSharp.exe";
    // The debug options for the server
    // let debugOptions = { execArgv: ['-lsp', '-d' };5

    // If the extension is launched in debug mode then the debug server options are used
    // Otherwise the run options are used
    /*let serverOptions = {
        // run: { command: serverExe, args: ['-lsp', '-d'] },
        run: {
            command: serverExe,
            args: ["C:/git/wave_vm/langserver/bin/Debug/netcoreapp3.1/langserver.dll"],
            transport: TransportKind.pipe,
        },
        // debug: { command: serverExe, args: ['-lsp', '-d'] }
        debug: {
            command: serverExe,
            args: ["C:/git/wave_vm/langserver/bin/Debug/netcoreapp3.1//SampleServer.dll"],
            transport: TransportKind.pipe,
            runtime: "",
        },
    };*/
    /*let time = 100;
    let serverOptions = async () => {
        await new Promise((r) => setTimeout(r, time));
        time = 10000;
         const [reader, writer] = createServerPipeTransport("\\\\.\\pipe\\wave-lps");
         return {
             reader,
             writer,
         };
    };
*/
    // Options to control the language client
    let clientOptions = {
        // Register the server for plain text documents
        documentSelector: [
            {
                pattern: "**/*.wave",
            },
        ],
        progressOnInitialization: true,
        synchronize: {
            // Synchronize the setting section 'languageServerExample' to the server
            configurationSection: "languageServerExample",
            fileEvents: workspace.createFileSystemWatcher("**/*.wave"),
        },
    };

    // Create the language client and start the client.
    const client = new LanguageClient("languageServerExample", "Language Server Example", serverOptions, clientOptions);
    client.registerProposedFeatures();
    client.trace = Trace.Verbose;
    let disposable = client.start();

    // Push the disposable to the context's subscriptions so that the
    // client can be deactivated on extension deactivation
    context.subscriptions.push(disposable);
}

exports.activate = activate;