namespace vein.cmd;

using System.Collections.Concurrent;
using Expressive;
using fs;
using ishtar;
using Org.BouncyCastle.Asn1.X509;
using runtime;
using System.Collections.Generic;
using MoreLinq;
using styles;

[ExcludeFromCodeCoverage]
public class TestCommandSettings : CommandSettings, IProjectSettingProvider
{
    [Description("Path to vproj file")]
    [CommandArgument(0, "[PROJECT]")]
    public string Project { get; set; }

    [Description("Use fixture entry point")]
    [CommandOption("--fixture")]
    public bool UseFixtureEntryPoint { get; set; }

    [Description("Show log from vm")]
    [CommandOption("--trace")]
    public bool DiagnosticEnabled { get; set; }

    [Description("run test as parallel runner")]
    [CommandOption("--parallel")]
    public bool Parallel { get; set; }
}

[ExcludeFromCodeCoverage]
public class TestCommand(WorkloadDb db, ShardStorage shardStorage) : AsyncCommandWithProject<TestCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, TestCommandSettings settings, VeinProject project)
    {
        using var tag = ScopeMetric.Begin("project.test")
            .WithProject(project);
        if (settings.UseFixtureEntryPoint)
            return await new RunCommand(db).ExecuteAsync(ctx, new RunSettings
            {
                EntryPoint = "fixture",
                DoNotRedirectOutput = !settings.DiagnosticEnabled
            }, project);
        
        var execFile = project.WorkDir.SubDirectory("bin").File($"{project.Name}.wll");


        var resolver = new AssemblyResolver();

        var dependency = new List<VeinModule>();
        if (project.Dependencies.Packages.Count != 0)
        {
            resolver.AddSearchPath(project.WorkDir);

            void add_deps_path(IReadOnlyCollection<PackageReference> refs)
            {
                foreach (var package in refs)
                {
                    resolver.AddSearchPath(shardStorage.GetPackageSpace(package.Name, package.Version)
                        .SubDirectory("lib"));
                    var manifest = shardStorage.GetManifest(package.Name, package.Version);

                    if (manifest is null)
                    {
                        Log.Error($"Failed to load [orange]'{package.Name}@{package.Version}'[/] manifest.");
                        continue;
                    }

                    add_deps_path(manifest.Dependencies.AsReadOnly());
                }
            }

            add_deps_path(project.Dependencies.Packages);

            foreach (var package in project.Dependencies.Packages)
            {
                try
                {
                    dependency.Add(resolver.ResolveDep(package, dependency));
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to load dependencies.");
                    Log.Error($"Try 'rune restore'");
                    throw;
                }
            }
        }


        var asm = IshtarAssembly.LoadFromFile(execFile);

        var targetModule = resolver.Resolve(asm, dependency);

        var fixturesClasses = targetModule.class_table
            .Where(x => x.Aspects.Any(z => z.Name.Equals("fixture")))
            .ToList();

        var testMethods = fixturesClasses
            .SelectMany(x => x.Methods)
            .Where(x => x.IsStatic)
            .Where(x => x.Aspects.Any(z => z.Name.Equals("test")))
            .ToList();

        var results = new ConcurrentDictionary<string, List<(string, bool)>>();
        await AnsiConsole.AlternateScreenAsync(async () =>
        {
            await AnsiConsole.Progress()
                .HideCompleted(false)
                .AutoClear(true)
                .Columns(
                    new ProgressBarColumn(),
                    new SpinnerColumn { Spinner = Spinner.Known.Dots12, CompletedText = "✅", FailedText = "❌" },
                    new TaskDescriptionColumn { Alignment = Justify.Left })
                .StartAsync(async context => {
                    async ValueTask test(VeinMethod method, CancellationToken token = default)
                    {
                        var task = context.AddTask($"[orange]{method.Name.EscapeMarkup()}[/]...");
                        await Task.Delay(100, token);
                        var r = await new RunCommand(db).ExecuteAsync(ctx, new RunSettings
                        {
                            EntryPoint = method.Name,
                            EntryPointClass = method.Owner.FullName.NameWithNS,
                            DoNotRedirectOutput = true
                        }, project);

                        var lst = results.GetOrAdd(method.Owner.FullName.NameWithNS, new List<(string, bool)>());

                        lst.Add((method.RawName, r == 0));
                        task.Value(100);
                        await Task.Delay(100, token);
                        if (r != 0)
                            task.FailTask();
                        else
                            task.SuccessTask();
                        await Task.Delay(100, token);
                    }
                    if (settings.Parallel)
                        await Parallel.ForEachAsync(testMethods, test);
                    else
                        foreach (var method in testMethods)
                            await test(method);
                });
        });
        
        await Task.Delay(100);
        var root = new Tree($"Test cases '[orange]{targetModule.Name.moduleName.EscapeMarkup()}[/]'");
        foreach (var a in results.GroupBy(x => x.Key))
        {
            foreach (var (clazz, list) in a)
            {
                var classNode = root.AddNode($"[yellow]{a.Key.EscapeMarkup()}[/] {list.Count(x => x.Item2)}/{list.Count}");
                foreach (var (method, success) in list)
                    classNode.AddNode(success
                        ? $"\u2705 [lime rapidblink]PASSED[/] '[orange]{method.EscapeMarkup()}[/]'"
                        : $"\u274c [red rapidblink]FAILED[/] '[orange]{method.EscapeMarkup()}[/]'");
            }
        }
        
        AnsiConsole.Write(root);
        AnsiConsole.WriteLine();

        return 0;
    }
}

public class AssemblyResolver : ModuleResolverBase
{
    protected override void debug(string s) {}
}
