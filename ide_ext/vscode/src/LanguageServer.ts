import { StreamInfo, 
    LanguageClient,
     ServerOptions,
      RevealOutputChannelOn,
       LanguageClientOptions,
        CloseAction, 
        ErrorAction, State 
    } 
from 'vscode-languageclient/node';
import * as vscode from 'vscode';
import * as os from 'os';
import * as path from 'path';
import * as fs from 'fs-extra';
import * as net from 'net';
import * as url from 'url';


import * as cp from 'child_process';
//import * as tmp from 'tmp';

import * as portfinder from 'portfinder';

import { promisify } from 'util';


namespace CommonPaths {
    export const storageFullPath = "O:/wave_vm/lsp/bin/Debug/net6.0";
    export const executableNames = {
        darwin: "manalsp",
        linux: "manalsp",
        win32: "manalsp.exe",
        aix: null,
        android: null,
        freebsd: null,
        openbsd: null,
        sunos: null,
        cygwin: null,
        haiku: null,
        netbsd: null
    };
}

function isPathExecutable(path : string) : Promise<boolean> {
    return new Promise((resolve, reject) => {
        fs.access(path, fs.constants.X_OK, err => {
            if (err) {
                resolve(false);
            } else {
                resolve(true);
            }
        });
    });
}

/*function tmpName(config : tmp.SimpleOptions) : Promise<string> {
    return new Promise<string>((resolve, reject) => {
        tmp.tmpName(config, (err, path) => {
            if (err) {
                reject(err);
            } else {
                resolve(path);
            }
        });
    });
}*/

