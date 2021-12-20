namespace ishtar;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using emit;
using vein;
using vein.extensions;
using vein.fs;
using vein.runtime;

public abstract class ModuleResolverBase : IAssemblyResolver
{
    public const string MODULE_FILE_EXTENSION = "wll";
    protected readonly HashSet<DirectoryInfo> search_paths = new ();
    private readonly HashSet<string> search_paths_collider_ = new ();

    public ModuleResolverBase AddSearchPath(params DirectoryInfo[] dirs)
    {
        foreach (var dir in dirs)
        {
            if (!search_paths_collider_.Add(dir.FullName)) continue;
            search_paths.Add(dir);
            debug($"Assembly search path [gray]'{dir}'[/] is added.");
        }
        return this;
    }

    public virtual VeinModule ResolveDep(VeinArtifact artifact, IReadOnlyList<VeinModule> deps)
    {
        if (artifact.Path is null or { Exists: false })
        {
            debug($"Failed resolve 'unnamed' dependency for '{artifact.ProjectName}' project.");
            throw new FileNotFoundException();
        }

        if (!IshtarAssembly.TryLoadFromFile(artifact.Path, out var asm, out var exception))
        {
            debug($"Failed resolve [orange]'{artifact.Path.Name}'[/] dependency for [orange]'{artifact.ProjectName}'[/] project.");
            throw exception;
        }

        var mod = ModuleReader.Read(asm.Sections.First().data, deps,
            (s, v) => ResolveDep(s, v, deps));

        debug($"Dependency [orange]'{mod.Name}@{mod.Version}'[/] is resolved. [gray][[from artifacts]][/]");
        return mod;
    }

    public virtual VeinModule ResolveDep(IDependency package, IReadOnlyList<VeinModule> deps)
        => ResolveDep(package.Name, package.Version, deps);

    public virtual VeinModule ResolveDep(string name, Version version, IReadOnlyList<VeinModule> deps)
    {
        var file = FindInPaths(name);
        if (file is null)
        {
            debug($"Dependency '{name}' is not resolved.");
            throw new FileNotFoundException(name);
        }

        var asm = IshtarAssembly.LoadFromFile(file);

        var mod = ModuleReader.Read(asm.Sections.First().data, deps,
            (s, v) => ResolveDep(s, v, deps));
        debug($"Dependency [orange]'{name}@{mod.Version}'[/] is resolved. [gray][[from '{file}']][/]");
        return mod;
    }

    private FileInfo FindInPaths(string name)
    {
        try
        {
            var files = search_paths
                .Where(x => x.Exists)
                .SelectMany(x => x.EnumerateFiles($"*.{MODULE_FILE_EXTENSION}"))
                .Where(x =>
                    x.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            var result =
                files.Where(x =>
                    x.Name.Equals($"{name}.{MODULE_FILE_EXTENSION}", StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();

            if (result.Length > 1)
                throw new MultipleAssemblyVersionDetected($"{files.Select(x => $"{x.DirectoryName}/{x.Name}").Join(',')}");

            return result.Single();
        }
        catch
        {
            return null;
        }
    }

    protected abstract void debug(string s);
}


public class MultipleAssemblyVersionDetected : Exception
{
    public MultipleAssemblyVersionDetected(string msg) : base($"Multiple assembly version detected: {msg}")  { }
}
