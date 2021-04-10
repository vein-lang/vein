namespace wave.langserver
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Build.Evaluation;
    using Microsoft.VisualStudio.LanguageServer.Protocol;

    internal class EditorState
    {
        private readonly ProjectLoader _projectLoader;
        private readonly Action<string, MessageType>? _log;
        private readonly Action<Exception> _internalError;
        private readonly ProcessingQueue load;
        private readonly ConcurrentDictionary<Uri, Project> projects;

        public EditorState(ProjectLoader projectLoader, Action<string, MessageType>? log = null, Action<Exception> internalError = null)
        {
            _projectLoader = projectLoader;
            _log = log;
            _internalError = internalError;
            ignoreEditorUpdatesForFiles = new ConcurrentDictionary<Uri, byte>();
        }
        
        
        
        public void InitializeFile(string file)
        {
            
        }

        public Task LoadProjectsAsync(ImmutableArray<Uri?> initialProjects)
        {
            throw new NotImplementedException();
        }
        
        private static bool ValidFileUri(Uri? file) => file != null && file.IsFile && file.IsAbsoluteUri;
        
        
        private readonly ConcurrentDictionary<Uri, byte> ignoreEditorUpdatesForFiles;
        
        /// <summary>
        /// needed to determine if the reality of a source file that has changed on disk is indeed given by the content on disk,
        /// or whether its current state as it is in the editor needs to be preserved
        /// </summary>
        private readonly ConcurrentDictionary<Uri, FileContent> openFiles =
            new ConcurrentDictionary<Uri, FileContent>();
        
        internal void IgnoreEditorUpdatesFor(Uri uri) => this.ignoreEditorUpdatesForFiles.TryAdd(uri, default);
        
        private bool IgnoreFile(Uri? file) => 
            file == null || 
            this.ignoreEditorUpdatesForFiles.ContainsKey(file) || 
            file.LocalPath.ToLowerInvariant().Contains("vctmp");

        public Task OpenFileAsync(TextDocumentItem textDocument, 
            Action<string, MessageType> showError, 
            Action<string, MessageType> logError)
        {
            if (!ValidFileUri(textDocument.Uri))
            {
                throw new ArgumentException("invalid text document identifier");
            }
            _ = this.ManagerTaskAsync(textDocument.Uri, (associatedWithProject) =>
            {
                if (this.IgnoreFile(textDocument.Uri))
                    return;
                var file = this.openFiles.GetOrAdd(textDocument.Uri, textDocument.Uri);
                if (!associatedWithProject)
                {
                    logError?.Invoke(
                        $"The file {textDocument.Uri.LocalPath} is not associated with a compilation unit. Only syntactic diagnostics are generated.",
                        MessageType.Info);
                }

               // _ = manager.AddOrUpdateSourceFileAsync(file);
            });
            
            return Task.CompletedTask;//this.projects.SourceFileChangedOnDiskAsync(textDocument.Uri, this.GetOpenFile); // NOTE: relies on that the manager task is indeed executed first!
        }
        
        
        internal class ProjectProperties
        {
            public readonly string Version;
            public readonly string OutputPath;
            public readonly bool IsExecutable;
            public readonly string ProcessorArchitecture;
            public readonly bool ExposeReferencesViaTestNames;

            public ProjectProperties(
                string version,
                string outputPath,
                bool isExecutable,
                string processorArchitecture,
                bool loadTestNames)
            {
                this.Version = version ?? "";
                this.OutputPath = outputPath;
                this.IsExecutable = isExecutable;
                this.ProcessorArchitecture = processorArchitecture;
                this.ExposeReferencesViaTestNames = loadTestNames;
            }
        }
        
        public class ProjectInformation
        {
            public delegate bool Loader(Uri projectFile, [NotNullWhen(true)] out ProjectInformation? projectInfo);

            internal readonly ProjectProperties Properties;
            public readonly ImmutableArray<string> SourceFiles;
            public readonly ImmutableArray<string> ProjectReferences;
            public readonly ImmutableArray<string> References;

            internal static ProjectInformation Empty(
                string version,
                string outputPath) =>
                new ProjectInformation(
                    version,
                    outputPath,
                    false,
                    "Unspecified",
                    false,
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>());

            public ProjectInformation(
                string version,
                string outputPath,
                bool isExecutable,
                string processorArchitecture,
                bool loadTestNames,
                IEnumerable<string> sourceFiles,
                IEnumerable<string> projectReferences,
                IEnumerable<string> references)
            {
                this.Properties = new ProjectProperties(
                    version, outputPath, isExecutable, processorArchitecture, loadTestNames);
                this.SourceFiles = sourceFiles.ToImmutableArray();
                this.ProjectReferences = projectReferences.ToImmutableArray();
                this.References = references.ToImmutableArray();
            }
        }
        private class Project : IDisposable
        {
            public readonly Uri ProjectFile;

            public Uri? OutputPath { get; private set; }

            public ProjectProperties Properties { get; private set; }

            private bool isLoaded;
            
            /// <summary>
            /// contains the path of all specified source files,
            /// regardless of whether or not the path is valid, the file exists and could be loaded
            /// </summary>
            private ImmutableHashSet<Uri> specifiedSourceFiles = ImmutableHashSet<Uri>.Empty;
            
            /// <summary>
            /// contains the path to the dlls of all specified references,
            /// regardless of whether or not the path is valid, the file exists and could be loaded
            /// </summary>
            private ImmutableHashSet<Uri> specifiedReferences = ImmutableHashSet<Uri>.Empty;

            /// <summary>
            /// contains the path to the *project* file of all specified project references,
            /// regardless of whether or not the path is valid, and a project with the corresponding uri exists
            /// </summary>
            private ImmutableHashSet<Uri> specifiedProjectReferences = ImmutableHashSet<Uri>.Empty;

            /// <summary>
            /// contains the uris to all source files that have been successfully loaded and are incorporated into the compilation
            /// </summary>
            private ImmutableHashSet<Uri> loadedSourceFiles;

            private readonly ProcessingQueue processing;
            private readonly Action<string, MessageType> log;
            
            public void Dispose()
            {}
            
            internal Project(
                Uri projectFile,
                ProjectInformation projectInfo,
                Action<Exception>? onException,
                Action<PublishDiagnosticParams>? publishDiagnostics,
                Action<string, MessageType>? log)
            {
                this.ProjectFile = projectFile;
                this.Properties = projectInfo.Properties;
                
                var version = Version.TryParse(projectInfo.Properties.Version, out Version v) ? v : null;
                if (projectInfo.Properties.Version.Equals("Latest", StringComparison.InvariantCultureIgnoreCase))
                {
                    version = new Version(0, 3);
                }
                var ignore = version == null || version < new Version(0, 3) ? true : false;
                
                this.processing = new ProcessingQueue(onException);
                this.log = log ?? ((msg, severity) => Console.WriteLine($"{severity}: {msg}"));

                this.loadedSourceFiles = ImmutableHashSet<Uri>.Empty;
            }
            
            /// <summary>
            /// If the given file is a loaded source file of this project,
            /// executes the given task for that file on the CompilationUnitManager.
            /// </summary>
            public bool ManagerTask(Uri file, Action executeTask, IDictionary<Uri, Uri?> projectOutputPaths)
            {
                this.processing.QueueForExecution(
                    () =>
                    {
                        if (!this.specifiedSourceFiles.Contains(file))
                        {
                            return false;
                        }
                        if (!this.loadedSourceFiles.Contains(file))
                        {
                            return false;
                        }
                        executeTask();
                        return true;
                    },
                    out bool didExecute);
                return didExecute;
            }
        }
        
        /// <summary>
        /// If the given file can be uniquely associated with a compilation unit,
        /// executes the given Action on the CompilationUnitManager of that project (if one exists), passing true as second argument.
        /// Executes the given Action on the DefaultManager otherwise, passing false as second argument.
        /// </summary>
        public Task ManagerTaskAsync(Uri file, Action<bool> executeTask) =>
            this.load.QueueForExecutionAsync(() =>
            {
                var didExecute = false;
                var options = new ParallelOptions { TaskScheduler = TaskScheduler.Default };
                var projectOutputPaths = this.projects.ToImmutableDictionary(
                    p => p.Key, p => p.Value.OutputPath);
                Parallel.ForEach(this.projects.Values, options, project =>
                {
                    if (project.ManagerTask(file, () => executeTask(true), projectOutputPaths))
                    {
                        didExecute = true;
                    }
                });
                if (!didExecute) executeTask(false);
            });
    }
}