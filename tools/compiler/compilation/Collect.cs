namespace vein.compilation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cmd;
using project;
using Spectre.Console;
using styles;

public record ClassicCompilationProgressionTask(ProgressContext context, ProgressTask current) : IProgressionTask
{
    public IProgressionTask IsIndeterminate(bool has)
    {
        current.IsIndeterminate(has);
        return this;
    }

    public double MaxValue
    {
        get => current.MaxValue;
        set => current.MaxValue = value;
    }
    public IProgressionTask Increment(double value)
    {
        current.Increment(value);
        return this;
    }

    public void VeinStatus(string status) => current.VeinStatus(status);

    public void Description(string description) => current.Description(description);

    public void StopTask() => current.StopTask();

    public void SuccessTask() => current.SuccessTask();

    public void FailTask() => current.FailTask();

    public void Value(double value) => current.Value(value);

    public IProgressionTask AddChildTask(string description, bool autoStart = true, double maxValue = 100, bool allowHide = true) =>
        this with { current = context.AddTask(description, autoStart, maxValue, allowHide) };

    public IProgressionTask WithState<T>(string key, T val) where T : class
    {
        current.WithState(key, val);
        return this;
    }
}


public partial class CompilationTask
{
    public static IReadOnlyCollection<CompilationTarget> Collect(
        DirectoryInfo info, IProgressionTask task, CompileSettings settings)
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

            var t = new CompilationTarget(p, task)
            {
                CompilationSettings = settings
            };

            targets.Add(p, t);
        }

        task.IsIndeterminate(true);
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
            if (targets.TryGetValue(project, out var target))
                compilationTarget.Dependencies.Add(target);
            else
            {
                targets.Add(project, new CompilationTarget(project, task) { CompilationSettings = settings });
                compilationTarget.Dependencies.Add(targets[project]);
            }
        }

        task.StopTask();

        return targets.Values.ToList().AsReadOnly();
    }
}
