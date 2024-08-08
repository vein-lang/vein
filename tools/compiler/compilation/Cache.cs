namespace vein.compilation;

using System;
using Spectre.Console;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MoreLinq;
using Newtonsoft.Json;
using vein.cmd;

[ExcludeFromCodeCoverage]
public class Cache
{
    public static Asset Validate(CompilationTarget target, ProgressTask read_task,
        Dictionary<FileInfo, string> sources)
    {
        if (target.CompilationSettings?.IgnoreCache == true)
        {
            target.HasChanged = true;
            return new Asset();
        }
        var cacheFolder = target.Project.CacheDir;
        var file = cacheFolder.File("project.assets.json");

        if (!cacheFolder.Exists) cacheFolder.Create();

        var hashmap = new Dictionary<string, string>();

        var asset = (IAssetable)new Asset();

        asset.Hashes = hashmap;
        asset.HashesFile = file;

        sources = sources.ToDictionary(x => x.Key, x => x.Value);

        sources.Add(target.Project.ProjectFile, target.Project.ProjectFile.ReadToEnd());

        foreach (var (key, value) in sources)
        {
            read_task.Increment(1);
            read_task.VeinStatus($"Check changes [grey]'{key.Name}'[/]...");
            var hash = Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(value)));
            hashmap[key.Name] = hash;
        }


        read_task.Value(0);
        if (!file.Exists)
        {
            target.HasChanged = true;
            return (Asset)asset;
        }

        var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(file.ReadToEnd());

        if (!result.SequenceEqual(hashmap))
            target.HasChanged = true;
        return (Asset)asset;
    }

    public static void SaveAstAsset(CompilationTarget target)
    {
        var cacheFolder = target.Project.CacheDir.SubDirectory("ast");

        if (!cacheFolder.Exists) cacheFolder.Create();
        cacheFolder.EnumerateFiles("*.ast.json").Pipe(x => x.Delete()).Consume();

        foreach (var (key, val) in target.AST)
        {
            var name = key.Name;
            var result = JsonConvert.SerializeObject(val, Formatting.Indented);
            File.WriteAllText(cacheFolder.File($"{name}.ast.json").FullName, result);
        }
    }

    public interface IAssetable
    {
        Dictionary<string, string> Hashes { get; set; }
        FileInfo HashesFile { get; set; }
    }

    public struct Asset : IAssetable
    {
        Dictionary<string, string> IAssetable.Hashes { get; set; }
        FileInfo IAssetable.HashesFile { get; set; }
    }


    public static void SaveAssets(IAssetable asset)
    {
        if (asset.Hashes is null)
            return;
        if (asset.HashesFile.Exists) asset.HashesFile.Delete();
        File.WriteAllText(asset.HashesFile.FullName, JsonConvert.SerializeObject(asset.Hashes, Formatting.Indented));
    }
}
