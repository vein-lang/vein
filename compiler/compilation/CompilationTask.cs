namespace vein.compilation
{
    using MoreLinq;
    using Spectre.Console;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using ishtar;
    using vein;
    using cmd;
    using exceptions;
    using ishtar.emit;
    using extensions;
    using pipes;
    using project;
    using runtime;
    using stl;
    using syntax;
    using static runtime.VeinTypeCode;
    using reflection;
    using vein.fs;
    using vein.styles;

    public enum CompilationStatus
    {
        NotStarted,
        Success,
        Failed
    }

    public class CompilationLog
    {
        public Queue<string> Info { get; } = new();
        public Queue<string> Warn { get; } = new();
        public Queue<string> Error { get; } = new();
    }
    public class CompilationTarget : IEquatable<CompilationTarget>
    {
        private CompilationStatus _status { get; set; } = CompilationStatus.NotStarted;

        public VeinProject Project { get; }

        public CompilationStatus Status
        {
            get => _status;
            set
            {
                if (value == CompilationStatus.Failed)
                    Task.FailTask();
                if (value == CompilationStatus.Success)
                {
                    Task.MaxValue = 10;
                    Task.Value(10);
                    Task.SuccessTask();
                }
                this._status = value;
            }
        }

        public CompilationTarget This() => this;
        public CompilationLog Logs { get; } = new();
        public List<CompilationTarget> Dependencies { get; } = new();
        public ProgressTask Task { get; set; }
        public IReadOnlyCollection<VeinArtifact> Artifacts { get; private set; } = new List<VeinArtifact>();
        public HashSet<VeinModule> LoadedModules { get; } = new();
        public AssemblyResolver Resolver { get; }
        public CompilationTarget(VeinProject p, ProgressContext ctx)
            => (Project, Task, Resolver) =
               (p, ctx.AddTask($"[red](waiting)[/] Compile [orange]'{p.Name}'[/]...", allowHide: false)
                   .WithState("project", p), new(this));


        // Indicate files has changed
        public bool HasChanged { get; set; }

        public Dictionary<FileInfo, DocumentDeclaration> AST { get; } = new();

        public DirectoryInfo GetOutputDirectory()
            => new(Path.Combine(Project.WorkDir.FullName, "bin"));

        public CompilationTarget AcceptArtifacts(IReadOnlyCollection<VeinArtifact> artifacts)
        {
            Artifacts = artifacts;
            foreach (var artifact in artifacts)
                Log.Info($"Populated artifact with [purple]'{artifact.Kind}'[/] type, path: [gray]'{artifact.Path}'[/]", this);
            return this;
        }


        #region IEquatable

        public bool Equals(CompilationTarget other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Project.Name, other.Project.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CompilationTarget)obj);
        }

        public override int GetHashCode() => (Project != null ? Project.Name.GetHashCode() : 0);

        #endregion
    }

    public class CompilationTask
    {
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

        public static IReadOnlyCollection<CompilationTarget> Collect(DirectoryInfo info, ProgressTask task, ProgressContext ctx)
        {
            var files = info.EnumerateFiles("*.vproj", SearchOption.AllDirectories)
                .ToList()
                .AsReadOnly();

            if (!files.Any())
            {
                Log.Error($"Projects not found in [orange]'{info}'[/] directory.");
                return null;
            }

            task.IsIndeterminate(false);
            task.MaxValue = files.Count;

            var targets = new Dictionary<VeinProject, CompilationTarget>();

            foreach (var file in files)
            {
                var p = VeinProject.LoadFrom(file);

                if (p is null)
                {
                    Log.Error($"Failed to load [orange]'{file}'[/] project.");
                    task.FailTask();
                    return null;
                }
                task.Increment(1);
                task.VeinStatus($"Reading [orange]'{p.Name}'[/]");

                var t = new CompilationTarget(p, ctx);

                targets.Add(p, t);
            }

            task.IsIndeterminate();
            task.Description("[gray]Build dependency tree...[/]");

            foreach (var compilationTarget in targets.Values.ToList())
                foreach (var reference in compilationTarget.Project.Dependencies.Projects)
                {
                    var path = new Uri(reference.path, UriKind.RelativeOrAbsolute).IsAbsoluteUri
                    ? reference.path
                    : Path.Combine(info.FullName, reference.path);

                    var fi = new FileInfo(path);

                    if (!fi.Exists)
                    {
                        Log.Error($"Failed to load [orange]'{fi.FullName}'[/] project. [[not found]]", compilationTarget);
                        continue;
                    }

                    var project = VeinProject.LoadFrom(fi);
                    if (targets.ContainsKey(project))
                        compilationTarget.Dependencies.Add(targets[project]);
                    else
                    {
                        targets.Add(project, new CompilationTarget(project, ctx));
                        compilationTarget.Dependencies.Add(targets[project]);
                    }
                }

            task.StopTask();

            return targets.Values.ToList().AsReadOnly();
        }


        public static IReadOnlyCollection<CompilationTarget> Run(DirectoryInfo info, ProgressContext context, CompileSettings settings)
        {
            var collection =
                Collect(info, context.AddTask("Collect projects"), context);
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
                    list.Add(target.Resolver.ResolveDep(package, list));
                task.StopTask();
            });

            Parallel.ForEachAsync(collection, async (target, token) =>
            {
                var q = collection;
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

        public CompilationTask(CompilationTarget target, CompileSettings flags)
        {
            _flags = flags;
            Project = target.Project;
            Target = target;
        }

        internal VeinProject Project { get; set; }
        internal CompilationTarget Target { get; set; }

        internal readonly CompileSettings _flags;
        internal readonly VeinSyntax syntax = new();
        internal readonly Dictionary<FileInfo, string> Sources = new ();
        internal ProgressTask Status;
        internal ProgressContext StatusCtx;
        internal VeinModuleBuilder module;
        internal GeneratorContext Context;
        internal List<VeinArtifact> artifacts { get; } = new();

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
                    Log.Defer.Error($"[red bold]{e.Message.Trim().EscapeMarkup()}[/]\n\tin '[orange bold]{key}[/]'.");
                    read_task.FailTask();
                    return false;
                }
            }

            read_task.SuccessTask();

            Context = new GeneratorContext();

            module = new VeinModuleBuilder(Project.Name);

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

        public List<(ClassBuilder clazz, MemberDeclarationSyntax member)>
            LinkClasses((FileInfo, DocumentDeclaration doc) tuple)
            => LinkClasses(tuple.doc);
        public List<(ClassBuilder clazz, MemberDeclarationSyntax member)> LinkClasses(DocumentDeclaration doc)
        {
            var classes = new List<(ClassBuilder clazz, MemberDeclarationSyntax member)>();

            foreach (var member in doc.Members)
            {
                if (member is ClassDeclarationSyntax clazz)
                {
                    Status.VeinStatus($"Regeneration class [grey]'{clazz.Identifier}'[/]");
                    clazz.OwnerDocument = doc;
                    var result = CompileClass(clazz, doc);
                    Context.Classes.Add(result.FullName, result);
                    classes.Add((result, clazz));
                }
                else if (member is AspectDeclarationSyntax aspect)
                {
                    Status.VeinStatus($"Regeneration aspect [grey]'{aspect.Identifier}'[/]");
                    aspect.OwnerDocument = doc;
                    var result = CompileAspect(aspect, doc);
                    Context.Classes.Add(result.FullName, result);
                    classes.Add((result, aspect));
                }
                else
                    Log.Defer.Warn($"[grey]Member[/] [yellow underline]'{member.GetType().Name}'[/] [grey]is not supported.[/]");
            }

            return classes;
        }

        public ClassBuilder CompileAspect(AspectDeclarationSyntax member, DocumentDeclaration doc)
        {
            var name = member.Identifier.ExpressionString.EndsWith("Aspect")
                ? $"global::{doc.Name}/{member.Identifier.ExpressionString}"
                : $"global::{doc.Name}/{member.Identifier.ExpressionString}Aspect";

            var clazz = module.DefineClass(name)
                .WithIncludes(doc.Includes);

            clazz.Flags |= ClassFlags.Public; // temporary all aspect has public
            clazz.Flags |= ClassFlags.Aspect; // indicate when it class is a aspect

            clazz.Parents.Add(VeinCore.AspectClass);

            var getUsages = clazz.DefineMethod("getUsages", MethodFlags.Override | MethodFlags.Public, TYPE_I4.AsClass());

            var aspectUsage = member.Aspects.FirstOrDefault(x => x.IsAspectUsage);
            var flags = 0;

            if (aspectUsage is null)
                flags = 1 << 2;
            else
            {
                // TODO
            }

            getUsages
                .GetGenerator()
                .Emit(OpCodes.LDC_I4_S, flags)
                .Emit(OpCodes.RET);

            return clazz;
        }

        public ClassBuilder CompileClass(ClassDeclarationSyntax member, DocumentDeclaration doc)
        {
            void _defineClass(ClassBuilder clz)
            {
                KnowClasses.Add(member.Identifier, clz);
                if (member.Aspects.FirstOrDefault(x => x.IsAlias)?.Args?.SingleOrDefault().Value is not StringLiteralExpressionSyntax alias)
                    return;
                KnowClasses.Add(new IdentifierExpression(alias.Value), clz);
            }


            if (member.IsForwardedType)
            {
                var result = VeinCore.All.
                    FirstOrDefault(x =>
                        x.FullName.Name.Equals(member.Identifier.ExpressionString));

                if (result is not null)
                {
                    var clz = new ClassBuilder(module, result);
                    module.class_table.Add(clz);

                    clz.Includes.AddRange(doc.Includes);
                    TypeForwarder.Indicate(clz);
                    CompileAnnotation(member, doc, clz);
                    _defineClass(clz);
                    return clz;
                }

                throw new ForwardedTypeNotDefinedException(member.Identifier.ExpressionString);
            }

            var clazz = module.DefineClass($"global::{doc.Name}/{member.Identifier.ExpressionString}")
                .WithIncludes(doc.Includes);
            _defineClass(clazz);
            CompileAnnotation(member, doc, clazz);
            return clazz;
        }

        private void ShitcodePlug((ClassBuilder clazz, MemberDeclarationSyntax member) clz)
            => ShitcodePlug(clz.clazz);

        private void ShitcodePlug(ClassBuilder clz)
        {
            var dd = clz.Parents.FirstOrDefault(x => x.FullName == VeinCore.ValueTypeClass.FullName);
            if (dd is not null && dd != VeinCore.ValueTypeClass)
            {
                clz.Parents.Remove(dd);
                clz.Parents.Add(VeinCore.ValueTypeClass);
            }
            var ss = clz.Parents.FirstOrDefault(x => x.FullName == VeinCore.ObjectClass.FullName);
            if (ss is not null && ss != VeinCore.ObjectClass)
            {
                clz.Parents.Remove(ss);
                clz.Parents.Add(VeinCore.ObjectClass);
            }
        }

        public void CompileAnnotation(FieldDeclarationSyntax dec, DocumentDeclaration doc, VeinField field) =>
            CompileAnnotation(dec.Aspects, x =>
                $"aspect/{x.Name}/class/{field.Owner.Name}/field/{dec.Field.Identifier}.",
                doc, field, AspectTarget.Field);

        public void CompileAnnotation(MethodDeclarationSyntax dec, DocumentDeclaration doc, VeinMethod method) =>
            CompileAnnotation(dec.Aspects, x =>
                $"aspect/{x.Name}/class/{method.Owner.Name}/method/{method.Name}.",
                doc, method, AspectTarget.Method);

        public void CompileAnnotation(ClassDeclarationSyntax dec, DocumentDeclaration doc, VeinClass clazz) =>
            CompileAnnotation(dec.Aspects, x =>
                $"aspect/{x.Name}/class/{clazz.Name}.", doc, clazz, AspectTarget.Class);

        private void CompileAnnotation(
            List<AspectSyntax> aspects,
            Func<AspectSyntax, string> nameGenerator,
            DocumentDeclaration doc, IAspectable aspectable,
            AspectTarget target)
        {
            foreach (var annotation in aspects.TrimNull().Where(annotation => annotation.Args.Length != 0))
            {
                var aspect = new Aspect(annotation.Name.ToString(), target);
                foreach (var (exp, index) in annotation.Args.Select((x, y) => (x, y)))
                {

                    if (exp.CanOptimizationApply())
                    {
                        var optimized = exp.ForceOptimization();
                        var converter = optimized.GetTypeCode().GetConverter();
                        var calculated = converter(optimized.ExpressionString);
                        module.WriteToConstStorage($"{nameGenerator(annotation)}_{index}",
                            calculated);
                        aspect.DefineArgument(index, calculated);
                    }
                    else
                        Log.Defer.Error("[red bold]Aspects require compile-time constant.[/]", annotation, doc);
                }
                aspectable.Aspects.Add(aspect);
            }
        }

        public void LinkMetadata((ClassBuilder @class, MemberDeclarationSyntax member) x)
        {
            if (x.member is not ClassDeclarationSyntax)
                return;

            var (@class, member) = (x.@class, x.member as ClassDeclarationSyntax);
            var doc = member.OwnerDocument;
            @class.Flags = GenerateClassFlags(member);

            if (member.IsInterface)
            {
                @class.Flags |= ClassFlags.Abstract;
                @class.Flags |= ClassFlags.Interface;
                @class.Flags |= ClassFlags.Public;
            }

            var owners = member.Inheritances;

            // ignore core base types
            if (member.Identifier.ExpressionString is "Object" or "ValueType") // TODO
                return;
            if (!owners.Any() && !@class.Parents.Any())
            {
                // fallback transform
                if (member.IsStruct)
                {
                    owners.Add(new TypeSyntax(new IdentifierExpression("ValueType"))
                        .SetPos<TypeSyntax>(member.Identifier.Transform));
                }
                else
                {
                    owners.Add(new TypeSyntax(new IdentifierExpression("Object"))
                        .SetPos<TypeSyntax>(member.Identifier.Transform));
                }
            }

            owners
                .Select(owner => FetchType(owner, doc))
                .Where(p => !@class.Parents.Contains(p))
                .Pipe(p => @class.Parents.Add(p)).Consume();
        }

        public void ValidateInheritance((ClassBuilder @class, MemberDeclarationSyntax member) x)
        {
            if (x.member is not ClassDeclarationSyntax member)
                return;
            var (@class, _) = x;
            ValidateInheritanceInterfaces(@class, member);
            ValidateCollisionsMethods(@class, member);
        }

        public void ValidateInheritanceInterfaces(ClassBuilder @class, ClassDeclarationSyntax member)
        {
            var prepairedAbstracts = @class.Parents
                .SelectMany(x => x.Methods)
                .Where(x => !x.IsPrivate)
                .Where(x => !x.IsStatic)
                .Where(x => x.IsAbstract);

            foreach (var method in prepairedAbstracts.Where(x => !@class.ContainsImpl(x)))
            {
                Log.Defer.Error(
                    $"[red]'{@class.Name}'[/] does not implement inherited abstract member [red]'{method.Owner.Name}.{method.Name}'[/]"
                    , member.Identifier, member.OwnerDocument);
            }
        }
        public void ValidateCollisionsMethods(ClassBuilder @class, ClassDeclarationSyntax member)
        {
            var prepairedOthers = @class.Parents
                .SelectMany(x => x.Methods)
                .Where(x => !x.IsPrivate)
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsSpecial);

            foreach (var method in prepairedOthers.Where(@class.Contains).Where(x => !x.IsOverride))
            {
                Log.Defer.Warn(
                    $"[yellow]'{method.Name}' hides inherited member '{method.Name}'.[/]",
                    member.Identifier, member.OwnerDocument);
            }
        }

        public (
            List<(MethodBuilder method, MethodDeclarationSyntax syntax)> methods,
            List<(VeinField field, FieldDeclarationSyntax syntax)> fields,
            List<(VeinProperty field, PropertyDeclarationSyntax syntax)> props)
            LinkMethods((ClassBuilder @class, MemberDeclarationSyntax member) x)
        {
            var methods = new List<(MethodBuilder method, MethodDeclarationSyntax syntax)>();
            var fields = new List<(VeinField field, FieldDeclarationSyntax syntax)>();
            var props = new List<(VeinProperty field, PropertyDeclarationSyntax syntax)>();

            if (x.member is not ClassDeclarationSyntax clazzSyntax)
                return (methods, fields, props);
            var (@class, _) = x;
            var doc = clazzSyntax.OwnerDocument;

            foreach (var member in clazzSyntax.Members)
            {
                switch (member)
                {
                    case IPassiveParseTransition transition when member.IsBrokenToken:
                        var e = transition.Error;
                        //var pos = member.Transform.pos;
                        //var err_line = member.Transform.DiffErrorFull(doc);
                        //errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/] \n\t" +
                        //           $"at '[orange bold]{pos.Line} line, {pos.Column} column[/]' \n\t" +
                        //           $"in '[orange bold]{doc.FileEntity}[/]'." +
                        //           $"{err_line}");
                        Log.Defer.Error($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/]", member, doc);
                        break;
                    case MethodDeclarationSyntax method:
                        Status.VeinStatus($"Regeneration method [grey]'{method.Identifier}'[/]");
                        method.OwnerClass = clazzSyntax;
                        methods.Add(CompileMethod(method, @class, doc));
                        break;
                    case FieldDeclarationSyntax field:
                        Status.VeinStatus($"Regeneration field [grey]'{field.Field.Identifier}'[/]");
                        field.OwnerClass = clazzSyntax;
                        fields.Add(CompileField(field, @class, doc));
                        break;
                    case PropertyDeclarationSyntax prop:
                        Status.VeinStatus($"Regeneration property [grey]'{prop.Identifier}'[/]");
                        prop.OwnerClass = clazzSyntax;
                        props.Add(CompileProperty(prop, @class, doc));
                        break;
                    default:
                        Log.Defer.Warn($"[grey]Member[/] '[yellow underline]{member.GetType().Name}[/]' [grey]is not supported.[/]", member, doc);
                        break;
                }
            }
            return (methods, fields, props);
        }

        public (MethodBuilder method, MethodDeclarationSyntax syntax)
            CompileMethod(MethodDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
        {
            var retType = member.ReturnType.IsSelf ? clazz :
                FetchType(member.ReturnType, doc);

            if (retType is null)
                return default;

            var args = GenerateArgument(member, doc);

            if (member.IsConstructor())
            {
                member.Identifier = new IdentifierExpression("ctor");

                if (args.Length == 0)
                    return (clazz.GetDefaultCtor() as MethodBuilder, member);
                var ctor = clazz.DefineMethod("ctor", GenerateMethodFlags(member), clazz, args);
                ctor.Owner = clazz;
                CompileAnnotation(member, doc, ctor);
                return (ctor, member);
            }

            var method = clazz.DefineMethod(member.Identifier.ExpressionString, GenerateMethodFlags(member), retType, args);

            method.Owner = clazz;

            if (clazz.IsInterface)
            {
                method.Flags |= MethodFlags.Abstract;
                method.Flags |= MethodFlags.Public;
            }

            CompileAnnotation(member, doc, method);

            return (method, member);
        }

        public (VeinField field, FieldDeclarationSyntax member)
            CompileField(FieldDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
        {
            var fieldType = member.Type.IsSelf ?
                clazz :
                FetchType(member.Type, doc);

            if (fieldType is null)
                return default;
            var name = member.Field.Identifier.ExpressionString;
            var @override = member.Aspects.FirstOrDefault(x => x.IsNative);

            if (@override is not null && @override.Args.Any())
            {
                var exp = @override.Args[0];
                if (exp is ArgumentExpression { Value: StringLiteralExpressionSyntax value })
                    name = value.Value;
                else
                {
                    Log.Defer.Error($"Invalid argument expression", exp);
                    throw new SkipStatementException();
                }
            }


            var field = clazz.DefineField(name, GenerateFieldFlags(member), fieldType);

            CompileAnnotation(member, doc, field);
            return (field, member);
        }

        public (VeinProperty prop, PropertyDeclarationSyntax member)
            CompileProperty(PropertyDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
        {
            var propType = member.Type.IsSelf ?
                clazz :
                FetchType(member.Type, doc);

            if (propType is null)
            {
                Log.Defer.Error($"[red bold]Unknown type detected. Can't resolve [italic]{member.Type.Identifier}[/][/] \n\t" +
                           PleaseReportProblemInto(),
                    member.Identifier, doc);
                return default;
            }

            if (member is { Setter: { IsEmpty: true }, Getter: { IsEmpty: true } })
                return (clazz.DefineAutoProperty(member.Identifier, GenerateFieldFlags(member), propType), member);
            return (clazz.DefineEmptyProperty(member.Identifier, GenerateFieldFlags(member), propType), member);
        }


        private static string PleaseReportProblemInto()
            => $"Please report the problem into 'https://github.com/vein-lang/vein/issues'.";

        public void GenerateCtor((ClassBuilder @class, MemberDeclarationSyntax member) x)
        {
            if (x.member is not ClassDeclarationSyntax member)
                return;
            var (@class, _) = x;
            Context.Document = member.OwnerDocument;
            var doc = member.OwnerDocument;

            if (@class.GetDefaultCtor() is not MethodBuilder ctor)
            {
                Log.Defer.Error($"[red bold]Class/struct '{@class.Name}' has problem with generate default ctor.[/]\n\t" +
                    $"{PleaseReportProblemInto()}",
                    null, doc);
                return;
            }

            Context.CurrentMethod = ctor;

            var gen = ctor.GetGenerator();

            if (!gen.HasMetadata("context"))
                gen.StoreIntoMetadata("context", Context);

            // emit calling based ctors
            @class.Parents.Select(z => z.GetDefaultCtor()).Where(z => z != null)
                .Pipe(z => gen.EmitThis().Emit(OpCodes.CALL, z))
                .Consume();


            var pregen = new List<(ExpressionSyntax exp, VeinField field)>();


            foreach (var field in @class.Fields)
            {
                if (field.IsStatic)
                    continue;
                if (gen.FieldHasAlreadyInited(field))
                    continue;
                if (field.IsLiteral)
                    continue; // TODO
                var stx = member.Fields
                    .SingleOrDefault(x => x.Field.Identifier.ExpressionString.Equals(field.Name));
                if (stx is null && field.IsSpecial)
                {
                    pregen.Add((null, field));
                    continue;
                }
                if (stx is null)
                {
                    Log.Defer.Error($"[red bold]Field '{field.Name}' in class/struct/interface '{@class.Name}' has undefined.[/]", null, doc);
                    continue;
                }
                pregen.Add((stx.Field.Expression, field));
            }

            foreach (var (exp, field) in pregen)
            {
                if (!field.Aspects.Any(x => x.Name.Equals("native", StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (exp is null)
                        gen.Emit(OpCodes.LDNULL);
                    else
                        gen.EmitExpression(exp);
                    gen.EmitStageField(field);
                }
            }
            // ctors has return himself
            gen.Emit(OpCodes.LDARG_0).Emit(OpCodes.RET);
        }

        public void GenerateStaticCtor((ClassBuilder @class, MemberDeclarationSyntax member) x)
        {
            if (x.member is not ClassDeclarationSyntax member)
                return;
            var (@class, _) = x;
            var doc = member.OwnerDocument;

            if (@class.GetStaticCtor() is not MethodBuilder ctor)
            {
                Log.Defer.Error($"[red bold]Class/struct '{@class.Name}' has problem with generate static ctor.[/]\n\t" +
                                $"{PleaseReportProblemInto()}",
                    null, doc);
                return;
            }
            Context.CurrentMethod = ctor;

            var gen = ctor.GetGenerator();

            gen.StoreIntoMetadata("context", Context);

            var pregen = new List<(ExpressionSyntax exp, VeinField field)>();

            foreach (var field in @class.Fields)
            {
                // skip non-static field,
                // they do not need to be initialized in the static constructor
                if (!field.IsStatic)
                    continue;
                if (gen.FieldHasAlreadyInited(field))
                    continue;
                var stx = member.Fields
                    .SingleOrDefault(x => x.Field.Identifier.ExpressionString.Equals(field.Name));

                if (stx is null && field.IsSpecial)
                {
                    pregen.Add((null, field));
                    continue;
                }
                if (stx is null)
                {
                    Log.Defer.Error($"[red bold]Field '{field.Name}' in class/struct '{@class.Name}' has undefined.[/]", null, doc);
                    continue;
                }
                pregen.Add((stx.Field.Expression, field));
            }

            foreach (var (exp, field) in pregen)
            {
                if (exp is null)
                    // value_type can also have a NULL value
                    gen.Emit(OpCodes.LDNULL);
                else
                    gen.EmitExpression(exp);
                gen.Emit(OpCodes.STSF, field);
            }
        }

        public void GenerateBody((MethodBuilder method, MethodDeclarationSyntax member) t)
        {
            if (t == default) return;
            var (method, member) = t;

            GenerateBody(method, member.Body, member.OwnerClass.OwnerDocument);
        }

        public void PostgenerateBody(MethodBuilder method)
        {
            var generator = method.GetGenerator();
            // fucking shit fucking
            // VM needs the end-of-method notation, which is RETURN.
            // but in case of the VOID method, user may not write it
            // and i didnt think of anything smarter than checking last OpCode
            if (!generator._opcodes.Any() && method.ReturnType.TypeCode == TYPE_VOID)
                generator.Emit(OpCodes.RET);
            if (generator._opcodes.Any() && generator._opcodes.Last() != OpCodes.RET.Value && method.ReturnType.TypeCode == TYPE_VOID)
                generator.Emit(OpCodes.RET);
        }

        private void GenerateBody(MethodBuilder method, BlockSyntax block, DocumentDeclaration doc)
        {
            if (block is null)
                return;

            Status.VeinStatus($"Emitting [gray]'{method.Owner.FullName}:{method.Name}'[/]");
            foreach (var pr in block.Statements.SelectMany(x => x.ChildNodes.Concat(new[] { x })))
                AnalyzeStatement(pr, doc);

            if (method.IsAbstract)
                return;

            var generator = method.GetGenerator();
            Context.Document = doc;
            Context.CurrentMethod = method;
            Context.CreateScope();
            generator.StoreIntoMetadata("context", Context);

            foreach (var statement in block.Statements)
            {
                try
                {
                    generator.EmitStatement(statement);
                }
                catch (NotSupportedException e)
                {
                    Log.Defer.Error($"[red bold]This syntax/statement currently is not supported.[/]", statement,
                        Context.Document);
#if DEBUG
                    if (_flags.DisplayStacktraceGenerator)
                        AnsiConsole.WriteException(e);
#endif
                }
                catch (NotImplementedException e)
                {
                    Log.Defer.Error($"[red bold]This syntax/statement currently is not implemented.[/]", statement,
                        Context.Document);
#if DEBUG
                    if (_flags.DisplayStacktraceGenerator)
                        AnsiConsole.WriteException(e);
#endif
                }
                catch (SkipStatementException e)
                {
#if DEBUG
                    if (_flags.DisplayStacktraceGenerator)
                        AnsiConsole.WriteException(e);
#endif
                }
                catch (Exception e)
                {
                    Log.Defer.Error($"[red bold]{e.Message.EscapeMarkup()}[/] in [italic]EmitStatement(...);[/]",
                        statement, Context.Document);
                }
            }
        }

        private void AnalyzeStatement(BaseSyntax statement, DocumentDeclaration doc)
        {
            if (statement is not IPassiveParseTransition { IsBrokenToken: true } transition)
                return;
            var pos = statement.Transform.pos;
            var e = transition.Error;
            var diff_err = statement.Transform.DiffErrorFull(doc);
            Log.errors.Enqueue($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/] \n\t" +
                       $"at '[orange bold]{pos.Line} line, {pos.Column} column[/]' \n\t" +
                       $"in '[orange bold]{doc.FileEntity}[/]'." +
                       $"{diff_err}");
        }

        public void GenerateProp((VeinProperty prop, PropertyDeclarationSyntax member) t)
        {
            if (t == default) return;

            var (prop, member) = t;
            var doc = member.OwnerClass.OwnerDocument;

            if (prop.Owner is not ClassBuilder clazz)
            {
                Log.Defer.Error($"[red bold]Internal error in[/] [orange bold]GenerateProp[/]\n\t{PleaseReportProblemInto()}",
                    member, doc);
                return;
            }

            if (prop.Setter is not null || prop.Getter is not null)
                return; // skip auto property (already generated).

            VeinArgumentRef[] getArgList(bool isSetter)
            {
                var val_ref = new VeinArgumentRef("value", prop.PropType);
                var this_ref = new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, prop.Owner);
                if (prop.IsStatic && !isSetter)
                    return new VeinArgumentRef[0];
                if (prop.IsStatic && isSetter)
                    return new VeinArgumentRef[1] { val_ref };
                if (!prop.IsStatic && isSetter)
                    return new VeinArgumentRef[2] { this_ref, val_ref };
                if (!prop.IsStatic && !isSetter)
                    return new VeinArgumentRef[1] { this_ref };
                throw new ArgumentException();
            }

            if (member.Setter is not null)
            {
                var args = getArgList(true);
                prop.Setter = clazz.DefineMethod($"set_{prop.Name}",
                    VeinProperty.ConvertShadowFlags(prop.Flags), prop.PropType, args);

                GenerateBody((MethodBuilder)prop.Setter, member.Setter.Body, doc);
            }

            if (member.Getter is not null || member.IsShortform())
            {
                var args = getArgList(false);
                prop.Getter = clazz.DefineMethod($"get_{prop.Name}",
                    VeinProperty.ConvertShadowFlags(prop.Flags), prop.PropType, args);

                if (member.Getter is not null)
                    GenerateBody((MethodBuilder)prop.Getter, member.Getter.Body, doc);
                if (member.IsShortform())
                    GenerateBody((MethodBuilder)prop.Getter, new(new ReturnStatementSyntax(member.Expression)), doc);
            }
        }

        public void GenerateField((VeinField field, FieldDeclarationSyntax member) t)
        {
            if (t == default)
                return;


            var (field, member) = t;
            var doc = member.OwnerClass.OwnerDocument;

            // skip uninited fields
            if (member.Field.Expression is null)
                return;

            // validate type compatible
            if (member.Field.Expression is LiteralExpressionSyntax literal)
            {
                if (literal is NumericLiteralExpressionSyntax numeric)
                {
                    if (!field.FieldType.TypeCode.CanImplicitlyCast(numeric))
                    {
                        var diff_err = literal.Transform.DiffErrorFull(doc);

                        var value = numeric.GetTypeCode();
                        var variable = member.Type.Identifier;
                        var variable1 = field.FieldType.TypeCode;

                        Log.errors.Enqueue(
                            $"[red bold]Cannot implicitly convert type[/] " +
                            $"'[purple underline]{numeric.GetTypeCode().AsClass().Name}[/]' to " +
                            $"'[purple underline]{field.FieldType.Name}[/]'.\n\t" +
                            $"at '[orange bold]{numeric.Transform.pos.Line} line, {numeric.Transform.pos.Column} column[/]' \n\t" +
                            $"in '[orange bold]{doc.FileEntity}[/]'." +
                            $"{diff_err}");
                    }
                }
                else if (literal.GetTypeCode() != field.FieldType.TypeCode)
                {
                    var diff_err = literal.Transform.DiffErrorFull(doc);
                    Log.errors.Enqueue(
                        $"[red bold]Cannot implicitly convert type[/] " +
                        $"'[purple underline]{literal.GetTypeCode().AsClass().Name}[/]' to " +
                        $"'[purple underline]{member.Type.Identifier}[/]'.\n\t" +
                        $"at '[orange bold]{literal.Transform.pos.Line} line, {literal.Transform.pos.Column} column[/]' \n\t" +
                        $"in '[orange bold]{doc.FileEntity}[/]'." +
                        $"{diff_err}");
                }
            }

            var clazz = field.Owner;

            if (member.Modifiers.Any(x => x.ModificatorKind == ModificatorKind.Const))
            {
                var assigner = member.Field.Expression;

                if (assigner is NewExpressionSyntax)
                {
                    var diff_err = assigner.Transform.DiffErrorFull(doc);
                    Log.errors.Enqueue(
                        $"[red bold]The expression being assigned to[/] '[purple underline]{member.Field.Identifier}[/]' [red bold]must be constant[/]. \n\t" +
                        $"at '[orange bold]{assigner.Transform.pos.Line} line, {assigner.Transform.pos.Column} column[/]' \n\t" +
                        $"in '[orange bold]{doc.FileEntity}[/]'." +
                        $"{diff_err}");
                    return;
                }

                try
                {
                    var converter = field.GetConverter();

                    if (assigner is UnaryExpressionSyntax { OperatorType: ExpressionType.Negate } negate)
                        module.WriteToConstStorage(field.FullName, converter($"-{negate.ExpressionString.Trim('(', ')')}")); // shit
                    else
                        module.WriteToConstStorage(field.FullName, converter(assigner.ExpressionString));
                }
                catch (ValueWasIncorrectException e)
                {
                    throw new MaybeMismatchTypeException(field, e);
                }
            }
        }

        private Dictionary<IdentifierExpression, VeinClass> KnowClasses = new ();

        private void LoadAliases()
        {
            foreach (var clazz in Target.LoadedModules.SelectMany(x => x.class_table)
                         .Where(x => x.Aspects.Any(x => x.IsAlias())))
            {
                var aliases = clazz.Aspects.Where(x => x.IsAlias()).Select(x => x.AsAlias());

                if (aliases.Count() > 1)
                {
                    Log.Defer.Error($"[red bold]Detected multiple alises[/] '[purple underline]{aliases.Select(x => x.Name)}[/]'");
                    continue;
                }

                var alias = aliases.Single();

                var class_id = new IdentifierExpression(clazz.Name);
                var alias_id = new IdentifierExpression(alias.Name);


                Status.VeinStatus($"Load alias [grey]'{class_id}'[/] -> [grey]'{alias_id}'[/]...");

                if (!KnowClasses.ContainsKey(class_id))
                    KnowClasses[class_id] = clazz;
                if (!KnowClasses.ContainsKey(alias_id))
                    KnowClasses[alias_id] = clazz;
            }
        }

        private VeinClass FetchType(IdentifierExpression typename, DocumentDeclaration doc)
        {
            if (KnowClasses.ContainsKey(typename))
                return KnowClasses[typename];

            var retType = module.TryFindType(typename.ExpressionString, doc.Includes);

            if (retType is null)
            {
                Log.Defer.Error($"[red bold]Cannot resolve type[/] '[purple underline]{typename}[/]'", typename, doc);
                return new UnresolvedVeinClass($"{this.module.Name}%global::{doc.Name}/{typename}");
            }

            return KnowClasses[typename] = retType;
        }
        private VeinClass FetchType(TypeSyntax typename, DocumentDeclaration doc)
            => FetchType(typename.Identifier, doc);

        private VeinArgumentRef[] GenerateArgument(MethodDeclarationSyntax method, DocumentDeclaration doc)
        {
            var args = new List<VeinArgumentRef>();
            var reserved = method.Parameters.FirstOrDefault(x => $"{x.Identifier}".Equals(VeinArgumentRef.THIS_ARGUMENT));

            if (reserved is not null)
            {
                Log.Defer.Error("Cannot use reserved argument name.", reserved.Identifier, doc);
                throw new SkipStatementException();
            }

            if (!method.Modifiers.Any(x => x.ModificatorKind == ModificatorKind.Static))
                args.Add(new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, FetchType(method.OwnerClass.Identifier, doc)));

            if (method.Parameters.Count == 0)
                return args.ToArray();

            return args.Concat(method.Parameters.Select(parameter => new VeinArgumentRef
            {
                Type = parameter.Type.IsSelf ?
                    FetchType(method.OwnerClass.Identifier, doc) :
                    FetchType(parameter.Type, doc),
                Name = parameter.Identifier.ExpressionString

            })).ToArray();
        }
        private ClassFlags GenerateClassFlags(ClassDeclarationSyntax clazz)
        {
            var flags = (ClassFlags) 0;

            var annotations = clazz.Aspects;
            var mods = clazz.Modifiers;

            foreach (var annotation in annotations)
            {
                var kind = annotation;
                switch (annotation)
                {

                    //Log.Defer.Error(
                    //    $"Cannot apply [orange bold]annotation[/] [red bold]{kind}[/] to [orange]'{clazz.Identifier}'[/] " +
                    //    $"class/struct/interface declaration.",
                    //    clazz.Identifier, clazz.OwnerDocument);
                    //continue;
                    case { IsSpecial: true }:
                        flags |= ClassFlags.Special;
                        continue;
                    case { IsNative: true }:
                    case { IsForwarded: true }:
                    case { IsAlias: true }:
                        continue;
                    //case VeinAnnotationKind.Readonly when !clazz.IsStruct:
                    //    Log.Defer.Error(
                    //        $"[orange bold]Aspect[/] [red bold]{kind}[/] can only be applied to a structure declaration.",
                    //        clazz.Identifier, clazz.OwnerDocument);
                    //    continue;
                    //case VeinAnnotationKind.Readonly when clazz.IsStruct:
                    //    // TODO
                    //    continue;

                    default:
                        Log.Defer.Error(
                            $"In [orange]'{clazz.Identifier}'[/] class/struct/interface [red bold]{kind}[/] " +
                            $"is not supported [orange bold]annotation[/].",
                            clazz.Identifier, clazz.OwnerDocument);
                        continue;
                }
            }

            foreach (var mod in mods)
            {
                switch (mod.ModificatorKind.ToString().ToLower())
                {
                    case "public":
                        flags |= ClassFlags.Public;
                        continue;
                    case "private":
                        flags |= ClassFlags.Private;
                        continue;
                    case "static":
                        flags |= ClassFlags.Static;
                        continue;
                    case "internal":
                        flags |= ClassFlags.Internal;
                        continue;
                    case "abstract":
                        flags |= ClassFlags.Abstract;
                        continue;
                    default:
                        Log.Defer.Error($"In [orange]'{clazz.Identifier}'[/] class/struct/interface " +
                                        $"[red bold]{mod}[/] is not supported [orange bold]modificator[/].",
                            clazz.Identifier, clazz.OwnerDocument);

                        continue;
                }
            }

            return flags;
        }

        private FieldFlags GenerateFieldFlags(MemberDeclarationSyntax member)
        {
            var flags = (FieldFlags)0;

            var annotations = member.Aspects;
            var mods = member.Modifiers;

            foreach (var annotation in annotations)
            {
                switch (annotation)
                {
                    //case VeinAnnotationKind.Virtual:
                    //    flags |= FieldFlags.Virtual;
                    //    continue;
                    case { IsSpecial: true }:
                        flags |= FieldFlags.Special;
                        continue;
                    case { IsNative: true }:
                        flags |= FieldFlags.Special;
                        continue;
                    //case VeinAnnotationKind.Readonly:
                    //    flags |= FieldFlags.Readonly;
                    //    continue;
                    //case VeinAnnotationKind.Getter:
                    //case VeinAnnotationKind.Setter:
                    //    //errors.Add($"In [orange]'{field.Field.Identifier}'[/] field [red bold]{kind}[/] is not supported [orange bold]annotation[/].");
                    //    continue;
                    default:
                        if (member is FieldDeclarationSyntax field)
                        {
                            Log.Defer.Error(
                                $"In [orange]'{field.Field.Identifier}'[/] field [red bold]{annotation.Name}[/] " +
                                $"is not supported [orange bold]annotation[/].",
                                annotation, field.OwnerClass.OwnerDocument);
                        }

                        if (member is PropertyDeclarationSyntax prop)
                        {
                            Log.Defer.Error(
                                $"In [orange]'{prop.Identifier}'[/] property [red bold]{annotation.Name}[/] " +
                                $"is not supported [orange bold]annotation[/].",
                                annotation, prop.OwnerClass.OwnerDocument);
                        }
                        continue;
                }
            }

            foreach (var mod in mods)
            {
                switch (mod.ModificatorKind)
                {
                    case ModificatorKind.Private:
                        continue;
                    case ModificatorKind.Public:
                        flags |= FieldFlags.Public;
                        continue;
                    case ModificatorKind.Static:
                        flags |= FieldFlags.Static;
                        continue;
                    case ModificatorKind.Protected:
                        flags |= FieldFlags.Protected;
                        continue;
                    case ModificatorKind.Internal:
                        flags |= FieldFlags.Internal;
                        continue;
                    case ModificatorKind.Override:
                        flags |= FieldFlags.Override;
                        continue;
                    case ModificatorKind.Const when member is PropertyDeclarationSyntax:
                        goto default;
                    case ModificatorKind.Const:
                        flags |= FieldFlags.Literal;
                        continue;
                    case ModificatorKind.Readonly:
                        flags |= FieldFlags.Readonly;
                        continue;
                    case ModificatorKind.Abstract:
                        flags |= FieldFlags.Abstract;
                        continue;
                    case ModificatorKind.Virtual:
                        flags |= FieldFlags.Virtual;
                        continue;
                    default:
                        switch (member)
                        {
                            case FieldDeclarationSyntax field:
                                Log.Defer.Error(
                                    $"In [orange]'{field.Field.Identifier}'[/] field [red bold]{mod.ModificatorKind}[/] " +
                                    $"is not supported [orange bold]modificator[/].",
                                    mod, field.OwnerClass.OwnerDocument);
                                break;
                            case PropertyDeclarationSyntax prop:
                                Log.Defer.Error(
                                    $"In [orange]'{prop.Identifier}'[/] property [red bold]{mod.ModificatorKind}[/] " +
                                    $"is not supported [orange bold]modificator[/].",
                                    mod, prop.OwnerClass.OwnerDocument);
                                break;
                        }
                        continue;
                }
            }


            //if (flags.HasFlag(FieldFlags.Private) && flags.HasFlag(MethodFlags.Public))
            //    errors.Add($"Modificator [red bold]public[/] cannot be combined with [red bold]private[/] in [orange]'{field.Field.Identifier}'[/] field.");


            return flags;
        }

        private MethodFlags GenerateMethodFlags(MethodDeclarationSyntax method)
        {
            var flags = (MethodFlags)0;

            var aspects = method.Aspects;
            var mods = method.Modifiers;

            foreach (var aspect in aspects)
            {
                switch (aspect)
                {
                    //case VeinAnnotationKind.Virtual:
                    //    flags |= MethodFlags.Virtual;
                    //    continue;
                    case { IsSpecial: true }:
                        flags |= MethodFlags.Special;
                        continue;
                    case { IsNative: true }:
                        continue;
                    //case VeinAnnotationKind.Readonly:
                    //case VeinAnnotationKind.Getter:
                    //case VeinAnnotationKind.Setter:
                    //    Log.Defer.Error(
                    //        $"In [orange]'{method.Identifier}'[/] method [red bold]{annotation.Name}[/] " +
                    //        $"is not supported [orange bold]annotation[/].",
                    //        method.Identifier, method.OwnerClass.OwnerDocument);
                    //    continue;
                    default:
                        Log.Defer.Error(
                            $"In [orange]'{method.Identifier}'[/] method [red bold]{aspect.Name}[/] " +
                            $"is not supported [orange bold]annotation[/].",
                            method.Identifier, method.OwnerClass.OwnerDocument);
                        continue;
                }
            }

            foreach (var mod in mods)
            {
                switch (mod.ModificatorKind.ToString().ToLower())
                {
                    case "public":
                        flags |= MethodFlags.Public;
                        continue;
                    case "extern":
                        flags |= MethodFlags.Extern;
                        continue;
                    case "private":
                        flags |= MethodFlags.Private;
                        continue;
                    case "static":
                        flags |= MethodFlags.Static;
                        continue;
                    case "protected":
                        flags |= MethodFlags.Protected;
                        continue;
                    case "internal":
                        flags |= MethodFlags.Internal;
                        continue;
                    case "override":
                        flags |= MethodFlags.Override;
                        continue;
                    case "virtual":
                        flags |= MethodFlags.Virtual;
                        continue;
                    case "abstract":
                        flags |= MethodFlags.Virtual;
                        continue;
                    default:
                        Log.Defer.Error(
                            $"In [orange]'{method.Identifier}'[/] method [red bold]{mod.ModificatorKind}[/] " +
                            $"is not supported [orange bold]modificator[/].",
                            method.Identifier, method.OwnerClass.OwnerDocument);
                        continue;
                }
            }


            if (flags.HasFlag(MethodFlags.Private) && flags.HasFlag(MethodFlags.Public))
                Log.Defer.Error(
                    $"Modificator [red bold]public[/] cannot be combined with [red bold]private[/] " +
                    $"in [orange]'{method.Identifier}'[/] method.",
                    method.ReturnType, method.OwnerClass.OwnerDocument);


            return flags;
        }
    }

    public static class ManaStatusContextEx
    {
        /// <summary>Sets the status message.</summary>
        /// <param name="context">The status context.</param>
        /// <param name="status">The status message.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static ProgressTask VeinStatus(this ProgressTask context, string status)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            Thread.Sleep(10); // so, i need it :(

            if (context.State.Get<bool>("isDeps"))
                context.Description = $"[red](dep)[/] {status}";
            if (context.State.Get<VeinProject>("project") is { } p)
                context.Description = $"[orange](project [purple]{p.Name}[/])[/] {status}";
            else
                context.Description = status;
            return context;
        }
    }
}
