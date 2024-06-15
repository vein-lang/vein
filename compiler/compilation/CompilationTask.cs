namespace vein.compilation;

using MoreLinq;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ishtar;
using vein;
using cmd;
using ishtar.emit;
using extensions;
using pipes;
using project;
using runtime;
using stl;
using syntax;
using vein.fs;
using vein.styles;

public partial class CompilationTask(CompilationTarget target, CompileSettings flags)
{
    internal VeinProject Project { get; set; } = target.Project;
    internal CompilationTarget Target { get; set; } = target;

    internal readonly CompileSettings _flags = flags;
    internal readonly VeinSyntax syntax = new();
    internal readonly Dictionary<FileInfo, string> Sources = new ();
    internal ProgressTask Status;
    internal ProgressContext StatusCtx;
    internal VeinModuleBuilder module;
    internal GeneratorContext Context;
    internal List<VeinArtifact> artifacts { get; } = new();

    private readonly Dictionary<IdentifierExpression, VeinClass> KnowClasses = new ();

    private static string PleaseReportProblemInto()
        => $"Please report the problem into 'https://github.com/vein-lang/vein/issues'.";

    public static IReadOnlyCollection<CompilationTarget> Run(DirectoryInfo info, CompileSettings settings) => AnsiConsole
        .Progress()
        .AutoClear(false)
        .AutoRefresh(true)
        .HideCompleted(true)
        .Columns(
            new ProgressBarColumn(),
            new PercentageColumn(),
            new SpinnerColumn { Spinner = Spinner.Known.Dots8Bit, CompletedText = "✅", FailedText = "❌" },
            new TaskDescriptionColumn { Alignment = Justify.Left })
        .Start(ctx => Run(info, ctx, settings));
    
    public static IReadOnlyCollection<CompilationTarget> Run(DirectoryInfo info, ProgressContext context, CompileSettings settings)
    {
        var collection =
            Collect(info, context.AddTask("Collect projects"), context, settings);
        var list = new List<VeinModule>();
        var shardStorage = new ShardStorage();

        Parallel.ForEach(collection, (target) =>
        {
            if (target.Project.Dependencies.Packages.Count == 0)
                return;
            var task = context.AddTask($"Collect modules for '{target.Project.Name}'...").IsIndeterminate();
            target.Resolver.AddSearchPath(target.Project.WorkDir);


            void add_deps_path(IReadOnlyCollection<PackageReference> refs)
            {
                foreach (var package in refs)
                {
                    target.Resolver.AddSearchPath(shardStorage.GetPackageSpace(package.Name, package.Version)
                        .SubDirectory("lib"));
                    var manifest = shardStorage.GetManifest(package.Name, package.Version);

                    if (manifest is null)
                    {
                        Log.Error($"Failed to load [orange]'{package.Name}@{package.Version}'[/] manifest.", target);
                        continue;
                    }

                    add_deps_path(manifest.Dependencies.AsReadOnly());
                }
            }

            add_deps_path(target.Project.Dependencies.Packages);

            foreach (var package in target.Project.Dependencies.Packages)
            {
                try
                {
                    list.Add(target.Resolver.ResolveDep(package, list));
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to load dependencies.");
                    Log.Error($"Try 'veinc restore'");
                    throw;
                }
            }
            task.StopTask();
        });

        Parallel.ForEachAsync(collection, async (target, token) =>
        {
            while (!token.IsCancellationRequested)
            {
                if (target.Dependencies.Any(x => x.Status == CompilationStatus.NotStarted))
                    await Task.Delay(200, token);
                else
                    break;
            }

            if (target.Dependencies.Any(x => x.Status == CompilationStatus.Failed))
                return;
            target.Dependencies
                .SelectMany(x => x.Artifacts)
                .OfType<BinaryArtifact>()
                .Select(x => target.Resolver.ResolveDep(x, list))
                .Pipe(list.Add)
                .Consume();

            target.LoadedModules.AddRange(list);

            var c = new CompilationTask(target, settings)
            {
                StatusCtx = context,
                Status = target.Task
            };

            var status = c.ProcessFiles(target.Project.Sources, target.LoadedModules)
                ? CompilationStatus.Success
                : CompilationStatus.Failed;
            if (status is CompilationStatus.Success)
                PipelineRunner.Run(c, target);
            if (status is CompilationStatus.Success)
                target.AcceptArtifacts(c.artifacts.AsReadOnly());
            target.Status = status;
        }).Wait();

        return collection;
    }
    private bool ProcessFiles(IReadOnlyCollection<FileInfo> files, IReadOnlyCollection<VeinModule> deps)
    {
        if (_flags.IsNeedDebuggerAttach)
        {
            var task = StatusCtx.AddTask($"[green]Waiting debugger[/]...").IsIndeterminate();
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(400);
            }
            task.SuccessTask();
        }
        foreach (var file in files)
        {
            Status.VeinStatus($"Read [grey]'{file.Name}'[/]...");
            var text = File.ReadAllText(file.FullName);
            if (text.StartsWith("#ignore"))
                continue;
            Sources.Add(file, text);
        }

