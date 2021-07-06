namespace mana.lsp
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Markdig;
    using Markdig.Extensions.DefinitionLists;
    using Markdig.Renderers.Normalize;
    using Markdig.Syntax;
    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using project;
    using SixLabors.ImageSharp;
    using Sprache;
    using stl;
    using syntax;
    using Position = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
    using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

    public class ProjectManager : IDisposable
    {
        private readonly Action<Exception> _onException;
        private readonly Action<string, MessageType> _log;
        private readonly Action<PublishDiagnosticParams> _publish;

        public ProjectManager(Action<Exception> onException, Action<string, MessageType> log, Action<PublishDiagnosticParams> publish)
        {
            _onException = onException;
            _log = log;
            _publish = publish;
            this.Load = new ProcessingQueue(onException);
            this.Projects = new ConcurrentDictionary<Uri, ManaProject>();
        }

        public ConcurrentDictionary<Uri, ManaProject> Projects { get; set; }

        public ProcessingQueue Load { get; }

        public void Dispose()
        {
        }

        public Task LoadProjectsAsync(IEnumerable<Uri> projects, ProjectLoader qsProjectLoader, Func<Uri, FileContentManager> getOpenFile)
        {
            return Task.CompletedTask;
        }

        public Task AssemblyChangedOnDiskAsync(Uri dllPath)
        {
            return Task.CompletedTask;

        }

        public Task ProjectChangedOnDiskAsync(Uri project, object o, Func<Uri, FileContentManager> getOpenFile)
        {
            return Task.CompletedTask;

        }

        public Task SourceFileChangedOnDiskAsync(Uri sourceFile, Func<Uri, FileContentManager> getOpenFile)
        {
            return Task.CompletedTask;

        }



        /// <summary>
        /// If the given file can be uniquely associated with a compilation unit, 
        /// executes the given Action on the CompilationUnitManager of that project (if one exists), passing true as second argument. 
        /// Executes the given Action on the DefaultManager otherwise, passing false as second argument. 
        /// Throws an ArgumentNullException if the given Action is null. 
        /// </summary>
        public Task ManagerTaskAsync(Uri file, Action<CompilationUnitManager, bool> executeTask)
        {
            if (executeTask == null) throw new ArgumentNullException(nameof(executeTask));
            return this.Load.QueueForExecutionAsync(() =>
            {
                var didExecute = false;
                var options = new ParallelOptions { TaskScheduler = TaskScheduler.Default };
                //var projectOutputPaths = this.Projects.ToImmutableDictionary(p => p.Key, p => p.Value.OutputPath);
                //Parallel.ForEach(this.Projects.Values, options, project =>
                //{
                //    if (project.ManagerTask(file, m => executeTask(m, true), projectOutputPaths))
                //    { didExecute = true; }
                //});
                //if (!didExecute) executeTask(this.DefaultManager, false);
            });
        }

        public WorkspaceEdit Rename(RenameParams renameParams, bool versionedChanges)
        {
            return null;

        }

        public Location DefinitionLocation(TextDocumentPositionParams textDocumentPositionParams)
        {
            return null;

        }

        public SignatureHelp SignatureHelp(TextDocumentPositionParams textDocumentPositionParams, MarkupKind format)
        {
            return null;

        }

        public Hover HoverInformation(TextDocumentPositionParams doc, MarkupKind format)
        {
            Hover? GetHover(string? info, Transform t) => info == null ? null : new Hover
            {
                Contents = new MarkupContent { Kind = format, Value = info }
            };
            var markdown = format == MarkupKind.Markdown;
            var h = new Hover();
            try
            {
                var f = File.ReadAllText(doc.TextDocument.Uri.AbsolutePath);
                var result = new ManaSyntax().CompilationUnit.ParseMana(f);
                foreach (var member in result.Members)
                {
                    if (member is ClassDeclarationSyntax c)
                    {
                        if (!c.IsInside(AsManaPosition(doc.Position)))
                            continue;

                        var t = doc.Position;
                        var s = c.Identifier.Transform.pos;

                        if ((t.Line + 1) == s.Line)
                        {
                            if (s.Column < t.Character && s.Column + c.Identifier.Transform.len > t.Character)
                            {
                                var str = new StringBuilder();

                                str.Append($"```mana\nclass {c.Identifier.ExpressionString}\n```\n");

                                return GetHover(str.ToString(), c.Identifier.Transform);
                            }
                        }

                        foreach(var method in c.Methods)
                        {
                            if (!method.IsInside(AsManaPosition(doc.Position)))
                                continue;
                        }
                    }
                }

                return h;
            }
            catch (Exception e)
            {
                _log?.Invoke(e.Message, MessageType.Warning);
                return null;
            }

        }

        public static string Span(string s, Color color)
        {
            return $"<span style=\"color:#{color.ToHex()};\">{s}</span>";
        }

        static string ToMarkdown(IEnumerable<Block> blocks)
        {
            var writer = new StringWriter();
            var renderer = new NormalizeRenderer(writer);
            var pipeline = new MarkdownPipelineBuilder().Build();
            pipeline.Setup(renderer);
            foreach (var block in blocks)
            {
                renderer.Render(block);
            }

            // We convert \n to \r because the YAML serialization will eventually
            // output \n\n for \n, but \r\n for \r.
            return writer.ToString().TrimEnd().Replace('\n', '\r');
        }

        public DocumentHighlight[] DocumentHighlights(TextDocumentPositionParams textDocumentPositionParams)
        {
            return null;

        }

        public ImmutableDictionary<string, WorkspaceEdit> CodeActions(CodeActionParams codeActionParams)
        {
            return null;

        }

        public Location[] SymbolReferences(ReferenceParams referenceParams)
        {
            return null;

        }

        public Sprache.Position AsManaPosition(Position vsPos)
            => new(0, vsPos.Line + 1, vsPos.Character);

        public Range AsRange(Transform t) =>  new()
        {
            Start = new Position(t.pos.Line, t.pos.Column),
            End = new Position(t.pos.Line, t.len)
        };

        public SymbolInformation Create(ClassDeclarationSyntax clazz, DocumentSymbolParams documentSymbolParams)
        {
            var s = new SymbolInformation();

            s.Location = new Location { Uri = documentSymbolParams.TextDocument.Uri};

            s.Location.Range = AsRange(clazz.Identifier.Transform);

            s.Name = clazz.Identifier.ExpressionString;
            s.Kind = SymbolKind.Class;
            s.ContainerName = $"{s.Name}.mana";

            return s;
        }
        public SymbolInformation[] DocumentSymbols(DocumentSymbolParams documentSymbolParams)
        {
            var list = new List<SymbolInformation>();
            try
            {
                var f = File.ReadAllText(documentSymbolParams.TextDocument.Uri.AbsolutePath);
                var result = new ManaSyntax().CompilationUnit.ParseMana(f);

                foreach (var member in result.Members)
                {
                    if (member is ClassDeclarationSyntax c)
                        list.Add(Create(c, documentSymbolParams));
                }

                return list.ToArray();
            }
            catch (Exception e)
            {
                _log?.Invoke(e.Message, MessageType.Warning);
                return null;
            }
        }


    }

    public class FileContentManager
    {
        internal Uri Uri { get; }

        public string FileName { get; }

        private readonly ManagedList<BaseSyntax> content;
        private readonly ManagedList<ImmutableArray<BaseSyntax>> tokens;
        private readonly FileHeader header;

        private DocumentDeclaration compilationUnit;

        /// <summary>
        /// List of unprocessed updates that are all limited to the same (single) line.
        /// </summary>
        private readonly Queue<TextDocumentContentChangeEvent> unprocessedUpdates;

        /// <summary>
        /// Contains the line numbers in the current content that have been modified.
        /// </summary>
        private readonly ManagedSortedSet editedContent;

        /// <summary>
        /// Contains the line numbers in the current token list that have been modified.
        /// </summary>
        private readonly ManagedSortedSet editedTokens;

        /// <summary>
        /// Contains the qualified names of the callables for which content has been modified.
        /// </summary>
        private readonly ManagedList<IdentifierExpression> editedCallables;

        // properties containing different kinds of diagnostics:
        private readonly ManagedList<Diagnostic> scopeDiagnostics;
        private readonly ManagedList<Diagnostic> syntaxDiagnostics;
        private readonly ManagedList<Diagnostic> contextDiagnostics;
        private readonly ManagedList<Diagnostic> semanticDiagnostics;
        private readonly ManagedList<Diagnostic> headerDiagnostics;

        /// <summary>
        /// Used to store partially computed semantic diagnostics until they are ready for publishing.
        /// </summary>
        private readonly ManagedList<Diagnostic> updatedSemanticDiagnostics;

        /// <summary>
        /// Used to store partially computed header diagnostics until they are ready for publishing.
        /// </summary>
        private readonly ManagedList<Diagnostic> updatedHeaderDiagnostics;

        // locks and other stuff used coordinate:

        /// <summary>
        /// Used as sync root for all managed data structures.
        /// </summary>
        internal ReaderWriterLockSlim SyncRoot { get; }

        /// <summary>
        /// Used to periodically trigger processing the queued changes if no further editing takes place for a while.
        /// </summary>
        private readonly System.Timers.Timer timer;

        private readonly ConcurrentDictionary<string, FileContentManager> fileContentManagers;

        public FileContentManager(Uri uri, string getFileId)
        {
            throw new NotImplementedException();
        }

        internal void AddTimerTriggeredUpdateEvent() => this.timer.Start();

        // events and event handlers

        /// <summary>
        /// Publish an event to notify all subscribers when the timer for queued changes expires.
        /// </summary>
        internal event TimerTriggeredUpdate? TimerTriggeredUpdateEvent;

        internal delegate Task TimerTriggeredUpdate(Uri file);

        /// <summary>
        /// Publish an event to notify all subscribers when the entire type checking needs to be re-run.
        /// </summary>
        internal event GlobalTypeChecking? GlobalTypeCheckingEvent;

        internal delegate Task GlobalTypeChecking();

        internal void TriggerGlobalTypeChecking() =>
            this.GlobalTypeCheckingEvent?.Invoke();

        public FileContentManager()
        {
            this.fileContentManagers = new ConcurrentDictionary<string, FileContentManager>();
        }

        public void Flush()
        {
            
        }

        public void ReplaceFileContent(string fileContent)
        {
            throw new NotImplementedException();
        }

        public void Verify(DocumentDeclaration compilationUnit)
        {
            throw new NotImplementedException();
        }

        public PublishDiagnosticParams Diagnostics()
        {
            throw new NotImplementedException();
        }

        public void PushChange(TextDocumentContentChangeEvent change, out bool publish)
        {
            throw new NotImplementedException();
        }
    }

    public class ProjectLoader
    {

    }

    internal class EditorState : IDisposable
    {
        private readonly ProjectManager Projects;
        private readonly ProjectLoader ProjectLoader;
        public void Dispose() => this.Projects.Dispose();

        private readonly Action<PublishDiagnosticParams> Publish;
        private readonly Action<string, Dictionary<string, string>, Dictionary<string, int>> SendTelemetry;

        /// <summary>
        /// needed to determine if the reality of a source file that has changed on disk is indeed given by the content on disk, 
        /// or whether its current state as it is in the editor needs to be preserved
        /// </summary>
        private readonly ConcurrentDictionary<Uri, FileContentManager> OpenFiles;
        private FileContentManager GetOpenFile(Uri key) => this.OpenFiles.TryGetValue(key, out var file) ? file : null;

        /// <summary>
        /// any edits in the editor to the listed files (keys) are ignored, while changes on disk are still being processed
        /// </summary>
        private readonly ConcurrentDictionary<Uri, byte> IgnoreEditorUpdatesForFiles;
        internal void IgnoreEditorUpdatesFor(Uri uri) => this.IgnoreEditorUpdatesForFiles.TryAdd(uri, new byte());

        private static bool ValidFileUri(Uri file) => file != null && file.IsFile && file.IsAbsoluteUri;
        private bool IgnoreFile(Uri file) => file == null || this.IgnoreEditorUpdatesForFiles.ContainsKey(file) || file.LocalPath.ToLowerInvariant().Contains("vctmp");

        /// <summary>
        /// Calls the given publishDiagnostics Action with the changed diagnostics whenever they have changed, 
        /// calls the given onException Action whenever the compiler encounters an internal error, and
        /// does nothing if the a given action is null.
        /// </summary>
        internal EditorState(ProjectLoader projectLoader,
            Action<PublishDiagnosticParams> publishDiagnostics, Action<string, Dictionary<string, string>, Dictionary<string, int>> sendTelemetry,
            Action<string, MessageType> log, Action<Exception> onException, object onTemporaryProjectLoaded)
        {
            this.IgnoreEditorUpdatesForFiles = new ConcurrentDictionary<Uri, byte>();
            this.SendTelemetry = sendTelemetry ?? ((_, __, ___) => { });
            this.Publish = param =>
            {
                var onProjFile = param.Uri.AbsolutePath.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase);
                if (!param.Diagnostics.Any() || this.OpenFiles.ContainsKey(param.Uri) || onProjFile)
                {
                    // Some editors (e.g. Visual Studio) will actually ignore diagnostics for .csproj files.
                    // Since all errors on project loading are associated with the corresponding project file for publishing, 
                    // we need to replace the project file ending before publishing. This issue is naturally resolved once we have our own project files...
                    var parentDir = Path.GetDirectoryName(param.Uri.AbsolutePath);
                    var projFileWithoutExtension = Path.GetFileNameWithoutExtension(param.Uri.AbsolutePath);
                    if (onProjFile && Uri.TryCreate(Path.Combine(parentDir, $"{projFileWithoutExtension}.qsproj"), UriKind.Absolute, out var parentFolder))
                    { param.Uri = parentFolder; }
                    publishDiagnostics?.Invoke(param);
                }
            };

            this.ProjectLoader = projectLoader ;
            this.Projects = new ProjectManager(onException, log, this.Publish);
            this.OpenFiles = new ConcurrentDictionary<Uri, FileContentManager>();
        }

        /// <summary>
        /// If the given uri corresponds to a C# project file, 
        /// determines if the project is consistent with a recognized Q# project using the ProjectLoader.
        /// Returns the project information containing the outputPath of the project 
        /// along with the Q# source files as well as all project and dll references as out parameter if it is. 
        /// Returns null if it isn't, or if the project file itself has been listed as to be ignored. 
        /// Calls SendTelemetry with suitable data if the project is a recognized Q# project. 
        /// </summary>
        internal bool QsProjectLoader(Uri projectFile, out object info)
        {
            info = null;
            //if (projectFile == null || !ValidFileUri(projectFile) || IgnoreFile(projectFile)) return false;
            //var projectInstance = this.ProjectLoader.TryGetQsProjectInstance(projectFile.LocalPath, out var telemetryProps);
            //if (projectInstance == null) return false;

            //var outputDir = projectInstance.GetPropertyValue("OutputPath");
            //var targetFile = projectInstance.GetPropertyValue("TargetFileName");
            //var outputPath = Path.Combine(projectInstance.Directory, outputDir, targetFile);

            //var sourceFiles = GetItemsByType(projectInstance, "QsharpCompile");
            //var projectReferences = GetItemsByType(projectInstance, "ProjectReference");
            //var references = GetItemsByType(projectInstance, "Reference");
            //var version = projectInstance.GetPropertyValue("QsharpLangVersion");

            //info = new ProjectInformation(version, outputPath, sourceFiles, projectReferences, references);
            return true;
        }

        /// <summary>
        /// For each given uri, loads the corresponding project if the uri contains the project file for a Q# project, 
        /// and publishes suitable diagnostics for it.
        /// Throws an ArgumentNullException if the given sequence of uris, or if any of the contained uris is null.
        /// </summary>
        public Task LoadProjectsAsync(IEnumerable<Uri> projects) =>
            this.Projects.LoadProjectsAsync(projects, null, GetOpenFile);

        /// <summary>
        /// If the given uri corresponds to the project file for a Q# project, 
        /// updates that project in the list of tracked projects or adds it if needed, and publishes suitable diagnostics for it.
        /// Throws an ArgumentNullException if the given uri is null.
        /// </summary>
        public Task ProjectDidChangeOnDiskAsync(Uri project) =>
            this.Projects.ProjectChangedOnDiskAsync(project, null, GetOpenFile);

        /// <summary>
        /// Updates all tracked Q# projects that reference the assembly with the given uri
        /// either directly or indirectly via a reference to another Q# project, and publishes suitable diagnostics.
        /// Throws an ArgumentNullException if the given uri is null.
        /// </summary>
        public Task AssemblyDidChangeOnDiskAsync(Uri dllPath) =>
            this.Projects.AssemblyChangedOnDiskAsync(dllPath);

        /// <summary>
        /// To be used whenever a .qs file is changed on disk.
        /// Reloads the file from disk and updates the project(s) which list it as souce file 
        /// if the file is not listed as currently being open in the editor, publishing suitable diagnostics. 
        /// If the file is listed as being open in the editor, updates all load diagnostics for the file, 
        /// but does not update the file content, since the editor manages that one. 
        /// Throws an ArgumentNullException if the given uri is null.
        /// </summary>
        public Task SourceFileDidChangeOnDiskAsync(Uri sourceFile) =>
            this.Projects.SourceFileChangedOnDiskAsync(sourceFile, GetOpenFile);


        // routines related to tracking the editor state

        /// <summary>
        /// To be called whenever a file is opened in the editor.
        /// Does nothing if the given file is listed as to be ignored.
        /// Otherwise publishes suitable diagnostics for it. 
        /// Invokes the given Action showError with a suitable message if the given file cannot be loaded.  
        /// Invokes the given Action logError with a suitable message if the given file cannot be associated with a compilation unit,
        /// or if the given file is already listed as being open in the editor. 
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// Throws an ArgumentNullException if the given content is null.
        /// </summary>
        internal Task OpenFileAsync(TextDocumentItem textDocument,
            Action<string, MessageType> showError = null, Action<string, MessageType> logError = null)
        {
            if (!ValidFileUri(textDocument?.Uri)) throw new ArgumentException("invalid text document identifier");
            if (textDocument.Text == null) throw new ArgumentNullException(nameof(textDocument.Text));
            _ = this.Projects.ManagerTaskAsync(textDocument.Uri, (manager, associatedWithProject) =>
            {
                if (IgnoreFile(textDocument.Uri)) return;
                var newManager = CompilationUnitManager.InitializeFileManager(textDocument.Uri, textDocument.Text, this.Publish, ex =>
                {
                    showError?.Invoke($"Failed to load file '{textDocument.Uri.LocalPath}'", MessageType.Error);
                    manager.LogException(ex);
                });

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail. 
                // To mitigate the impact of failures we choose to just log them as info. 
                var file = this.OpenFiles.GetOrAdd(textDocument.Uri, newManager);
                if (file != newManager) // this may be the case (depending on the editor) e.g. when opening a version control diff ...
                {
                    showError?.Invoke($"Version control and opening multiple versions of the same file in the editor are currently not supported. \n" +
                        $"Intellisense has been disable for the file '{textDocument.Uri.LocalPath}'. An editor restart is required to enable intellisense again.", MessageType.Error);
#if DEBUG
                    if (showError == null) logError?.Invoke("Attempting to open a file that is already open in the editor.", MessageType.Error);
#endif

                    this.IgnoreEditorUpdatesFor(textDocument.Uri);
                    this.OpenFiles.TryRemove(textDocument.Uri, out FileContentManager _);
                    if (!associatedWithProject) _ = manager.TryRemoveSourceFileAsync(textDocument.Uri);
                    this.Publish(new PublishDiagnosticParams { Uri = textDocument.Uri, Diagnostics = new Diagnostic[0] });
                    return;
                }

                if (!associatedWithProject) logError?.Invoke(
                    $"The file {textDocument.Uri.LocalPath} is not associated with a compilation unit. Only syntactic diagnostics are generated."
                    , MessageType.Info);
                _ = manager.AddOrUpdateSourceFileAsync(file);
            });
            // reloading from disk in case we encountered a file already open error above
            return this.Projects.SourceFileChangedOnDiskAsync(textDocument.Uri, GetOpenFile); // NOTE: relies on that the manager task is indeed executed first!
        }

        /// <summary>
        /// To be called whenever a file is changed within the editor (i.e. changes are not necessarily reflected on disk).
        /// Does nothing if the given file is listed as to be ignored.
        /// Throws an ArgumentException if the uri of the text document identifier in the given parameter is null or not an absolute file uri. 
        /// Throws an ArgumentNullException if the given content changes are null. 
        /// </summary>
        internal Task DidChangeAsync(DidChangeTextDocumentParams param)
        {
            if (!ValidFileUri(param?.TextDocument?.Uri)) throw new ArgumentException("invalid text document identifier");
            if (param.ContentChanges == null) throw new ArgumentNullException(nameof(param.ContentChanges));
            return this.Projects.ManagerTaskAsync(param.TextDocument.Uri, (manager, __) =>
            {
                if (IgnoreFile(param.TextDocument.Uri)) return;

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail. 
                // To mitigate the impact of failures we choose to ignore them silently. 
                if (!this.OpenFiles.ContainsKey(param.TextDocument.Uri)) return;
                _ = manager.SourceFileDidChangeAsync(param); // independent on whether the file does or doesn't belong to a project
            });
        }

        /// <summary>
        /// Used to reload the file content when a file is saved.
        /// Does nothing if the given file is listed as to be ignored.
        /// Expects to get the entire content of the file at the time of saving as argument.
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// Throws an ArgumentNullException if the given content is null.
        /// </summary>
        internal Task SaveFileAsync(TextDocumentIdentifier textDocument, string fileContent)
        {
            if (!ValidFileUri(textDocument?.Uri)) throw new ArgumentException("invalid text document identifier");
            if (fileContent == null) throw new ArgumentNullException(nameof(fileContent));
            return this.Projects.ManagerTaskAsync(textDocument.Uri, (manager, __) =>
            {
                if (IgnoreFile(textDocument.Uri)) return;

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail. 
                // To mitigate the impact of failures we choose to ignore them silently and do our best to recover. 
                if (!this.OpenFiles.TryGetValue(textDocument.Uri, out var file))
                {
                    file = CompilationUnitManager.InitializeFileManager(textDocument.Uri, fileContent, this.Publish, manager.LogException);
                    this.OpenFiles.TryAdd(textDocument.Uri, file);
                    _ = manager.AddOrUpdateSourceFileAsync(file);
                }
                else _ = manager.AddOrUpdateSourceFileAsync(file, fileContent); // let's reload the file content on saving
            });
        }

        /// <summary>
        /// To be called whenever a file is closed in the editor.
        /// Does nothing if the given file is listed as to be ignored.
        /// Otherwise the file content is reloaded from disk (in case changes in the editor are discarded without closing), and the diagnostics are updated.
        /// Invokes the given Action onError with a suitable message if the given file is not listed as being open in the editor. 
        /// Throws an ArgumentException if the uri of the given text document identifier is null or not an absolute file uri. 
        /// </summary>
        internal Task CloseFileAsync(TextDocumentIdentifier textDocument, Action<string, MessageType> onError = null)
        {
            if (!ValidFileUri(textDocument?.Uri)) throw new ArgumentException("invalid text document identifier");
            _ = this.Projects.ManagerTaskAsync(textDocument.Uri, (manager, associatedWithProject) => // needs to be *first* (due to the modification of OpenFiles)
            {
                if (IgnoreFile(textDocument.Uri)) return;

                // Currently it is not possible to handle both the behavior of VS and VS Code for changes on disk in a manner that will never fail. 
                // To mitigate the impact of failures we choose to ignore them silently.
                var removed = this.OpenFiles.TryRemove(textDocument.Uri, out FileContentManager __);
#if DEBUG
                if (!removed) onError?.Invoke($"Attempting to close file '{textDocument.Uri.LocalPath}' that is not currently listed as open in the editor.", MessageType.Error);
#endif
                if (!associatedWithProject) _ = manager.TryRemoveSourceFileAsync(textDocument.Uri);
                this.Publish(new PublishDiagnosticParams { Uri = textDocument.Uri, Diagnostics = new Diagnostic[0] });
            });
            // When edits are made in a file, but those are discarded by closing the file and hitting "no, don't save",
            // no notification is sent for the now discarded changes;
            // hence we reload the file content from disk upon closing. 
            return this.Projects.SourceFileChangedOnDiskAsync(textDocument.Uri, GetOpenFile); // NOTE: relies on that the manager task is indeed executed first!
        }


        // routines related to providing information for editor commands

        /// <summary>
        /// Returns the workspace edit that describes the changes to be done if the symbol at the given position - if any - is renamed to the given name. 
        /// Returns null if no symbol exists at the specified position, 
        /// or if the specified uri is not a valid file uri. 
        /// or if some parameters are unspecified (null) or inconsistent with the tracked editor state. 
        /// </summary>
        public WorkspaceEdit Rename(RenameParams param, bool versionedChanges = false) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.Rename(param, versionedChanges) : null;

        /// <summary>
        /// Returns the source file and position where the item at the given position is declared at,
        /// if such a declaration exists, and returns the given position and file otherwise.
        /// Returns null if the given file is listed as to be ignored or if the information cannot be determined at this point.
        /// </summary>
        public Location DefinitionLocation(TextDocumentPositionParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.DefinitionLocation(param) : null;

        /// <summary>
        /// Returns the signature help information for a call expression if there is such an expression at the specified position.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content
        /// or if no call expression exists at the specified position at this time
        /// or if no signature help information can be provided for the call expression at the specified position.
        /// </summary>
        public SignatureHelp SignatureHelp(TextDocumentPositionParams param, MarkupKind format = MarkupKind.PlainText) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.SignatureHelp(param, format) : null;

        /// <summary>
        /// Returns information about the item at the specified position as Hover information.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no token exists at the specified position at this time.
        /// </summary>
        public Hover HoverInformation(TextDocumentPositionParams param, MarkupKind format = MarkupKind.PlainText) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.HoverInformation(param, format) : null;

        /// <summary>
        /// Returns an array with all usages of the identifier at the given position (if any) as DocumentHighlights.
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no identifier exists at the specified position at this time.
        /// </summary>
        public DocumentHighlight[] DocumentHighlights(TextDocumentPositionParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.DocumentHighlights(param) : null;

        /// <summary>
        /// Returns an array with all locations where the symbol at the given position - if any - is referenced. 
        /// Returns null if the given file is listed as to be ignored,
        /// or if some parameters are unspecified (null),
        /// or if the specified position is not a valid position within the currently processed file content,
        /// or if no symbol exists at the specified position at this time.
        /// </summary>
        public Location[] SymbolReferences(ReferenceParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.SymbolReferences(param) : null;

        /// <summary>
        /// Returns the SymbolInformation for each namespace declaration, type declaration, and function or operation declaration.
        /// Returns null if the given file is listed as to be ignored, or if the given parameter or its uri is null.
        /// </summary>
        public SymbolInformation[] DocumentSymbols(DocumentSymbolParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.DocumentSymbols(param) : null;

        /// <summary>
        /// Returns a dictionary of workspace edits suggested by the compiler for the given location and context.
        /// The keys of the dictionary are suitable titles for each edit that can be presented to the user. 
        /// Returns null if the given file is listed as to be ignored, or if the given parameter or its uri is null.
        /// </summary>
        public ImmutableDictionary<string, WorkspaceEdit> CodeActions(CodeActionParams param) =>
            ValidFileUri(param?.TextDocument?.Uri) && !IgnoreFile(param.TextDocument.Uri) ? this.Projects.CodeActions(param) : null;
    }
}
