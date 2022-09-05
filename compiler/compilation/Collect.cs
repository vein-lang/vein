namespace vein.compilation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cmd;
using project;
using Spectre.Console;
using styles;
using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    public static IReadOnlyCollection<CompilationTarget> Collect(
        DirectoryInfo info, ProgressTask task, ProgressContext ctx, CompileSettings settigs)
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
            t.CompilationSettings = settigs;

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
                targets.Add(project, new CompilationTarget(project, ctx) { CompilationSettings = settigs });
                compilationTarget.Dependencies.Add(targets[project]);
            }
        }

        task.StopTask();

        return targets.Values.ToList().AsReadOnly();
    }
}