/*
function sha256sum(path : string) : Promise<string> {
    return new Promise((resolve, reject) => {
        // Node.js hashes are an odd kind of stream, see
        // https://stackoverflow.com/a/18658613 for an example.
        // What we need to do is set up the events on the file read stream
        // to end the hash stream when it ends, so that pipe does everything
        // for us automatically.
        var readStream = fs.createReadStream(path);
        var hash = crypto.createHash('sha256');
        hash.setEncoding('hex');

        readStream
            .on('end', () => {
                hash.end();
                resolve(hash.read().toString());
            })
            .on('error', reject);

        readStream.pipe(hash);
    });
}
*/
/**
 * Given a server, attempts to listen on a given port, incrementing the port
 * number on failure, and yielding the actual port that was used.
 *
 * @param server The server that will be listened on.
 * @param port The first port to try listening on.
 * @param maxPort The highest port number before considering the promise a
 *     failure.
 * @param hostname The hostname that the server should listen on.
 * @returns A promise that yields the actual port number used, or that fails
 *     when net.Server yields an error other than EADDRINUSE or when all ports
 *     up to and including maxPort are already in use.
 */
 function listenPromise(server: net.Server, port: number, maxPort: number, hostname: string): Promise<number> {
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
                if ("code" in err && (err as any).code === "EADDRINUSE") {
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

export class LanguageServer 
{
    private serverExe : {
        path : string,
        version : string
    } | undefined;
    private context : vscode.ExtensionContext;
    private rootFolder : string;

    constructor(context : vscode.ExtensionContext, rootFolder : string) {
        this.context = context;
        this.rootFolder = rootFolder;
    }

    async findExecutable() : Promise<boolean> {
        let lsPath : string | undefined | null = undefined;
        // Before anything else, look at the user's configuration to see
        // if they set a path manually for the language server.
        //let versionCheck = false;
        let config = vscode.workspace.getConfiguration();
        lsPath = config.get("manaDevkit.languageServerPath");

        // If lsPath is still undefined or null, then we didn't have a manual
        // path set up above.
        if (lsPath === undefined || lsPath === null) {
            // Look at the global storage path for the context to try and find the
            // language server executable.
            let exeName = CommonPaths.executableNames[os.platform()];
            if (exeName === null) {
                throw new Error(`Unsupported platform: ${os.platform()}`);
            }
            lsPath = path.join(CommonPaths.storageFullPath, exeName);
            //versionCheck = true;
        }

        // Since lsPath has been set unconditionally, we can now proceed to
        // check if it's valid or not.
        if (!await isPathExecutable(lsPath)) {
            console.log(`[mana-lsp] "${lsPath}" is not executable. Proceed to download Q# language server.`)
            // Language server didn't exist or wasn't executable.
            return false;
        }
        // NB: There is a possible race condition here, as per node docs. An
        //     alternative approach might be to simply run the language server
        //     and catch errors there.
        var response : {stdout: string, stderr: string};
        try {
            response = await promisify(cp.exec)(`"${lsPath}" --version`);
        } catch (err) {
            console.log(`[mana-lsp] Error while fetching LSP version: ${err}`);
            throw err;
        }

        if (response.stderr.trim().length !== 0) {
            throw new Error(`Language server returned error when reporting version: ${response.stderr}`);
        }

        let version = response.stdout.trim();
        /*let info = getPackageInfo(this.context);
        if (info === undefined || info === null) {
            throw new Error("Package info was undefined.");
        }
        if (versionCheck && info.version !== version) {
            console.log(`[mana-lsp] Found version ${version}, expected version ${info.version}. Clearing cached version.`);
            //await this.clearCache();
            return false;
        }*/

        this.serverExe = {
            path: lsPath,
            version: version
        };
        return true;
    }

    /*private async setAsExecutable(path : string) : Promise<void> {
        let results = await promisify(cp.exec)(`chmod +x "${path}"`);
        console.log(`[mana-lsp] Results from setting ${path} as executable:\n${results.stdout}\nstderr:\n${results.stderr}`);
        return;
    }*/

    /**
     * Invokes a new process for the language server, using the executable found
     * by the findExecutable method.
     *
     * @param port TCP port to use to talk to the language server.
     */
     async spawnProcess(port : number) : Promise<cp.ChildProcess> {
        if (this.serverExe === undefined) {
            // Try to find the exe, and fail out if not found.
            if (!await this.findExecutable()) {
                throw new Error("Could not find language server executable to spawn.");
            } else {
                // Assert that the exe info is there so that TypeScript is happy;
                // this should be post-condition of findExecutable returning true.
                console.assert(this.serverExe !== undefined, "Language server path not set after successfully finding executable. This should never happen.");
                this.serverExe = this.serverExe!;
            }
        }
        var args: string[];
        args = args = [`--port=${port}`];

        let process = cp.spawn(
            this.serverExe.path,
            args,
            {
                cwd:  this.rootFolder
            }
        ).on('error', err => {
            console.log(`[mana-lsp] Child process spawn failed with ${err}.`);
            throw(err);
        }).on('exit', (exitCode, signal) => {
            console.log(`[mana-lsp] manalsp exited with code ${exitCode} and signal ${signal}.`);
        });

        process.stderr.on('data', (data) => {
            console.error(`[mana-lsp] ${data}`);
        });
        process.stdout.on('data', (data) => {
            console.log(`[mana-lsp] ${data}`);
        });

        return process;
    }

    private startOptions(): ServerOptions {
        return () => new Promise((resolve, reject) => {

            let server = net.createServer(socket => {
                // We use an explicit cast here as a workaround for a bug in @types/node.
                // https://github.com/DefinitelyTyped/DefinitelyTyped/issues/17020
                resolve({
                    reader: socket,
                    writer: socket
                } as StreamInfo);
            });

            // Begin by trying to find an appropriate port to pass along to the LSP executable.
            portfinder.getPortPromise({'port': 8091})
                .then(port => {
                    console.log(`[mana-lsp] Found port at ${port}.`);
                    // We found a port, so let's go along and use it to
                    // make a socket server.
                    return listenPromise(server, port, port + 10, '127.0.0.1');
                })
                .then((actualPort) => {
                    console.log(`[mana-lsp] Successfully listening on port ${actualPort}, spawning server.`);
                    return this.spawnProcess(actualPort);
                })
                .then((childProcess) => {
                    console.log(`[mana-lsp] started Mana Language Server as PID ${childProcess.pid}.`);
                })
                .catch(err => {
                    // Could not find a port...
                    console.log(`[mana-lsp] Could not open an unused port: ${err}.`);
                    reject(err);
                });

        });
    }

    private async startClient() : Promise<LanguageClient> {
        //const languageServerStartedAt = Date.now();
        let serverOptions = this.startOptions();

        let clientOptions: LanguageClientOptions = {
            initializationOptions: {
                client: "VSCode",
            },
            documentSelector: [
                {scheme: "file", language: "mana"}
            ],
            revealOutputChannelOn: RevealOutputChannelOn.Never,

            // Due to the known issue
            // https://github.com/Microsoft/vscode-languageserver-node/issues/105,
            // we use the workaround from
            // https://github.com/felixfbecker/vscode-php-intellisense/pull/23
            // to convert URIs in and out of VS Code's internal formatting through
            // the "uri" NPM package.
            uriConverters: {
                // VS Code by default %-encodes even the colon after the drive letter
                // NodeJS handles it much better
                code2Protocol: uri => url.format(url.parse(uri.toString(true))),
                protocol2Code: str => vscode.Uri.parse(str)
            },

            errorHandler: {
                closed: () => {
                    return CloseAction.DoNotRestart;
                },
                error: (error, message, count) => {
                    // By default, continue the server as best as possible.
                    return ErrorAction.Shutdown;
                }
            }
        };

        let client = new LanguageClient(
            'mana',
            'Mana Language Extension',
            serverOptions,
            clientOptions
        );
        this.context.subscriptions.push(
            // The definition of the StateChangeEvent has changed in recent versions of VS Code,
            // so we use a dictionary from enum of different states to strings to make sure that
            // the debug log is useful even if the enum changes.
            client.onDidChangeState(stateChangeEvent => {
                var states : { [key in State]: string } = {
                    [State.Running]: "running",
                    [State.Starting]: "starting",
                    [State.Stopped]: "stopped"
                };
                console.log(`[mana-lsp] State ${states[stateChangeEvent.oldState]} -> ${states[stateChangeEvent.newState]}`);
            })
        );

        return client;

    }

    async start(retries : number = 3) : Promise<void> {
        if (!await this.findExecutable()) {
            // Try again after downloading.
            try {
                throw new Error();
            } catch (err) {
                console.log(`[mana-lsp] Error downloading language server: ${err}. ${retries} left.`);
                if (retries > 0) {
                    return this.start(retries - 1);
                } else {
                    let retryItem = "Try again";
                    let reportFeedbackItem = "Report feedback...";
                    switch (await vscode.window.showErrorMessage(
                        "Could not download Mana language server.",
                        retryItem, reportFeedbackItem
                    )) {
                        case retryItem:
                            return this.start(1);
                            break;
                        case reportFeedbackItem:
                            vscode.env.openExternal(vscode.Uri.parse(
                                "https://github.com/0xF6/mana_lang/issues/new?assignees=&labels=bug,Area-IDE&template=bug_report.md&title="
                            ));
                            return;
                            break;
                    }
                }
            }
            if (!this.findExecutable()) {
                // NB: This case should never occur, as we only get to this
                //     point in control flow by passing the try/catch above
                //     without throwing an error. If we get here something
                //     seriously wrong has occurred inside the extension.
                throw new Error("Could not find language server.");
            }
        }
        let client = await this.startClient();
        let disposable = client.start();
        this.context.subscriptions.push(disposable);

        console.log("[mana-lsp] Started LanguageClient object.");
    }
}