        var read_task = StatusCtx.AddTask($"[gray]Compiling files[/]...");

        read_task.MaxValue = Sources.Count;

        var asset = Cache.Validate(Target, read_task, Sources);

        if (!Target.HasChanged)
        {
            if (Target.Dependencies.All(x => !x.HasChanged))
            {
                read_task.SuccessTask();
                Status.VeinStatus("[gray]unchanged[/]");
                return true;
            }

            Target.HasChanged = true;
        }

        read_task.Value(0);

        foreach (var (key, value) in Sources)
        {
            read_task.VeinStatus($"Compile [grey]'{key.Name}'[/]...");
            read_task.Increment(1);
            try
            {
                var result = syntax.CompilationUnit.ParseVein(value);
                result.FileEntity = key;
                result.SourceText = value;
                // apply root namespace into includes
                result.Includes.Add($"global::{result.Name}");
                Target.AST.Add(key, result);
            }
            catch (VeinParseException e)
            {
                Log.Defer.Error($"[red bold]{e.Message.Trim().EscapeMarkup()}[/]", e.AstItem, key);
                read_task.FailTask();
                return false;
            }
        }

        read_task.SuccessTask();

        Context = new(new(_flags.DisableOptimization));

        module = new(Project.Name, Project.Version.Version, Types.Storage);

        Context.Module = module;
        Context.Module.Deps.AddRange(deps);

        Status.IsIndeterminate();

        try
        {
            LoadAliases();

            Target.AST.Select(x => (x.Key, x.Value))
                .Pipe(x => Status.VeinStatus($"Linking [grey]'{x.Key.Name}'[/]..."))
                .SelectMany(LinkClasses)
                .ToList()
                .Pipe(LinkMetadata)
                .Pipe(ShitcodePlug)
                .Select(x => (LinkMethods(x), x))
                .ToList()
                .Pipe(x => x.Item1.Transition(
                    methods => methods.ForEach(GenerateBody),
                    fields => fields.ForEach(GenerateField),
                    props => props.ForEach(GenerateProp)))
                .Select(x => x.x)
                .Pipe(GenerateCtor)
                .Pipe(GenerateStaticCtor)
                .Pipe(ValidateInheritance)
                .ToList()
                .Pipe(x => x.clazz.Methods.OfExactType<MethodBuilder>().Pipe(PostgenerateBody).Consume())
                .Consume();

            Cache.SaveAstAsset(Target);
        }
        catch (SkipStatementException) { }

        Log.EnqueueErrorsRange(Context.Errors);
        if (Log.errors.Count == 0)
            Status.VeinStatus($"Result assembly [orange]'{module.Name}, {module.Version}'[/].");
        if (_flags.PrintResultType)
        {
            var table = new Table();
            table.AddColumn(new TableColumn("Type").Centered());
            table.Border(TableBorder.Rounded);
            foreach (var @class in module.class_table)
                table.AddRow(new Markup($"[blue]{@class.FullName.NameWithNS}[/]"));
            AnsiConsole.Write(table);
        }
        Status.Increment(100);
        if (Log.errors.Count == 0)
            Cache.SaveAssets(asset);
        return Log.errors.Count == 0;
    }
}
